namespace TravelOptimizer.Domain.Entities;

/// <summary>
/// Minimal user record. In the wider app this is owned elsewhere; the Travel area only needs the
/// timezone (for local-hour job scheduling, JOBS.md).
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>IANA timezone id, e.g. "Europe/London". Drives the local-hour gate in jobs.</summary>
    public string TimeZone { get; set; } = "Europe/London";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
