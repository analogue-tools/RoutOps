namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>
/// The chosen option for a leg, logged with the policy version that produced it so it can be
/// replayed/learned from. "You cannot improve a decision you didn't record" (spec §1).
/// </summary>
public class TravelDecision
{
    public int Id { get; set; }
    public int TravelLegId { get; set; }
    public TravelLeg TravelLeg { get; set; } = null!;

    public string ChosenMode { get; set; } = string.Empty;
    public DateTime RecommendedDeparture { get; set; }
    public DateTime PredictedArrival { get; set; }
    public int PredictedWastedMin { get; set; }
    public bool WasExploration { get; set; } // Layer 2 flag
    public int PolicyVersion { get; set; }   // which weights produced it
    public string Rationale { get; set; } = string.Empty;

    public LegOutcome? Outcome { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
