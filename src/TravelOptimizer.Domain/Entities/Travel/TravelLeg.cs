namespace TravelOptimizer.Domain.Entities.Travel;

/// <summary>
/// One movement between two consecutive calendar events (event N → event N+1). Carries the
/// pre-computed learning key parts (corridor/dayType/hourBucket) so predictions and decisions can
/// be pooled and learned from later.
/// </summary>
public class TravelLeg
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string FromLabel { get; set; } = string.Empty;
    public double FromLat { get; set; }
    public double FromLng { get; set; }

    public string ToLabel { get; set; } = string.Empty;
    public double ToLat { get; set; }
    public double ToLng { get; set; }

    public DateTime ArriveBy { get; set; }   // next event start (UTC)
    public DateTime NotBefore { get; set; }  // prev event end (UTC)

    public string CorridorKey { get; set; } = string.Empty; // geohash7:geohash7
    public string DayType { get; set; } = string.Empty;
    public string HourBucket { get; set; } = string.Empty;

    public ICollection<TravelPrediction> Predictions { get; set; } = new List<TravelPrediction>();
    public TravelDecision? Decision { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
