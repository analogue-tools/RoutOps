using TravelOptimizer.Domain.Entities.Travel;

namespace TravelOptimizer.Domain.Interfaces.Travel;

/// <summary>
/// Layer 2 — selects the option for a leg from the calibrated, feasible candidates. v1 greedy;
/// v2 contextual bandit with safe exploration. Produces a (not-yet-persisted) TravelDecision.
/// </summary>
public interface IPolicyService
{
    Task<TravelDecision> SelectAsync(TravelLeg leg, IReadOnlyList<TravelPrediction> options);
}
