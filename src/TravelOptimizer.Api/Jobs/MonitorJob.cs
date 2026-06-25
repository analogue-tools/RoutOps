using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Jobs;

/// <summary>
/// Watches the sources between plan-time and travel-time. Every ~30 min it re-queries TfL for legs
/// whose departure is coming up (and not yet travelled) and updates the decision when a disruption
/// shifts the best mode/departure (spec §1: react to a re-routed bus / suspended line).
/// </summary>
public class MonitorJob(
    AppDbContext db,
    IItineraryOptimizer optimizer,
    JobRunRegistry registry,
    ILogger<MonitorJob> logger) : IInvocable
{
    private const int LookaheadHours = 3;

    public async Task Invoke()
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddHours(LookaheadHours);

        var legIds = await db.TravelDecisions
            .Where(d => d.Outcome == null
                        && d.TravelLeg.ArriveBy > now
                        && d.RecommendedDeparture <= horizon)
            .Select(d => d.TravelLegId)
            .ToListAsync();

        if (legIds.Count == 0)
        {
            registry.Record(nameof(MonitorJob), true, "0 upcoming");
            return;
        }

        int changed = 0;
        foreach (var legId in legIds)
        {
            try
            {
                var result = await optimizer.ReoptimizeLegAsync(legId, CancellationToken.None);
                if (result.Changed) changed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MonitorJob failed to re-optimise leg {LegId}", legId);
            }
        }

        logger.LogInformation("MonitorJob checked {Count} upcoming leg(s); {Changed} plan(s) updated", legIds.Count, changed);
        registry.Record(nameof(MonitorJob), true, $"{legIds.Count} checked, {changed} updated");
    }
}
