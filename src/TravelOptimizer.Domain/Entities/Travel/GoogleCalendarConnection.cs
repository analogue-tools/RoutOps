namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>
/// A user's connected Google Calendar. We persist the long-lived refresh token (obtained via the
/// OAuth consent flow) and exchange it for short-lived access tokens at sync time.
/// </summary>
public class GoogleCalendarConnection
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Which calendar to pull; "primary" by default.</summary>
    public string CalendarId { get; set; } = "primary";

    public DateTime? LastSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
