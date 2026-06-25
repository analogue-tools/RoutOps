using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Interfaces.Travel;

namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>
/// Layer 2 selection. v1 is greedy-on-calibrated; the bandit path (safe exploration) is gated by
/// <see cref="PolicyOptions.Strategy"/>. Scoring follows spec §1 (minimise wasted time, hard
/// constrained to on-time arrival) and reads weights as data so Layer 3 can tune them.
/// </summary>
public class PolicyService(
    AppDbContext db,
    IOptions<PolicyOptions> options,
    ILogger<PolicyService> logger) : IPolicyService
{
    private readonly PolicyOptions _opts = options.Value;

    public async Task<TravelDecision> SelectAsync(TravelLeg leg, IReadOnlyList<TravelPrediction> candidates)
    {
        if (candidates.Count == 0)
            throw new InvalidOperationException($"No candidate predictions for leg {leg.Id}.");

        var weights = await LoadWeightsAsync(leg.UserId);
        int policyVersion = await GetPolicyVersionAsync(leg.UserId);

        double minBuffer = weights.GetValueOrDefault(PolicyKeys.MinBuffer, PolicyKeys.Defaults[PolicyKeys.MinBuffer]);

        var evals = candidates.Select(c => Evaluate(c, leg, weights)).ToList();

        var feasible = evals.Where(e => e.Feasible).ToList();
        OptionEval chosen;
        bool wasExploration = false;

        if (feasible.Count == 0)
        {
            // nothing arrives safely on time — pick the option that is least late, never explore
            chosen = evals.OrderBy(e => e.LatenessMin).ThenBy(e => e.EffectiveCost).First();
            logger.LogWarning("Leg {LegId}: no feasible on-time option; choosing least-late {Mode}", leg.Id, chosen.Pred.Mode);
        }
        else if (_opts.Strategy == PolicyStrategy.Bandit)
        {
            (chosen, wasExploration) = await SelectWithBanditAsync(leg, feasible, minBuffer);
        }
        else
        {
            chosen = feasible.OrderBy(e => e.EffectiveCost).First();
        }

        return new TravelDecision
        {
            TravelLegId = leg.Id,
            ChosenMode = chosen.Pred.Mode,
            RecommendedDeparture = chosen.Departure,
            PredictedArrival = chosen.Arrival,
            PredictedWastedMin = chosen.WastedMin,
            WasExploration = wasExploration,
            PolicyVersion = policyVersion,
            Rationale = BuildRationale(chosen, feasible.Count, wasExploration),
        };
    }

    /// <summary>Scores a single option per the §1 objective (shared with the Layer 3 shadow-eval).</summary>
    private static OptionEval Evaluate(TravelPrediction pred, TravelLeg leg, IReadOnlyDictionary<string, double> weights)
    {
        var s = TravelScoring.Evaluate(pred, leg, weights);
        return new OptionEval(pred, s.Departure, s.Arrival, s.WastedMin, s.EffectiveCost, s.Feasible, s.LatenessMin);
    }

    /// <summary>
    /// Layer 2 bandit (spec §2). Thompson-samples each feasible arm's wasted-time posterior; only
    /// deviates from greedy when (a) there is slack — the explored mode still clears minBuffer at its
    /// pessimistic estimate, (b) the corridor has enough samples to be worth exploring around, and
    /// (c) we are under the exploration-rate cap. Exploration here costs the user real minutes, so
    /// every guard must pass.
    /// </summary>
    private async Task<(OptionEval chosen, bool wasExploration)> SelectWithBanditAsync(
        TravelLeg leg, IReadOnlyList<OptionEval> feasible, double minBuffer)
    {
        var greedy = feasible.OrderBy(e => e.EffectiveCost).First();
        if (feasible.Count == 1) return (greedy, false);

        var history = await LoadArmHistoryAsync(leg);

        // Thompson sampling: draw an expected wasted-time for each arm from its posterior, pick best.
        OptionEval sampledBest = greedy;
        double bestSample = double.MaxValue;
        foreach (var opt in feasible)
        {
            double sample = SampleArm(opt, history.GetValueOrDefault(opt.Pred.Mode));
            if (sample < bestSample)
            {
                bestSample = sample;
                sampledBest = opt;
            }
        }

        if (ReferenceEquals(sampledBest, greedy) || sampledBest.Pred.Mode == greedy.Pred.Mode)
            return (greedy, false);

        // --- safe-exploration guards ---
        int totalSamples = history.Values.Sum(h => h.Count);
        if (totalSamples < _opts.MinSamplesToExplore) return (greedy, false);

        if (!ClearsBufferPessimistically(sampledBest.Pred, leg, minBuffer)) return (greedy, false);

        if (Random.Shared.NextDouble() > _opts.MaxExplorationRate) return (greedy, false);

        logger.LogInformation("Leg {LegId}: exploring {Mode} over greedy {Greedy}",
            leg.Id, sampledBest.Pred.Mode, greedy.Pred.Mode);
        return (sampledBest, true);
    }

    /// <summary>Pessimistic check: even if this mode runs PessimisticMultiplier slow, can we still arrive minBuffer early?</summary>
    private bool ClearsBufferPessimistically(TravelPrediction pred, TravelLeg leg, double minBuffer)
    {
        double pessimisticTravel = pred.CalibratedDurationMin * _opts.PessimisticMultiplier + pred.WaitMin;
        double availableMin = (leg.ArriveBy - leg.NotBefore).TotalMinutes - minBuffer;
        return pessimisticTravel <= availableMin;
    }

    /// <summary>Observed actual durations for each mode on this corridor+bucket (the arm reward history).</summary>
    private async Task<Dictionary<string, List<double>>> LoadArmHistoryAsync(TravelLeg leg)
    {
        var rows = await db.LegOutcomes
            .Where(o => o.TravelDecision.TravelLeg.CorridorKey == leg.CorridorKey
                        && o.TravelDecision.TravelLeg.HourBucket == leg.HourBucket)
            .Select(o => new { o.TravelDecision.ChosenMode, o.ActualDurationMin })
            .ToListAsync();

        return rows
            .GroupBy(r => r.ChosenMode)
            .ToDictionary(g => g.Key, g => g.Select(r => (double)r.ActualDurationMin).ToList());
    }

    /// <summary>Draws an expected wasted-time for an arm. No history → use the calibrated estimate with wide variance.</summary>
    private static double SampleArm(OptionEval opt, List<double>? samples)
    {
        if (samples is null || samples.Count == 0)
            return SampleNormal(opt.WastedMin, opt.WastedMin * 0.5); // optimistic prior, wide => encourages a look

        double mean = samples.Average();
        double variance = samples.Count > 1 ? samples.Sum(s => (s - mean) * (s - mean)) / (samples.Count - 1) : mean;
        double stdErr = Math.Sqrt(Math.Max(variance, 1) / samples.Count);
        return SampleNormal(mean, stdErr);
    }

    private static double SampleNormal(double mean, double stdDev)
    {
        // Box-Muller
        double u1 = 1.0 - Random.Shared.NextDouble();
        double u2 = 1.0 - Random.Shared.NextDouble();
        double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * z;
    }

    private async Task<Dictionary<string, double>> LoadWeightsAsync(int userId)
    {
        var rows = await db.PolicyWeights
            .Where(w => w.UserId == userId && w.IsActive)
            .ToListAsync();

        var dict = new Dictionary<string, double>();
        foreach (var w in rows) dict[w.Key] = w.Value;
        foreach (var (k, v) in PolicyKeys.Defaults) dict.TryAdd(k, v);
        return dict;
    }

    private async Task<int> GetPolicyVersionAsync(int userId)
    {
        bool any = await db.PolicyWeights.AnyAsync(w => w.UserId == userId && w.IsActive);
        if (!any) return 1;
        return await db.PolicyWeights
            .Where(w => w.UserId == userId && w.IsActive)
            .MaxAsync(w => w.Version);
    }

    private static string BuildRationale(OptionEval e, int feasibleCount, bool wasExploration)
    {
        var note = wasExploration ? " (exploration)" : string.Empty;
        return $"Chose {e.Pred.Mode}{note}: ~{e.WastedMin} min wasted, arrives {e.Arrival:HH:mm}, " +
               $"{feasibleCount} feasible option(s). {e.Pred.Rationale}";
    }

    /// <summary>Per-option scoring result; protected so the bandit subclass logic can reuse it.</summary>
    public sealed record OptionEval(
        TravelPrediction Pred,
        DateTime Departure,
        DateTime Arrival,
        int WastedMin,
        double EffectiveCost,
        bool Feasible,
        double LatenessMin);
}
