using MediatR;
using Microsoft.AspNetCore.Mvc;
using TravelOptimizer.Api.Common;
using TravelOptimizer.Api.Mapping.Travel;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Interfaces.Travel;

namespace TravelOptimizer.Api.Controllers.Travel;

[Route("api/sources")]
public class SourcesController : ApiControllerBase
{
    [HttpGet("health")]
    public async Task<IActionResult> Health() => await Mediator.Send(new GetSourceHealthRequest());
}

public record GetSourceHealthRequest : IRequest<IActionResult>;

public class GetSourceHealthHandler(ISourceHealthState health) : IRequestHandler<GetSourceHealthRequest, IActionResult>
{
    public Task<IActionResult> Handle(GetSourceHealthRequest request, CancellationToken cancellationToken)
    {
        // always surface every known mode (defaults to healthy if it hasn't been exercised yet)
        var rows = TravelMode.All
            .Select(mode => health.Get(mode))
            .Select(s => new SourceHealthResponse(
                s.Mode, s.State, s.EwmaSuccessRate, s.ConsecutiveFailures, s.AvgMape,
                s.LastSuccessAt, s.LastFailureAt, s.DisabledUntil, s.UpdatedAt))
            .ToList();

        return Task.FromResult<IActionResult>(new OkObjectResult(rows));
    }
}
