namespace TravelOptimizer.Domain.DataHelpers;

/// <summary>Where a <see cref="Entities.Travel.LegOutcome"/> actual came from (see spec §3).</summary>
public static class LegOutcomeSource
{
    public const string ManualTap = "manual_tap";  // the "I arrived" tap — v1 source of truth
    public const string Inferred = "inferred";     // derived from a downstream signal
}
