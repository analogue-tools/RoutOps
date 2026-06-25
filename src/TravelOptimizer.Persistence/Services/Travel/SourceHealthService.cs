using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Interfaces.Travel;

namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>
/// Durable companion to <see cref="ISourceHealthState"/>. The live state machine runs on the hot
/// path; this service (driven by HealthJob) folds learned corridor error into it and persists the
/// resulting snapshots so health survives restarts and is queryable by the dashboard.
/// </summary>
public class SourceHealthService(
    AppDbContext db,
    ISourceHealthState state,
    ILogger<SourceHealthService> logger) : ISourceHealthService
{
    private const int MinSamplesForMape = 3;

    public async Task RecomputeAndFlushAsync(CancellationToken ct)
    {
        // pull mean corridor MAPE per mode from the learned models (only where we have enough signal)
        var mapeByMode = await db.CorridorModels
            .Where(m => m.SampleCount >= MinSamplesForMape)
            .GroupBy(m => m.Mode)
            .Select(g => new { Mode = g.Key, AvgMape = g.Average(m => m.Mape) })
            .ToListAsync(ct);

        foreach (var row in mapeByMode)
            state.RecordMape(row.Mode, row.AvgMape);

        var existing = await db.SourceHealth.ToDictionaryAsync(s => s.Mode, ct);

        foreach (var snap in state.All())
        {
            if (!existing.TryGetValue(snap.Mode, out var row))
            {
                row = new SourceHealth { Mode = snap.Mode };
                db.SourceHealth.Add(row);
            }

            row.State = snap.State;
            row.EwmaSuccessRate = snap.EwmaSuccessRate;
            row.ConsecutiveFailures = snap.ConsecutiveFailures;
            row.AvgMape = snap.AvgMape;
            row.LastSuccessAt = snap.LastSuccessAt;
            row.LastFailureAt = snap.LastFailureAt;
            row.DisabledUntil = snap.DisabledUntil;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        logger.LogDebug("Flushed {Count} source-health snapshot(s)", state.All().Count);
    }

    public async Task SeedFromDbAsync(CancellationToken ct)
    {
        var rows = await db.SourceHealth.AsNoTracking().ToListAsync(ct);
        if (rows.Count > 0) state.Seed(rows);
    }
}
