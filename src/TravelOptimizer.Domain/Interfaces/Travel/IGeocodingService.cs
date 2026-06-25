using TravelOptimizer.Domain.Interfaces.Travel.Models;

namespace TravelOptimizer.Domain.Interfaces.Travel;

/// <summary>Resolves a free-text calendar location string to coordinates (LLM only on the fuzzy edge).</summary>
public interface IGeocodingService
{
    Task<GeocodeResult> GeocodeAsync(string locationText, CancellationToken ct);
}
