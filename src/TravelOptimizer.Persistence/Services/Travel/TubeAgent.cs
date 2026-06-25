using Microsoft.Extensions.Logging;
using TravelOptimizer.Domain.DataHelpers;

namespace TravelOptimizer.Persistence.Services.Travel;

public class TubeAgent(HttpClient http, ILogger<TubeAgent> logger) : SourceAgentBase(http, logger)
{
    public override string Mode => TravelMode.Tube;
    protected override string TflMode => "tube";
    protected override double FallbackKmPerHour => 22.0;
    protected override int TypicalWaitMin => 4;
    protected override double ApiConfidence => 0.80;
    protected override double MaxRangeKm => 40.0;
}
