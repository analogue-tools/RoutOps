using TravelOptimizer.Domain.Models.Travel;

namespace TravelOptimizer.Domain.Interfaces.Travel;

/// <summary>The decision agent (spec §1). Builds a day's itinerary, one logged decision per leg.</summary>
public interface IItineraryOptimizer
{
    Task<Itinerary> OptimizeDayAsync(int userId, DateOnly date, CancellationToken ct);

    /// <summary>
    /// Re-checks the sources for an existing leg and updates its decision in place (the MonitorJob
    /// path). Returns the change detected, so callers can log/notify when a disruption shifts the plan.
    /// </summary>
    Task<ReoptimizeResult> ReoptimizeLegAsync(int legId, CancellationToken ct);
}
