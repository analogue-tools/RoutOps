namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>
/// Layer 1 learned calibration. One row per (Mode, CorridorKey, DayType, HourBucket); an EWMA
/// multiplicative correction of the raw API estimate plus a running error (MAPE) for confidence.
/// </summary>
public class CorridorModel
{
    public int Id { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string CorridorKey { get; set; } = string.Empty;
    public string DayType { get; set; } = string.Empty;
    public string HourBucket { get; set; } = string.Empty;

    public double CorrectionFactor { get; set; } = 1.0; // EWMA actual/predicted
    public double Mape { get; set; } = 0.0;
    public int SampleCount { get; set; } = 0;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
