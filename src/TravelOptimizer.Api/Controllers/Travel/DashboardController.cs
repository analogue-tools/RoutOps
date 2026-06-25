using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Api.Common;
using TravelOptimizer.Api.Jobs;
using TravelOptimizer.Api.Mapping.Travel;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Controllers.Travel;

[Route("api/dashboard")]
public class DashboardController : ApiControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview() => await Mediator.Send(new GetDashboardOverviewRequest(GetUserId()));
}

public record GetDashboardOverviewRequest(int UserId) : IRequest<IActionResult>;

public class GetDashboardOverviewHandler(
    AppDbContext db,
    ISourceHealthState health,
    JobRunRegistry registry,
    IHttpClientFactory httpFactory,
    IConfiguration config) : IRequestHandler<GetDashboardOverviewRequest, IActionResult>
{
    public async Task<IActionResult> Handle(GetDashboardOverviewRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        var tz = ResolveTimeZone(user?.TimeZone);

        var date = await ResolveDateAsync(request.UserId, tz, cancellationToken);
        var (fromUtc, toUtc) = DayWindowUtc(date, tz);

        var legs = await db.TravelLegs
            .Where(l => l.UserId == request.UserId && l.ArriveBy >= fromUtc && l.ArriveBy < toUtc)
            .Include(l => l.Predictions)
            .Include(l => l.Decision)
            .ToListAsync(cancellationToken);

        int legCount = legs.Count;
        int totalWasted = legs.Sum(l => l.Decision?.PredictedWastedMin ?? 0);
        int mixedOptions = legs.Sum(l => l.Predictions.Count(p => p.Mode == TravelMode.Mixed && p.Feasible));

        int corridorsLearned = await db.CorridorModels.Select(m => m.CorridorKey).Distinct().CountAsync(cancellationToken);
        int sampleCount = await db.CorridorSamples.CountAsync(cancellationToken);

        var pending = await db.ProposedAdjustments
            .Where(a => a.UserId == request.UserId && a.Status == AdjustmentStatus.Proposed)
            .ToListAsync(cancellationToken);

        var sources = TravelMode.All
            .Select(mode => health.Get(mode))
            .Select(s => new SourceHealthResponse(
                s.Mode, s.State, s.EwmaSuccessRate, s.ConsecutiveFailures, s.AvgMape,
                s.LastSuccessAt, s.LastFailureAt, s.DisabledUntil, s.UpdatedAt))
            .ToList();

        var system = await SystemHealthBuilder.BuildAsync(db, registry, httpFactory, config, cancellationToken);

        var overview = new DashboardOverviewResponse(
            date,
            legCount,
            totalWasted,
            mixedOptions,
            corridorsLearned,
            sampleCount,
            pending.Count,
            pending.Sum(a => a.ShadowImprovementMin),
            sources,
            system);

        return new OkObjectResult(overview);
    }

    /// <summary>Date of the soonest upcoming leg, else the most recent leg, else local today.</summary>
    private async Task<DateOnly> ResolveDateAsync(int userId, TimeZoneInfo tz, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var upcoming = await db.TravelLegs
            .Where(l => l.UserId == userId && l.ArriveBy >= now)
            .OrderBy(l => l.ArriveBy)
            .Select(l => (DateTime?)l.ArriveBy)
            .FirstOrDefaultAsync(ct);

        var pick = upcoming ?? await db.TravelLegs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.ArriveBy)
            .Select(l => (DateTime?)l.ArriveBy)
            .FirstOrDefaultAsync(ct);

        var local = pick is null
            ? TimeZoneInfo.ConvertTimeFromUtc(now, tz)
            : TimeZoneInfo.ConvertTimeFromUtc(pick.Value, tz);

        return DateOnly.FromDateTime(local);
    }

    private static (DateTime fromUtc, DateTime toUtc) DayWindowUtc(DateOnly date, TimeZoneInfo tz)
    {
        var localStart = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(localStart, tz), TimeZoneInfo.ConvertTimeToUtc(localStart.AddDays(1), tz));
    }

    private static TimeZoneInfo ResolveTimeZone(string? id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(id) ? "Europe/London" : id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Europe/London"); }
    }
}
