namespace TravelOptimizer.Domain.DataHelpers;

/// <summary>
/// Lifecycle of a source agent as tracked by the self-healing layer. A <c>healthy</c> source is
/// trusted as-is; a <c>degraded</c> source is still consulted but its confidence is down-weighted;
/// a <c>disabled</c> source is skipped (bar the occasional recovery probe) until its cooldown ends.
/// </summary>
public static class SourceHealthStatus
{
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Disabled = "disabled";

    public static readonly IReadOnlyList<string> All = new[] { Healthy, Degraded, Disabled };
}
