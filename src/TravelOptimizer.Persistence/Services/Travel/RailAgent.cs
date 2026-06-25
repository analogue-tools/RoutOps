using Microsoft.Extensions.Logging;
using TravelOptimizer.Domain.DataHelpers;

namespace TravelOptimizer.Persistence.Services.Travel;

public class RailAgent(HttpClient http, ILogger<RailAgent> logger) : SourceAgentBase(http, logger)
{
    public override string Mode => TravelMode.Rail;
    protected override string TflMode => "national-rail";
    protected override double FallbackKmPerHour => 45.0;
    protected override int TypicalWaitMin => 10;
    protected override double ApiConfidence => 0.75;
    protected override double MaxRangeKm => 120.0;
}
