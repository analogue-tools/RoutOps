using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TravelOptimizer.Domain.Interfaces;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Domain.Interfaces.Travel.Models;

namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>
/// Resolves free-text calendar locations. Tries a cheap deterministic parse first; only falls back
/// to the LLM for genuinely messy strings (spec §1: "LLM only on the fuzzy edge").
/// </summary>
public class GeocodingService(IChatCompletionService llm, ILogger<GeocodingService> logger) : IGeocodingService
{
    public async Task<GeocodeResult> GeocodeAsync(string locationText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(locationText))
            return GeocodeResult.Miss(locationText);

        if (TryParseLatLng(locationText, out var lat, out var lng))
            return new GeocodeResult(true, lat, lng, locationText, 1.0);

        try
        {
            return await ResolveWithLlmAsync(locationText, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LLM geocoding failed for '{Location}'", locationText);
            return GeocodeResult.Miss(locationText);
        }
    }

    private static bool TryParseLatLng(string text, out double lat, out double lng)
    {
        lat = 0; lng = 0;
        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2
               && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out lat)
               && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lng)
               && lat is >= -90 and <= 90 && lng is >= -180 and <= 180;
    }

    private async Task<GeocodeResult> ResolveWithLlmAsync(string locationText, CancellationToken ct)
    {
        const string system =
            "You geocode short location strings to London coordinates. Output only JSON: " +
            "{\"found\":bool,\"lat\":number,\"lng\":number,\"confidence\":number}. " +
            "If you cannot resolve it confidently, return found=false.";

        var json = await llm.CompleteJsonAsync(system, locationText, ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        bool found = root.TryGetProperty("found", out var f) && f.ValueKind == JsonValueKind.True;
        if (!found) return GeocodeResult.Miss(locationText);

        double lat = root.GetProperty("lat").GetDouble();
        double lng = root.GetProperty("lng").GetDouble();
        double conf = root.TryGetProperty("confidence", out var c) ? c.GetDouble() : 0.5;
        return new GeocodeResult(true, lat, lng, locationText, conf);
    }
}
