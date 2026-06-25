namespace TravelOptimizer.Domain.Interfaces.Travel.Models;

/// <summary>Immutable view of a source's live health, handed out by <c>ISourceHealthState</c>.</summary>
public sealed record SourceHealthSnapshot(
    string Mode,
    string State,
    double EwmaSuccessRate,
    int ConsecutiveFailures,
    double AvgMape,
    DateTime? LastSuccessAt,
    DateTime? LastFailureAt,
    DateTime? DisabledUntil,
    DateTime UpdatedAt);
