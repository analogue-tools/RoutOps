using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Jobs;

/// <summary>
/// Runs every minute; keeps each user's itinerary for today and tomorrow continuously built so it
/// can just be read via GET /api/itineraries/{date}. OptimizeDayAsync is idempotent, so re-running
/// is cheap and never duplicates legs — newly synced calendar events get picked up within a minute.
/// </summary>
public class OptimizeDayJob(
    AppDbContext db,
    IItineraryOptimizer optimizer,
    ILogger<OptimizeDayJob> logger) : IInvocable
{
    public async Task Invoke()
    {
        var users = await db.Users.ToListAsync();
        foreach (var user in users)
        {
            var today = JobTime.LocalToday(user);
            foreach (var date in new[] { today, today.AddDays(1) })
            {
                try
                {
                    await optimizer.OptimizeDayAsync(user.Id, date, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "OptimizeDayJob failed for user {User} on {Date}", user.Id, date);
                }
            }
        }
    }
}
