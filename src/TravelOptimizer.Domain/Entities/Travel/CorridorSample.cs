namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>
/// A proactively-gathered point estimate for a corridor+mode at a moment in time. Produced by the
/// ProbeJob (not tied to a real trip) so the system keeps learning the corridors it sees often and
/// can chart predicted-time-by-hour even on days the user doesn't travel.
/// </summary>
public class CorridorSample
{
    public int Id { get; set; }

    public string Mode { get; set; } = string.Empty;       // TravelMode.*
    public string CorridorKey { get; set; } = string.Empty;
    public string DayType { get; set; } = string.Empty;
    public string HourBucket { get; set; } = string.Empty;

    public int PredictedDurationMin { get; set; }
    public int WaitMin { get; set; }
    public double Confidence { get; set; }
    public string Disruptions { get; set; } = string.Empty; // json

    public DateTime SampledAt { get; set; } = DateTime.UtcNow;
}
