using Microsoft.Extensions.Logging;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Interfaces.Travel;

namespace TravelOptimizer.Persistence.Services.Travel;

public class RailAgent(HttpClient http, ILogger<RailAgent> logger, ISourceHealthState health)
    : SourceAgentBase(http, logger, health)
{
    public override string Mode => TravelMode.Rail;
    protected override string TflMode => "national-rail";
    protected override double FallbackKmPerHour => 45.0;
    protected override int TypicalWaitMin => 10;
    protected override double ApiConfidence => 0.75;
    protected override double MaxRangeKm => 120.0;
}
