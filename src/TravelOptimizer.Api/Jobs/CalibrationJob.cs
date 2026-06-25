using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Jobs;

/// <summary>
/// Hourly, every run (Layer 1). Folds any not-yet-ingested LegOutcomes into their CorridorModels.
/// This is the safety net — the LogLegOutcome endpoint already ingests in real time — and it also
/// catches outcomes created via the inferred path.
/// </summary>
public class CalibrationJob(AppDbContext db, ICalibrationService calibration, ILogger<CalibrationJob> logger)
    : IInvocable
{
    public async Task Invoke()
    {
        var pending = await db.LegOutcomes
            .Where(o => o.IngestedAt == null)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        if (pending.Count == 0) return;

        foreach (var outcome in pending)
            await calibration.IngestOutcomeAsync(outcome);

        logger.LogInformation("CalibrationJob ingested {Count} outcome(s)", pending.Count);
    }
}
