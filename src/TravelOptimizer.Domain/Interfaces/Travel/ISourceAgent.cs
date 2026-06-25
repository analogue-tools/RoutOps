using TravelOptimizer.Domain.Entities.Travel;

namespace TravelOptimizer.Domain.Interfaces.Travel;

/// <summary>
/// A single transport mode estimator (Tube/Bus/Rail/Cycle/Walk). Returns a RAW estimate straight
/// from the upstream API call; calibration is applied separately (Layer 1).
/// </summary>
public interface ISourceAgent
{
    string Mode { get; } // TravelMode.*

    Task<TravelPrediction> EstimateAsync(TravelLeg leg, CancellationToken ct);
}
