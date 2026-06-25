namespace TravelOptimizer.Domain.Models.Travel;

/// <summary>Outcome of re-checking a leg against the sources (MonitorJob).</summary>
public record ReoptimizeResult(
    int LegId,
    bool Changed,
    string PreviousMode,
    string NewMode,
    DateTime PreviousDeparture,
    DateTime NewDeparture,
    string Note)
{
    public static ReoptimizeResult Unchanged(int legId, string mode, DateTime departure) =>
        new(legId, false, mode, mode, departure, departure, "No change.");
}
