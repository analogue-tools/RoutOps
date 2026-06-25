namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>
/// A single decision weight, stored as data so Layer 3 can tune it. Versioned + IsActive so a new
/// version supersedes the old without losing history.
/// </summary>
public class PolicyWeight
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string Key { get; set; } = string.Empty; // PolicyKeys.* (riskWeight | minBuffer | pref:cycle ...)
    public double Value { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
