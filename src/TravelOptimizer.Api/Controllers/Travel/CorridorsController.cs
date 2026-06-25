using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Api.Common;
using TravelOptimizer.Api.Mapping.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Controllers.Travel;

[Route("api/corridors")]
public class CorridorsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => await Mediator.Send(new GetCorridorsRequest());

    [HttpGet("{key}/samples")]
    public async Task<IActionResult> Samples(string key, [FromQuery] int limit = 200) =>
        await Mediator.Send(new GetCorridorSamplesRequest(key, limit));
}

public record GetCorridorsRequest : IRequest<IActionResult>;

public class GetCorridorsHandler(AppDbContext db) : IRequestHandler<GetCorridorsRequest, IActionResult>
{
    public async Task<IActionResult> Handle(GetCorridorsRequest request, CancellationToken cancellationToken)
    {
        var models = await db.CorridorModels
            .GroupBy(m => m.CorridorKey)
            .Select(g => new
            {
                CorridorKey = g.Key,
                ModelCount = g.Count(),
                AvgMape = g.Average(m => m.Mape),
                Modes = g.Select(m => m.Mode).Distinct().ToList(),
            })
            .ToListAsync(cancellationToken);

        var since = DateTime.UtcNow.AddHours(-24);
        var samples = await db.CorridorSamples
            .GroupBy(s => s.CorridorKey)
            .Select(g => new
            {
                CorridorKey = g.Key,
                Total = g.Count(),
                Recent = g.Count(s => s.SampledAt >= since),
                LastSampledAt = (DateTime?)g.Max(s => s.SampledAt),
                Modes = g.Select(s => s.Mode).Distinct().ToList(),
            })
            .ToListAsync(cancellationToken);

        var sampleByKey = samples.ToDictionary(s => s.CorridorKey);
        var keys = models.Select(m => m.CorridorKey).Union(samples.Select(s => s.CorridorKey)).Distinct();

        var rows = keys.Select(key =>
        {
            var m = models.FirstOrDefault(x => x.CorridorKey == key);
            sampleByKey.TryGetValue(key, out var s);
            var modes = (m?.Modes ?? new List<string>())
                .Union(s?.Modes ?? new List<string>())
                .OrderBy(x => x)
                .ToList();

            return new CorridorSummaryResponse(
                key,
                modes,
                m?.ModelCount ?? 0,
                s?.Total ?? 0,
                s?.Recent ?? 0,
                m?.AvgMape ?? 0,
                s?.LastSampledAt);
        })
        .OrderByDescending(r => r.TotalSampleCount)
        .ThenByDescending(r => r.ModelCount)
        .ToList();

        return new OkObjectResult(rows);
    }
}

public record GetCorridorSamplesRequest(string Key, int Limit) : IRequest<IActionResult>;

public class GetCorridorSamplesHandler(AppDbContext db) : IRequestHandler<GetCorridorSamplesRequest, IActionResult>
{
    public async Task<IActionResult> Handle(GetCorridorSamplesRequest request, CancellationToken cancellationToken)
    {
        int limit = Math.Clamp(request.Limit, 1, 1000);

        var samples = await db.CorridorSamples
            .Where(s => s.CorridorKey == request.Key)
            .OrderByDescending(s => s.SampledAt)
            .Take(limit)
            .Select(s => new CorridorSampleResponse(
                s.Mode, s.DayType, s.HourBucket, s.PredictedDurationMin, s.WaitMin, s.Confidence, s.SampledAt))
            .ToListAsync(cancellationToken);

        return new OkObjectResult(samples);
    }
}
