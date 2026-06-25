using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Interfaces.Travel;

namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>
/// Shared TfL Journey Planner plumbing for the source agents. Each concrete agent supplies its
/// mode parameters; the base issues the call and, on any failure (no key / network / no route),
/// degrades to a deterministic distance-based heuristic so the optimizer always has an estimate.
/// All values returned here are RAW — calibration (Layer 1) is applied downstream.
/// </summary>
public abstract class SourceAgentBase(HttpClient http, ILogger logger) : ISourceAgent
{
    public abstract string Mode { get; }

    /// <summary>TfL "mode" query value, e.g. "tube", "bus", "national-rail", "cycle", "walking".</summary>
    protected abstract string TflMode { get; }

    /// <summary>Average door-to-door speed used by the fallback heuristic.</summary>
    protected abstract double FallbackKmPerHour { get; }

    /// <summary>Typical first-leg wait for the mode (minutes).</summary>
    protected abstract int TypicalWaitMin { get; }

    /// <summary>Confidence we attach to a successful API estimate for this mode (0-1).</summary>
    protected abstract double ApiConfidence { get; }

    /// <summary>Hard reach limit for the mode in km (beyond this the option is infeasible).</summary>
    protected abstract double MaxRangeKm { get; }

    public async Task<TravelPrediction> EstimateAsync(TravelLeg leg, CancellationToken ct)
    {
        try
        {
            var fromTfl = await QueryTflAsync(leg, ct);
            if (fromTfl is not null) return fromTfl;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "TfL query failed for {Mode}; using heuristic fallback", Mode);
        }

        return Fallback(leg);
    }

    private async Task<TravelPrediction?> QueryTflAsync(TravelLeg leg, CancellationToken ct)
    {
        // TfL unified API: /Journey/JourneyResults/{lat,lng}/to/{lat,lng}?mode=...
        var from = $"{leg.FromLat.ToString(CultureInfo.InvariantCulture)},{leg.FromLng.ToString(CultureInfo.InvariantCulture)}";
        var to = $"{leg.ToLat.ToString(CultureInfo.InvariantCulture)},{leg.ToLng.ToString(CultureInfo.InvariantCulture)}";
        var url = $"Journey/JourneyResults/{from}/to/{to}?mode={TflMode}";

        using var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!doc.RootElement.TryGetProperty("journeys", out var journeys) ||
            journeys.ValueKind != JsonValueKind.Array || journeys.GetArrayLength() == 0)
        {
            return null;
        }

        // pick the shortest-duration journey
        int bestDuration = int.MaxValue;
        foreach (var j in journeys.EnumerateArray())
        {
            if (j.TryGetProperty("duration", out var d) && d.TryGetInt32(out var min))
                bestDuration = Math.Min(bestDuration, min);
        }

        if (bestDuration == int.MaxValue) return null;

        var disruptions = ExtractDisruptions(doc.RootElement);
        return new TravelPrediction
        {
            Mode = Mode,
            RawDurationMin = bestDuration,
            CalibratedDurationMin = bestDuration, // calibrated later by Layer 1
            WaitMin = TypicalWaitMin,
            Confidence = ApiConfidence,
            Feasible = true,
            Disruptions = disruptions,
            Rationale = $"TfL {TflMode} journey: {bestDuration} min.",
        };
    }

    private static string ExtractDisruptions(JsonElement root)
    {
        if (root.TryGetProperty("lineStatuses", out var ls) && ls.ValueKind == JsonValueKind.Array && ls.GetArrayLength() > 0)
            return ls.GetRawText();
        return string.Empty;
    }

    private TravelPrediction Fallback(TravelLeg leg)
    {
        double km = GeoMath.HaversineKm(leg.FromLat, leg.FromLng, leg.ToLat, leg.ToLng);
        // street/route factor: straight-line underestimates real travel, scale up ~1.3x
        double routeKm = km * 1.3;
        int durationMin = (int)Math.Ceiling(routeKm / FallbackKmPerHour * 60.0);
        bool feasible = km <= MaxRangeKm && durationMin > 0;

        return new TravelPrediction
        {
            Mode = Mode,
            RawDurationMin = Math.Max(durationMin, 1),
            CalibratedDurationMin = Math.Max(durationMin, 1),
            WaitMin = TypicalWaitMin,
            Confidence = 0.4, // heuristic is materially less certain than a live API estimate
            Feasible = feasible,
            Disruptions = string.Empty,
            Rationale = feasible
                ? $"Heuristic {Mode}: ~{routeKm:F1} km at {FallbackKmPerHour} km/h."
                : $"Infeasible for {Mode}: {km:F1} km exceeds {MaxRangeKm} km range.",
        };
    }
}
