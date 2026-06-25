using Microsoft.AspNetCore.Mvc;
using TravelOptimizer.Api.Common;

namespace TravelOptimizer.Api.Controllers.Travel;

[Route("api/itineraries")]
public class ItinerariesController : ApiControllerBase
{
    [HttpPost("optimize")]
    public async Task<IActionResult> Optimize([FromBody] OptimizeDayRequest request)
    {
        request.UserId = GetUserId();
        return await Mediator.Send(request);
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> Get(DateOnly date) =>
        await Mediator.Send(new GetItineraryRequest(GetUserId(), date));
}
