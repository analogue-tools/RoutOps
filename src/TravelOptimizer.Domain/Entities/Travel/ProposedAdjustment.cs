namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>
/// Layer 3 LLM output, gated. The reflection agent proposes; the backtest + human dispose. Every
/// draft carries the shadow-eval delta that decides whether it may auto-promote.
/// </summary>
public class ProposedAdjustment
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string Kind { get; set; } = string.Empty;   // AdjustmentKind.*
    public string Target { get; set; } = string.Empty; // e.g. "mode=cycle", "weight=risk_weight"
    public string Change { get; set; } = string.Empty; // e.g. "risk_weight 15 -> 22"
    public string Rationale { get; set; } = string.Empty;
    public double ShadowImprovementMin { get; set; }   // backtest delta (positive = less wasted time)
    public string Status { get; set; } = string.Empty; // AdjustmentStatus.*

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
