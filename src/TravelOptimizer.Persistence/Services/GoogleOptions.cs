namespace TravelOptimizer.Persistence.Services;

/// <summary>Bound from configuration ("Travel:Google"). Set up an OAuth 2.0 Web client in Google Cloud.</summary>
public class GoogleOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Must match a redirect URI registered on the OAuth client, e.g. https://localhost:5252/api/calendar/google/callback.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>How many days ahead to pull events on each sync.</summary>
    public int SyncWindowDays { get; set; } = 7;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
