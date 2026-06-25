namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>
/// One leg of a composite (mixed-mode) <see cref="TravelPrediction"/>, e.g. "walk to the station"
/// then "tube to the interchange". Single-mode predictions carry no segments.
/// </summary>
public class PredictionSegment
{
    public int Id { get; set; }
    public int TravelPredictionId { get; set; }
    public TravelPrediction TravelPrediction { get; set; } = null!;

    /// <summary>0-based position of this segment within the journey.</summary>
    public int Order { get; set; }

    public string Mode { get; set; } = string.Empty; // TravelMode.* (or verbatim TfL id)
    public int DurationMin { get; set; }
    public string FromLabel { get; set; } = string.Empty;
    public string ToLabel { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;

    /// <summary>Departure coordinates from TfL departurePoint (null when unavailable).</summary>
    public double? FromLat { get; set; }
    public double? FromLng { get; set; }
    public double? ToLat { get; set; }
    public double? ToLng { get; set; }
}
