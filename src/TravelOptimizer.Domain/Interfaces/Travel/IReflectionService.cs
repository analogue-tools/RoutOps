using TravelOptimizer.Domain.Entities.Travel;

namespace TravelOptimizer.Domain.Interfaces.Travel;

/// <summary>Layer 3 — the nightly self-review. Proposes; the backtest + human dispose.</summary>
public interface IReflectionService
{
    /// <summary>Assembles context, asks the LLM for structured drafts, shadow-evaluates each, and persists.</summary>
    Task<List<ProposedAdjustment>> ProposeAdjustmentsAsync(int userId, CancellationToken ct);

    /// <summary>Replays the last N days as if the proposal were active; returns wasted-minute delta (＋ = better).</summary>
    Task<double> ShadowEvaluateAsync(ProposedAdjustment proposal, int lookbackDays);
}
