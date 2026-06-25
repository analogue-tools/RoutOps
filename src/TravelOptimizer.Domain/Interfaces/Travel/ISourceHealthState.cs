using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Interfaces.Travel.Models;

namespace TravelOptimizer.Domain.Interfaces.Travel;

/// <summary>
/// In-memory, thread-safe source of truth for source-agent health. Updated on the hot path (one
/// cheap EWMA step per agent call) and read by the optimizer to skip/disable or down-weight sources.
/// The durable <see cref="SourceHealth"/> table is a periodic mirror of this state.
/// </summary>
public interface ISourceHealthState
{
    /// <summary>Records a live-API success for a mode and advances the state machine (recovery).</summary>
    void RecordSuccess(string mode);

    /// <summary>Records a fallback/failure for a mode and advances the state machine (degrade/disable).</summary>
    void RecordFailure(string mode);

    /// <summary>Folds a corridor MAPE reading in; high error can degrade an otherwise-reachable source.</summary>
    void RecordMape(string mode, double avgMape);

    string GetState(string mode);

    /// <summary>True while the mode is disabled; <paramref name="disabledUntil"/> is when its cooldown ends.</summary>
    bool IsDisabled(string mode, out DateTime? disabledUntil);

    SourceHealthSnapshot Get(string mode);
    IReadOnlyCollection<SourceHealthSnapshot> All();

    /// <summary>Rehydrates the in-memory state from a persisted snapshot (startup).</summary>
    void Seed(IEnumerable<SourceHealth> rows);
}
