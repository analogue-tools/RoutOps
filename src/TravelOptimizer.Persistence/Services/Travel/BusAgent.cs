using Microsoft.Extensions.Logging;
using TravelOptimizer.Domain.DataHelpers;

namespace TravelOptimizer.Persistence.Services.Travel;

public class BusAgent(HttpClient http, ILogger<BusAgent> logger) : SourceAgentBase(http, logger)
{
    public override string Mode => TravelMode.Bus;
    protected override string TflMode => "bus";
    protected override double FallbackKmPerHour => 13.0;
    protected override int TypicalWaitMin => 7;
    protected override double ApiConfidence => 0.60;
    protected override double MaxRangeKm => 20.0;
}
