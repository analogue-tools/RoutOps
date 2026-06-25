namespace TravelOptimizer.Domain.Interfaces.Travel;

/// <summary>
/// Owns the durable side of source-health self-healing: pulls corridor MAPE into the live state,
/// flushes the in-memory snapshots to the SourceHealth table, and rehydrates state on startup.
/// </summary>
public interface ISourceHealthService
{
    /// <summary>Folds corridor MAPE into the live state and mirrors every mode to the DB.</summary>
    Task RecomputeAndFlushAsync(CancellationToken ct);

    /// <summary>Loads persisted snapshots into the in-memory state (call once on startup).</summary>
    Task SeedFromDbAsync(CancellationToken ct);
}
