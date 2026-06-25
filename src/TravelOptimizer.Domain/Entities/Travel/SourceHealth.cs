using TravelOptimizer.Domain.DataHelpers;

namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>
/// Persisted snapshot of a source agent's reliability (one row per mode). The live source of truth
/// is the in-memory <c>ISourceHealthState</c>; this table is the durable mirror flushed by HealthJob
/// so state survives restarts and is queryable by the dashboard.
/// </summary>
public class SourceHealth
{
    public int Id { get; set; }

    public string Mode { get; set; } = string.Empty; // TravelMode.* (unique)
    public string State { get; set; } = SourceHealthStatus.Healthy;

    /// <summary>EWMA of success (1) vs fallback (0) over recent calls.</summary>
    public double EwmaSuccessRate { get; set; } = 1.0;
    public int ConsecutiveFailures { get; set; }

    /// <summary>Mean corridor MAPE for this mode, pulled from the learned CorridorModels.</summary>
    public double AvgMape { get; set; }

    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastFailureAt { get; set; }

    /// <summary>While set and in the future, the source is in cooldown and is skipped.</summary>
    public DateTime? DisabledUntil { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
