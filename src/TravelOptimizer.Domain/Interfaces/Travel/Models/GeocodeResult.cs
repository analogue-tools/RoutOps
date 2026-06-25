namespace TravelOptimizer.Domain.Interfaces.Travel.Models;

/// <summary>Result of resolving a free-text calendar location to coordinates.</summary>
public record GeocodeResult(bool Found, double Lat, double Lng, string Label, double Confidence)
{
    public static GeocodeResult Miss(string label) => new(false, 0, 0, label, 0);
}
