using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Interfaces.Travel;

namespace TravelOptimizer.Persistence.Services.Travel;

public partial class AdjustmentPromoter(AppDbContext db, ILogger<AdjustmentPromoter> logger) : IAdjustmentPromoter
{
    public async Task PromoteAsync(ProposedAdjustment proposal, CancellationToken ct)
    {
        switch (proposal.Kind)
        {
            case AdjustmentKind.WeightChange:
            case AdjustmentKind.PreferenceRule:
                await PromoteWeightAsync(proposal, ct);
                break;
            default:
                // advisory only (CorridorNote, DataSourceFlag) — no weight to write
                logger.LogInformation("Promoting advisory adjustment {Id} ({Kind}) with no weight change",
                    proposal.Id, proposal.Kind);
                break;
        }

        proposal.Status = AdjustmentStatus.Active;
        await db.SaveChangesAsync(ct);
    }

    private async Task PromoteWeightAsync(ProposedAdjustment proposal, CancellationToken ct)
    {
        var key = NormaliseKey(proposal.Target);
        if (!TryExtractNewValue(proposal.Change, out var newValue))
        {
            logger.LogWarning("Cannot parse new value from change '{Change}' for adjustment {Id}; skipping weight write",
                proposal.Change, proposal.Id);
            return;
        }

        int maxVersion = await db.PolicyWeights
            .Where(w => w.UserId == proposal.UserId)
            .Select(w => (int?)w.Version)
            .MaxAsync(ct) ?? 0;

        var existing = await db.PolicyWeights
            .Where(w => w.UserId == proposal.UserId && w.Key == key && w.IsActive)
            .ToListAsync(ct);
        foreach (var w in existing) w.IsActive = false;

        db.PolicyWeights.Add(new PolicyWeight
        {
            UserId = proposal.UserId,
            Key = key,
            Value = newValue,
            Version = maxVersion + 1,
            IsActive = true,
        });

        logger.LogInformation("Promoted {Key} -> {Value} (v{Version}) for user {User}",
            key, newValue, maxVersion + 1, proposal.UserId);
    }

    /// <summary>Target may be "weight=risk_weight", "mode=cycle", or a bare key. Normalise to a PolicyKey.</summary>
    public static string NormaliseKey(string target)
    {
        var t = target.Trim();
        if (t.StartsWith("weight=", StringComparison.OrdinalIgnoreCase))
            return t["weight=".Length..].Trim();
        if (t.StartsWith("mode=", StringComparison.OrdinalIgnoreCase))
            return PolicyKeys.Preference(t["mode=".Length..].Trim());
        return t;
    }

    /// <summary>Pulls the last number out of a change string like "risk_weight 15 -> 22".</summary>
    public static bool TryExtractNewValue(string change, out double value)
    {
        value = 0;
        var matches = NumberRegex().Matches(change);
        if (matches.Count == 0) return false;
        return double.TryParse(matches[^1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    [GeneratedRegex(@"-?\d+(\.\d+)?")]
    private static partial Regex NumberRegex();
}
