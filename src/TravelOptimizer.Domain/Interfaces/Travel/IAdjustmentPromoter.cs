using TravelOptimizer.Domain.Entities.Travel;

namespace TravelOptimizer.Domain.Interfaces.Travel;

/// <summary>
/// Promotes a gated proposal into live policy: for weight/preference kinds it supersedes the active
/// PolicyWeight with a new version; for advisory kinds (notes/flags) it just flips status to Active.
/// Shared by the manual approve endpoint and the Layer 3 auto-promote path.
/// </summary>
public interface IAdjustmentPromoter
{
    Task PromoteAsync(ProposedAdjustment proposal, CancellationToken ct);
}
