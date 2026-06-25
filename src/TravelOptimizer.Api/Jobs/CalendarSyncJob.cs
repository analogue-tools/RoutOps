using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Jobs;

/// <summary>
/// Periodically pulls each connected user's upcoming Google Calendar events into CalendarEvent so
/// the optimizer always plans against a current calendar.
/// </summary>
public class CalendarSyncJob(AppDbContext db, IGoogleCalendarService google, ILogger<CalendarSyncJob> logger)
    : IInvocable
{
    public async Task Invoke()
    {
        var userIds = await db.GoogleCalendarConnections.Select(c => c.UserId).ToListAsync();
        foreach (var userId in userIds)
        {
            try
            {
                await google.SyncAsync(userId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CalendarSyncJob failed for user {User}", userId);
            }
        }
    }
}
