using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Entities.Travel;

namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>
/// The §1 objective, in one place so the live policy and the Layer 3 shadow-eval score options
/// identically. Pure function of (prediction, leg, weights).
/// </summary>
public static class TravelScoring
{
    public sealed record Score(
        DateTime Departure,
        DateTime Arrival,
        int WastedMin,
        double EffectiveCost,
        bool Feasible,
        double LatenessMin);

    public static Score Evaluate(TravelPrediction pred, TravelLeg leg, IReadOnlyDictionary<string, double> weights)
    {
        double riskWeight = weights.GetValueOrDefault(PolicyKeys.RiskWeight, PolicyKeys.Defaults[PolicyKeys.RiskWeight]);
        double minBuffer = weights.GetValueOrDefault(PolicyKeys.MinBuffer, PolicyKeys.Defaults[PolicyKeys.MinBuffer]);
        double prefPenalty = weights.GetValueOrDefault(PolicyKeys.Preference(pred.Mode), 0.0);

        int travelMin = pred.CalibratedDurationMin + pred.WaitMin;

        var latestDeparture = leg.ArriveBy.AddMinutes(-minBuffer - travelMin);
        var departure = latestDeparture < leg.NotBefore ? leg.NotBefore : latestDeparture;
        var arrival = departure.AddMinutes(travelMin);

        double idleMin = (leg.ArriveBy - arrival).TotalMinutes;
        double latenessMin = Math.Max(0, (arrival - leg.ArriveBy).TotalMinutes);
        int wastedMin = (int)Math.Round(travelMin + Math.Max(0, idleMin));

        double effectiveCost = wastedMin + riskWeight * (1 - pred.Confidence) + prefPenalty;
        bool feasible = pred.Feasible && arrival <= leg.ArriveBy.AddMinutes(-minBuffer);

        return new Score(departure, arrival, wastedMin, effectiveCost, feasible, latenessMin);
    }
}
