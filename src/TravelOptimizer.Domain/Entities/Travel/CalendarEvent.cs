namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>
/// A calendar event for a user. In the wider app these come from the existing calendar import;
/// here they are the input the optimizer turns into legs (event N → event N+1). Coordinates are
/// resolved by the geocoder when missing.
/// </summary>
public class CalendarEvent
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Provider event id (e.g. Google event id), used to upsert on re-sync. Empty if local.</summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>Where this event came from, e.g. "google" or "manual".</summary>
    public string Source { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty; // free-text, may need geocoding
    public double Lat { get; set; }
    public double Lng { get; set; }
    public bool HasCoordinates { get; set; }

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
