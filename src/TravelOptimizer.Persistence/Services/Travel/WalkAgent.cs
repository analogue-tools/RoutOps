using Microsoft.Extensions.Logging;
using TravelOptimizer.Domain.DataHelpers;

namespace TravelOptimizer.Persistence.Services.Travel;

public class WalkAgent(HttpClient http, ILogger<WalkAgent> logger) : SourceAgentBase(http, logger)
{
    public override string Mode => TravelMode.Walk;
    protected override string TflMode => "walking";
    protected override double FallbackKmPerHour => 4.8;
    protected override int TypicalWaitMin => 0;
    protected override double ApiConfidence => 0.90; // walking is the most predictable mode
    protected override double MaxRangeKm => 6.0;
}
