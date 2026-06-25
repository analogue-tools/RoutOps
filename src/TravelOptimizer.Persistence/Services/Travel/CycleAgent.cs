using Microsoft.Extensions.Logging;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Interfaces.Travel;

namespace TravelOptimizer.Persistence.Services.Travel;

public class CycleAgent(HttpClient http, ILogger<CycleAgent> logger, ISourceHealthState health)
    : SourceAgentBase(http, logger, health)
{
    public override string Mode => TravelMode.Cycle;
    protected override string TflMode => "cycle";
    protected override double FallbackKmPerHour => 15.0;
    protected override int TypicalWaitMin => 2; // unlock a bike
    protected override double ApiConfidence => 0.70;
    protected override double MaxRangeKm => 15.0;
}
