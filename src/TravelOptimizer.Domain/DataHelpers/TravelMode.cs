namespace TravelOptimizer.Domain.DataHelpers;

/// <summary>
/// Travel modes the source agents can produce estimates for. String constants per ENTITIES.md
/// (entities never store raw mode strings inline).
/// </summary>
public static class TravelMode
{
    public const string Tube = "tube";
    public const string Bus = "bus";
    public const string Rail = "rail";
    public const string Cycle = "cycle";
    public const string Walk = "walk";

    public static readonly IReadOnlyList<string> All = new[] { Tube, Bus, Rail, Cycle, Walk };

    public static bool IsValid(string mode) => All.Contains(mode);
}
