namespace TravelOptimizer.Domain.DataHelpers;

/// <summary>
/// Selection strategy used by the policy service. v1 ships Greedy; Bandit is enabled by config
/// once Layer 1 has enough data to be worth exploring around (spec §2 Layer 2).
/// </summary>
public static class PolicyStrategy
{
    public const string Greedy = "greedy";
    public const string Bandit = "bandit";
}
