namespace TravelOptimizer.Domain.Interfaces.Travel;

/// <summary>
/// Google Calendar OAuth + sync. The connect flow stores a refresh token; sync pulls upcoming
/// events into the CalendarEvent table (deduped by provider event id) for the optimizer to use.
/// </summary>
public interface IGoogleCalendarService
{
    /// <summary>Builds the Google consent URL the user is sent to; <paramref name="state"/> round-trips the user id.</summary>
    string GetAuthorizationUrl(string state);

    /// <summary>Exchanges the OAuth code for tokens and stores the refresh token for the user.</summary>
    Task ConnectAsync(int userId, string code, CancellationToken ct);

    /// <summary>Pulls upcoming events for a connected user into CalendarEvent. Returns the number upserted.</summary>
    Task<int> SyncAsync(int userId, CancellationToken ct);

    Task<bool> IsConnectedAsync(int userId, CancellationToken ct);
}
