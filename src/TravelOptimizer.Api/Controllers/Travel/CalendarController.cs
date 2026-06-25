using Microsoft.AspNetCore.Mvc;
using TravelOptimizer.Api.Common;
using TravelOptimizer.Domain.Interfaces.Travel;

namespace TravelOptimizer.Api.Controllers.Travel;

[Route("api/calendar")]
public class CalendarController(IGoogleCalendarService google) : ApiControllerBase
{
    /// <summary>Step 1: send the user to Google's consent screen. State carries the user id back to the callback.</summary>
    [HttpGet("google/connect")]
    public IActionResult Connect()
    {
        var url = google.GetAuthorizationUrl(state: GetUserId().ToString());
        return Redirect(url);
    }

    /// <summary>Step 2: Google redirects here with a code; we exchange it for a refresh token and store it.</summary>
    [HttpGet("google/callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken ct)
    {
        if (!int.TryParse(state, out var userId))
            return BadRequest(new { message = "Invalid state." });

        await google.ConnectAsync(userId, code, ct);
        var synced = await google.SyncAsync(userId, ct);
        return Ok(new { connected = true, eventsSynced = synced });
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct) =>
        Ok(new { connected = await google.IsConnectedAsync(GetUserId(), ct) });

    /// <summary>Manual sync trigger (the CalendarSyncJob does this on a schedule).</summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken ct) =>
        Ok(new { eventsSynced = await google.SyncAsync(GetUserId(), ct) });
}
