namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>
/// The actual travel result for a decision (spec §3, the linchpin). The whole learning loop runs
/// on this; without it the agent can only measure whether its sources agree, not whether right.
/// </summary>
public class LegOutcome
{
    public int Id { get; set; }
    public int TravelDecisionId { get; set; }
    public TravelDecision TravelDecision { get; set; } = null!;

    public DateTime ActualArrival { get; set; }
    public int ActualDurationMin { get; set; }
    public string Source { get; set; } = string.Empty; // LegOutcomeSource.*
    public bool ArrivedOnTime { get; set; }

    /// <summary>Set once Layer 1 has folded this actual into its CorridorModel; keeps ingest idempotent.</summary>
    public DateTime? IngestedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
