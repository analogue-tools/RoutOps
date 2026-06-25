using Coravel.Invocable;
using TravelOptimizer.Domain.Interfaces.Travel;

namespace TravelOptimizer.Api.Jobs;

/// <summary>
/// Periodically recomputes source-health states — folding learned corridor MAPE into the live state
/// machine — and flushes the snapshots to the SourceHealth table so health survives restarts and is
/// queryable by the dashboard.
/// </summary>
public class HealthJob(ISourceHealthService health, JobRunRegistry registry, ILogger<HealthJob> logger)
    : IInvocable
{
    public async Task Invoke()
    {
        try
        {
            await registry.TrackAsync(nameof(HealthJob), () => health.RecomputeAndFlushAsync(CancellationToken.None));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HealthJob failed to recompute source health");
        }
    }
}
