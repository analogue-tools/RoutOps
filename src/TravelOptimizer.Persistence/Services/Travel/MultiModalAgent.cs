using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Interfaces.Travel;

namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>
/// Composite "best door-to-door route" agent. Unlike the single-mode agents it queries TfL WITHOUT a
/// mode filter, so the planner is free to stitch walking + tube + bus + rail into one journey. It
/// picks the shortest-total journey, breaks it into <see cref="PredictionSegment"/>s, and reports the
/// aggregate as a <see cref="TravelMode.Mixed"/> prediction that competes with the single modes on
/// the same scoring terms. Confidence tapers as the journey gets more transfers.
/// </summary>
public class MultiModalAgent(HttpClient http, ILogger<MultiModalAgent> logger, ISourceHealthState health)
    : SourceAgentBase(http, logger, health)
{
    public override string Mode => TravelMode.Mixed;
    protected override string TflMode => string.Empty;   // unused: we query without a mode filter
    protected override double FallbackKmPerHour => 18.0;  // blended door-to-door speed
    protected override int TypicalWaitMin => 5;
    protected override double ApiConfidence => 0.80;
    protected override double MaxRangeKm => 60.0;

    protected override async Task<TravelPrediction?> QueryTflAsync(TravelLeg leg, CancellationToken ct)
    {
        var url = $"Journey/JourneyResults/{FormatCoord(leg.FromLat, leg.FromLng)}/to/{FormatCoord(leg.ToLat, leg.ToLng)}";

        using var resp = await Http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!doc.RootElement.TryGetProperty("journeys", out var journeys) ||
            journeys.ValueKind != JsonValueKind.Array || journeys.GetArrayLength() == 0)
        {
            return null;
        }

        // choose the journey with the smallest summed leg duration, then materialise its segments
        List<PredictionSegment>? best = null;
        int bestTotal = int.MaxValue;
        foreach (var j in journeys.EnumerateArray())
        {
            var segs = ParseSegments(j);
            if (segs.Count == 0) continue;

            int total = segs.Sum(s => s.DurationMin);
            if (total < bestTotal)
            {
                bestTotal = total;
                best = segs;
            }
        }

        if (best is null || best.Count == 0) return null;

        int segmentCount = best.Count;
        double confidence = Math.Max(0.5, 0.80 - 0.05 * (segmentCount - 1));
        int waitMin = FirstSegmentWait(best);
        var disruptions = ExtractDisruptions(doc.RootElement);

        var summary = string.Join(" → ", best.Select(s => s.Mode));
        return new TravelPrediction
        {
            Mode = TravelMode.Mixed,
            RawDurationMin = bestTotal,
            CalibratedDurationMin = bestTotal, // calibrated later by Layer 1
            WaitMin = waitMin,
            Confidence = confidence,
            Feasible = true,
            Disruptions = disruptions,
            Rationale = $"TfL composite journey ({segmentCount} leg(s)): {summary}, {bestTotal} min.",
            Segments = best,
        };
    }

    private static List<PredictionSegment> ParseSegments(JsonElement journey)
    {
        var result = new List<PredictionSegment>();
        if (!journey.TryGetProperty("legs", out var legs) || legs.ValueKind != JsonValueKind.Array)
            return result;

        int order = 0;
        foreach (var leg in legs.EnumerateArray())
        {
            int duration = leg.TryGetProperty("duration", out var d) && d.TryGetInt32(out var min) ? min : 0;
            string modeId = ReadNested(leg, "mode", "id");
            string summaryText = ReadNested(leg, "instruction", "summary");
            string fromLabel = ReadNested(leg, "departurePoint", "commonName");
            string toLabel = ReadNested(leg, "arrivalPoint", "commonName");
            var fromLat = ReadPointCoord(leg, "departurePoint", "lat");
            var fromLng = ReadPointCoord(leg, "departurePoint", "lon");
            var toLat = ReadPointCoord(leg, "arrivalPoint", "lat");
            var toLng = ReadPointCoord(leg, "arrivalPoint", "lon");

            result.Add(new PredictionSegment
            {
                Order = order++,
                Mode = TravelMode.FromTflModeId(modeId),
                DurationMin = duration,
                FromLabel = fromLabel,
                ToLabel = toLabel,
                Summary = summaryText,
                FromLat = fromLat,
                FromLng = fromLng,
                ToLat = toLat,
                ToLng = toLng,
            });
        }

        return result;
    }

    /// <summary>Wait before boarding the first transit leg; a leading walk has no wait.</summary>
    private static int FirstSegmentWait(IReadOnlyList<PredictionSegment> segments)
    {
        var first = segments[0];
        return first.Mode == TravelMode.Walk ? 0 : TypicalWaitForMode(first.Mode);
    }

    private static int TypicalWaitForMode(string mode) => mode switch
    {
        TravelMode.Tube => 4,
        TravelMode.Bus => 7,
        TravelMode.Rail => 10,
        TravelMode.Cycle => 2,
        TravelMode.Walk => 0,
        _ => 5,
    };

    private static string ReadNested(JsonElement parent, string objName, string prop)
    {
        if (parent.TryGetProperty(objName, out var obj) && obj.ValueKind == JsonValueKind.Object &&
            obj.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String)
        {
            return val.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static double? ReadPointCoord(JsonElement parent, string objName, string prop)
    {
        if (!parent.TryGetProperty(objName, out var obj) || obj.ValueKind != JsonValueKind.Object ||
            !obj.TryGetProperty(prop, out var val))
        {
            return null;
        }

        if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var num))
            return num;

        if (val.ValueKind == JsonValueKind.String &&
            double.TryParse(val.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return null;
    }
}
