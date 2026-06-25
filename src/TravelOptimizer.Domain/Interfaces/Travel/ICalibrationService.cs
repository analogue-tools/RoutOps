using TravelOptimizer.Domain.Entities.Travel;

namespace TravelOptimizer.Domain.Interfaces.Travel;

/// <summary>Layer 1 — turns raw estimates into calibrated ones and learns from outcomes.</summary>
public interface ICalibrationService
{
    /// <summary>Applies the learned CorridorModel correction + confidence shrinkage to a raw prediction.</summary>
    Task<TravelPrediction> CalibrateAsync(TravelPrediction raw, TravelLeg leg);

    /// <summary>Folds a new actual into the matching CorridorModel (EWMA update). Idempotent per outcome.</summary>
    Task IngestOutcomeAsync(LegOutcome outcome);
}
