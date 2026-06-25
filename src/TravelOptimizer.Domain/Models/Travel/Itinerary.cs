using TravelOptimizer.Domain.Entities.Travel;

namespace TravelOptimizer.Domain.Models.Travel;

/// <summary>
/// Non-persisted aggregate returned by the decision agent for a single day. Rebuilt from persisted
/// legs/decisions/predictions by the GetItinerary read path.
/// </summary>
public class Itinerary
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public List<ItineraryLeg> Legs { get; set; } = new();

    /// <summary>Sum of the chosen decisions' predicted wasted minutes — the day's objective value.</summary>
    public int TotalPredictedWastedMin => Legs.Sum(l => l.Decision?.PredictedWastedMin ?? 0);
}

public class ItineraryLeg
{
    public TravelLeg Leg { get; set; } = null!;
    public TravelDecision? Decision { get; set; }
    public IReadOnlyList<TravelPrediction> Predictions { get; set; } = Array.Empty<TravelPrediction>();
}
