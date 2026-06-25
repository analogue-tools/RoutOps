using TravelOptimizer.Domain.DataHelpers;

namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>Bound from configuration ("Travel:Policy"). Controls Layer 2 behaviour.</summary>
public class PolicyOptions
{
    /// <summary>PolicyStrategy.Greedy (v1) or PolicyStrategy.Bandit (v2).</summary>
    public string Strategy { get; set; } = PolicyStrategy.Greedy;

    /// <summary>Max share of slack-bearing legs the bandit may explore on (spec §2: default 15%).</summary>
    public double MaxExplorationRate { get; set; } = 0.15;

    /// <summary>A bandit only explores a mode whose pessimistic estimate still clears minBuffer.</summary>
    public double PessimisticMultiplier { get; set; } = 1.25;

    /// <summary>Min samples before the bandit trusts a corridor enough to explore around it.</summary>
    public int MinSamplesToExplore { get; set; } = 5;
}
