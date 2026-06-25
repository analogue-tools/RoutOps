using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Jobs;

/// <summary>
/// Hourly; fires at the user's local 2 AM (Layer 3). Runs the LLM reflection → shadow-eval → gate.
/// Low-risk proposals that clear the backtest auto-promote; the rest land in the approval queue.
/// </summary>
public class ReflectionJob(AppDbContext db, IReflectionService reflection, ILogger<ReflectionJob> logger)
    : IInvocable
{
    private const int FireLocalHour = 2;

    public async Task Invoke()
    {
        var users = await db.Users.ToListAsync();
        foreach (var user in users)
        {
            if (JobTime.LocalHour(user) != FireLocalHour) continue;

            try
            {
                var proposals = await reflection.ProposeAdjustmentsAsync(user.Id, CancellationToken.None);
                if (proposals.Count > 0)
                    logger.LogInformation("ReflectionJob produced {Count} proposal(s) for user {User}", proposals.Count, user.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ReflectionJob failed for user {User}", user.Id);
            }
        }
    }
}
