using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Interfaces;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Domain.Interfaces.Travel.Models;

namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>
/// Layer 3 (spec §2/§9). The agent reviews its own track record nightly and proposes conservative,
/// testable improvements. Every draft is shadow-evaluated (counterfactual backtest) before it can
/// reach Active. The agent proposes; the backtest + human dispose.
/// </summary>
public class ReflectionService(
    AppDbContext db,
    IChatCompletionService llm,
    IAdjustmentPromoter promoter,
    IOptions<ReflectionOptions> options,
    ILogger<ReflectionService> logger) : IReflectionService
{
    private readonly ReflectionOptions _opts = options.Value;

    private const string SystemPrompt =
        "You audit a travel-routing agent's recent decisions and propose conservative, testable " +
        "improvements. Output only JSON matching {\"adjustments\":[{\"kind\":string,\"target\":string," +
        "\"change\":string,\"rationale\":string}]}. kind is one of preference_rule, weight_change, " +
        "data_source_flag, corridor_note. For weight_change, target is the weight key (e.g. risk_weight, " +
        "pref:cycle) and change ends with the proposed numeric value (e.g. 'risk_weight 15 -> 22'). " +
        "Never propose changes that risk late arrivals. Prefer few, high-confidence proposals.";

    public async Task<List<ProposedAdjustment>> ProposeAdjustmentsAsync(int userId, CancellationToken ct)
    {
        var context = await BuildContextAsync(userId, ct);
        if (context.Decisions.Count == 0)
        {
            logger.LogInformation("Reflection: no recent decisions for user {User}; nothing to propose", userId);
            return [];
        }

        List<AdjustmentDraft> drafts;
        try
        {
            var userJson = JsonSerializer.Serialize(context);
            var raw = await llm.CompleteJsonAsync(SystemPrompt, userJson, ct);
            drafts = ParseDrafts(raw);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reflection: LLM call/parse failed for user {User}", userId);
            return [];
        }

        var results = new List<ProposedAdjustment>();
        foreach (var draft in drafts)
        {
            if (!AdjustmentKind.All.Contains(draft.Kind))
            {
                logger.LogWarning("Reflection: discarding draft with unknown kind '{Kind}'", draft.Kind);
                continue;
            }

            var proposal = new ProposedAdjustment
            {
                UserId = userId,
                Kind = draft.Kind,
                Target = draft.Target,
                Change = draft.Change,
                Rationale = draft.Rationale,
                Status = AdjustmentStatus.Proposed,
            };

            proposal.ShadowImprovementMin = await ShadowEvaluateAsync(proposal, _opts.LookbackDays);
            db.ProposedAdjustments.Add(proposal);
            await db.SaveChangesAsync(ct);

            // The gate: low-risk + clears threshold → auto-promote. Else leave for one-tap human review.
            if (IsAutoPromotable(proposal) && proposal.ShadowImprovementMin >= _opts.AutoPromoteThresholdMinPerDay)
            {
                logger.LogInformation("Reflection: auto-promoting {Kind} '{Target}' (+{Delta:F1} min/day)",
                    proposal.Kind, proposal.Target, proposal.ShadowImprovementMin);
                await promoter.PromoteAsync(proposal, ct);
            }

            results.Add(proposal);
        }

        return results;
    }

    public async Task<double> ShadowEvaluateAsync(ProposedAdjustment proposal, int lookbackDays)
    {
        // Only weight/preference changes move the decision; advisory kinds have no measurable delta.
        if (proposal.Kind is not (AdjustmentKind.WeightChange or AdjustmentKind.PreferenceRule))
            return 0;

        var baseWeights = await LoadActiveWeightsAsync(proposal.UserId);
        var candidateWeights = ApplyProposal(baseWeights, proposal);
        if (candidateWeights is null) return 0;

        var since = DateTime.UtcNow.AddDays(-lookbackDays);
        var legs = await db.TravelLegs
            .Where(l => l.UserId == proposal.UserId && l.CreatedAt >= since && l.Decision != null)
            .Include(l => l.Predictions)
            .ToListAsync();

        if (legs.Count == 0) return 0;

        double oldTotal = 0, newTotal = 0;
        foreach (var leg in legs)
        {
            var preds = leg.Predictions.ToList();
            if (preds.Count == 0) continue;

            oldTotal += GreedyWasted(preds, leg, baseWeights);
            newTotal += GreedyWasted(preds, leg, candidateWeights);
        }

        double improvementTotal = oldTotal - newTotal; // positive = less wasted under the proposal
        int distinctDays = Math.Max(1, legs.Select(l => l.CreatedAt.Date).Distinct().Count());
        return improvementTotal / distinctDays; // min/day, comparable to the threshold
    }

    /// <summary>Greedy pick over the recorded predictions under a given weight set; returns its wasted minutes.</summary>
    private static double GreedyWasted(List<TravelPrediction> preds, TravelLeg leg, IReadOnlyDictionary<string, double> weights)
    {
        var scored = preds.Select(p => (p, s: TravelScoring.Evaluate(p, leg, weights))).ToList();
        var feasible = scored.Where(x => x.s.Feasible).ToList();
        var pick = feasible.Count > 0
            ? feasible.OrderBy(x => x.s.EffectiveCost).First()
            : scored.OrderBy(x => x.s.LatenessMin).ThenBy(x => x.s.EffectiveCost).First();
        return pick.s.WastedMin;
    }

    private bool IsAutoPromotable(ProposedAdjustment p)
    {
        // Conservative default (§11.2): preference rules and small weight changes may auto-promote;
        // anything touching minBuffer / feasibility always requires a human tap.
        if (p.Kind == AdjustmentKind.PreferenceRule) return true;
        if (p.Kind != AdjustmentKind.WeightChange) return false;

        var key = AdjustmentPromoter.NormaliseKey(p.Target);
        return key != PolicyKeys.MinBuffer;
    }

    private static Dictionary<string, double>? ApplyProposal(IReadOnlyDictionary<string, double> baseWeights, ProposedAdjustment p)
    {
        if (!AdjustmentPromoter.TryExtractNewValue(p.Change, out var newValue)) return null;
        var key = AdjustmentPromoter.NormaliseKey(p.Target);
        var dict = new Dictionary<string, double>(baseWeights) { [key] = newValue };
        return dict;
    }

    private async Task<Dictionary<string, double>> LoadActiveWeightsAsync(int userId)
    {
        var rows = await db.PolicyWeights.Where(w => w.UserId == userId && w.IsActive).ToListAsync();
        var dict = new Dictionary<string, double>();
        foreach (var w in rows) dict[w.Key] = w.Value;
        foreach (var (k, v) in PolicyKeys.Defaults) dict.TryAdd(k, v);
        return dict;
    }

    private async Task<ReflectionContext> BuildContextAsync(int userId, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-_opts.LookbackDays);

        var legs = await db.TravelLegs
            .Where(l => l.UserId == userId && l.CreatedAt >= since && l.Decision != null)
            .Include(l => l.Decision!).ThenInclude(d => d.Outcome)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);

        var decisions = legs.Select(l => new ReflectionDecision(
            l.Decision!.Id,
            l.CorridorKey,
            l.DayType,
            l.HourBucket,
            l.Decision.ChosenMode,
            l.Decision.PredictedWastedMin,
            l.Decision.WasExploration,
            l.Decision.Outcome?.ActualDurationMin,
            l.Decision.Outcome?.ArrivedOnTime,
            UserOverrode: false)) // override capture is a future signal (see notes)
            .ToList();

        var corridors = await db.CorridorModels
            .Where(m => m.SampleCount >= _opts.MinCorridorSamples && m.Mape >= _opts.HighMapeThreshold)
            .OrderByDescending(m => m.Mape)
            .Take(25)
            .Select(m => new ReflectionCorridor(m.Mode, m.CorridorKey, m.DayType, m.HourBucket, m.CorrectionFactor, m.Mape, m.SampleCount))
            .ToListAsync(ct);

        var weights = (await LoadActiveWeightsAsync(userId))
            .Select(kv => new ReflectionWeight(kv.Key, kv.Value))
            .ToList();

        return new ReflectionContext(userId, _opts.LookbackDays, decisions, corridors, weights);
    }

    private static List<AdjustmentDraft> ParseDrafts(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("adjustments", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<AdjustmentDraft>();
        foreach (var el in arr.EnumerateArray())
        {
            list.Add(new AdjustmentDraft(
                el.TryGetProperty("kind", out var k) ? k.GetString() ?? "" : "",
                el.TryGetProperty("target", out var t) ? t.GetString() ?? "" : "",
                el.TryGetProperty("change", out var c) ? c.GetString() ?? "" : "",
                el.TryGetProperty("rationale", out var r) ? r.GetString() ?? "" : ""));
        }

        return list;
    }
}
