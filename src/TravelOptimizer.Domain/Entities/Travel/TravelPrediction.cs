namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>One agent's estimate for a leg — both the raw API value and the calibrated value.</summary>
public class TravelPrediction
{
    public int Id { get; set; }
    public int TravelLegId { get; set; }
    public TravelLeg TravelLeg { get; set; } = null!;

    public string Mode { get; set; } = string.Empty; // TravelMode.*
    public int RawDurationMin { get; set; }
    public int CalibratedDurationMin { get; set; }
    public int WaitMin { get; set; }
    public double Confidence { get; set; }
    public bool Feasible { get; set; }
    public string Disruptions { get; set; } = string.Empty; // json
    public string Rationale { get; set; } = string.Empty;

    /// <summary>Ordered legs of a composite (mixed-mode) journey; empty for single-mode predictions.</summary>
    public ICollection<PredictionSegment> Segments { get; set; } = new List<PredictionSegment>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
