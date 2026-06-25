using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Jobs;

/// <summary>
/// Proactive corridor probing (spec: keep learning even on idle days). Derives the corridors the
/// user travels often (plus any upcoming legs), runs the full agent fan-out against current TfL
/// conditions, and stores the results as <see cref="CorridorSample"/> time-series rows. Cross-source
/// disagreement is logged as a confidence signal.
/// </summary>
public class ProbeJob(
    AppDbContext db,
    IEnumerable<ISourceAgent> agents,
    ICalibrationService calibration,
    JobRunRegistry registry,
    ILogger<ProbeJob> logger) : IInvocable
{
    private const int MaxCorridorsPerRun = 25;
    private static readonly TimeZoneInfo London = ResolveLondon();

    public async Task Invoke()
    {
        try
        {
            await registry.TrackAsync(nameof(ProbeJob), ProbeAsync);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ProbeJob failed");
        }
    }

    private async Task ProbeAsync()
    {
        var corridors = await SelectCorridorsAsync();
        if (corridors.Count == 0) return;

        var now = DateTime.UtcNow;
        var local = TimeZoneInfo.ConvertTimeFromUtc(now, London);
        var dayType = DayType.FromLocalDate(DateOnly.FromDateTime(local));
        var hourBucket = HourBucket.FromLocalHour(local.Hour);

        int stored = 0;
        foreach (var c in corridors)
        {
            var probeLeg = new TravelLeg
            {
                FromLat = c.FromLat, FromLng = c.FromLng,
                ToLat = c.ToLat, ToLng = c.ToLng,
                NotBefore = now,
                ArriveBy = now.AddHours(3),
                CorridorKey = c.CorridorKey,
                DayType = dayType,
                HourBucket = hourBucket,
            };

            var predictions = new List<TravelPrediction>();
            foreach (var agent in agents)
            {
                var raw = await agent.EstimateAsync(probeLeg, CancellationToken.None);
                await calibration.CalibrateAsync(raw, probeLeg);
                predictions.Add(raw);

                db.CorridorSamples.Add(new CorridorSample
                {
                    Mode = raw.Mode,
                    CorridorKey = c.CorridorKey,
                    DayType = dayType,
                    HourBucket = hourBucket,
                    PredictedDurationMin = raw.CalibratedDurationMin,
                    WaitMin = raw.WaitMin,
                    Confidence = raw.Confidence,
                    Disruptions = raw.Disruptions,
                    SampledAt = now,
                });
                stored++;
            }

            LogDisagreement(c.CorridorKey, predictions);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("ProbeJob sampled {Corridors} corridor(s), stored {Rows} sample(s)", corridors.Count, stored);
    }

    /// <summary>Frequent historical corridors (by leg count) unioned with any upcoming, untravelled legs.</summary>
    private async Task<List<Corridor>> SelectCorridorsAsync()
    {
        var now = DateTime.UtcNow;

        var frequent = await db.TravelLegs
            .GroupBy(l => l.CorridorKey)
            .Select(g => new { Key = g.Key, Count = g.Count(), Rep = g.OrderByDescending(l => l.CreatedAt).First() })
            .OrderByDescending(x => x.Count)
            .Take(MaxCorridorsPerRun)
            .Select(x => x.Rep)
            .ToListAsync();

        var upcoming = await db.TravelLegs
            .Where(l => l.ArriveBy > now && (l.Decision == null || l.Decision.Outcome == null))
            .ToListAsync();

        return frequent.Concat(upcoming)
            .GroupBy(l => l.CorridorKey)
            .Select(g => g.First())
            .Take(MaxCorridorsPerRun)
            .Select(l => new Corridor(l.CorridorKey, l.FromLat, l.FromLng, l.ToLat, l.ToLng))
            .ToList();
    }

    private void LogDisagreement(string corridorKey, IReadOnlyList<TravelPrediction> predictions)
    {
        var feasible = predictions.Where(p => p.Feasible && p.CalibratedDurationMin > 0).ToList();
        if (feasible.Count < 2) return;

        var durations = feasible.Select(p => (double)p.CalibratedDurationMin).ToList();
        double mean = durations.Average();
        double std = Math.Sqrt(durations.Sum(d => (d - mean) * (d - mean)) / durations.Count);
        double spread = mean > 0 ? std / mean : 0;

        if (spread > 0.6)
            logger.LogDebug("ProbeJob: high source disagreement on {Corridor} (cv={Spread:F2})", corridorKey, spread);
    }

    private static TimeZoneInfo ResolveLondon()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/London"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
    }

    private readonly record struct Corridor(string CorridorKey, double FromLat, double FromLng, double ToLat, double ToLng);
}
