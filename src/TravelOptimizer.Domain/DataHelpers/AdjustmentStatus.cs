namespace TravelOptimizer.Domain.DataHelpers;

/// <summary>Lifecycle of a Layer 3 reflection proposal as it moves through the gate.</summary>
public static class AdjustmentStatus
{
    public const string Proposed = "proposed";   // awaiting human approval
    public const string Active = "active";       // promoted, decision agent reads it
    public const string Rejected = "rejected";   // backtest failed or user declined

    public static readonly IReadOnlyList<string> All = new[] { Proposed, Active, Rejected };
}
