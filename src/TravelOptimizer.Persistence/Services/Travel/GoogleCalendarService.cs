using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Exceptions;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Persistence.Services;

namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>
/// Google Calendar OAuth + sync. Stores the refresh token from the consent flow and exchanges it
/// for access tokens at sync time; pulls upcoming located events into CalendarEvent (deduped by
/// Google event id) for the optimizer.
/// </summary>
public class GoogleCalendarService(
    AppDbContext db,
    IOptions<GoogleOptions> options,
    ILogger<GoogleCalendarService> logger) : IGoogleCalendarService
{
    private const string GoogleUser = "user"; // single principal per flow; tokens are scoped per connection row
    private readonly GoogleOptions _opts = options.Value;

    public string GetAuthorizationUrl(string state)
    {
        EnsureConfigured();
        var request = (GoogleAuthorizationCodeRequestUrl)CreateFlow().CreateAuthorizationCodeRequest(_opts.RedirectUri);
        request.AccessType = "offline"; // ask for a refresh token
        request.Prompt = "consent";     // force a refresh token even on re-consent
        request.State = state;
        return request.Build().ToString();
    }

    public async Task ConnectAsync(int userId, string code, CancellationToken ct)
    {
        EnsureConfigured();
        var token = await CreateFlow().ExchangeCodeForTokenAsync(GoogleUser, code, _opts.RedirectUri, ct);
        if (string.IsNullOrWhiteSpace(token.RefreshToken))
            throw new BadRequestException("Google did not return a refresh token. Revoke access and reconnect.");

        var conn = await db.GoogleCalendarConnections.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (conn is null)
        {
            conn = new GoogleCalendarConnection { UserId = userId };
            db.GoogleCalendarConnections.Add(conn);
        }

        conn.RefreshToken = token.RefreshToken;
        conn.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Google Calendar connected for user {User}", userId);
    }

    public async Task<bool> IsConnectedAsync(int userId, CancellationToken ct) =>
        await db.GoogleCalendarConnections.AnyAsync(c => c.UserId == userId, ct);

    public async Task<int> SyncAsync(int userId, CancellationToken ct)
    {
        var conn = await db.GoogleCalendarConnections.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (conn is null)
        {
            logger.LogDebug("Sync skipped: user {User} has no Google connection", userId);
            return 0;
        }

        var service = BuildService(conn.RefreshToken);
        var listRequest = service.Events.List(conn.CalendarId);
        listRequest.TimeMinDateTimeOffset = DateTimeOffset.UtcNow;
        listRequest.TimeMaxDateTimeOffset = DateTimeOffset.UtcNow.AddDays(_opts.SyncWindowDays);
        listRequest.SingleEvents = true;
        listRequest.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        Events events;
        try
        {
            events = await listRequest.ExecuteAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google Calendar sync failed for user {User}", userId);
            return 0;
        }

        int upserted = 0;
        foreach (var ev in events.Items ?? [])
        {
            var start = ev.Start?.DateTimeDateTimeOffset;
            var end = ev.End?.DateTimeDateTimeOffset;
            if (start is null || end is null) continue;          // all-day events have no travel leg
            if (string.IsNullOrWhiteSpace(ev.Location)) continue; // need a location to route to
            if (string.IsNullOrWhiteSpace(ev.Id)) continue;

            var existing = await db.CalendarEvents
                .FirstOrDefaultAsync(e => e.UserId == userId && e.ExternalId == ev.Id, ct);

            if (existing is null)
            {
                existing = new CalendarEvent { UserId = userId, ExternalId = ev.Id!, Source = "google" };
                db.CalendarEvents.Add(existing);
            }

            existing.Title = ev.Summary ?? string.Empty;
            existing.Location = ev.Location!;
            existing.HasCoordinates = false; // geocoded lazily at optimize time
            existing.StartUtc = start.Value.UtcDateTime;
            existing.EndUtc = end.Value.UtcDateTime;
            upserted++;
        }

        conn.LastSyncedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Google Calendar sync upserted {Count} event(s) for user {User}", upserted, userId);
        return upserted;
    }

    private CalendarService BuildService(string refreshToken)
    {
        var flow = CreateFlow();
        var credential = new UserCredential(flow, GoogleUser, new TokenResponse { RefreshToken = refreshToken });
        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "TravelOptimizer",
        });
    }

    private GoogleAuthorizationCodeFlow CreateFlow() => new(new GoogleAuthorizationCodeFlow.Initializer
    {
        ClientSecrets = new ClientSecrets { ClientId = _opts.ClientId, ClientSecret = _opts.ClientSecret },
        Scopes = [CalendarService.Scope.CalendarEventsReadonly],
    });

    private void EnsureConfigured()
    {
        if (!_opts.IsConfigured)
            throw new BadRequestException("Google OAuth is not configured (Travel:Google:ClientId/ClientSecret).");
    }
}
