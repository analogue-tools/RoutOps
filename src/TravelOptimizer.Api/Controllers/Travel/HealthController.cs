using MediatR;
using Microsoft.AspNetCore.Mvc;
using TravelOptimizer.Api.Common;
using TravelOptimizer.Api.Jobs;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Controllers.Travel;

[Route("api/health")]
public class HealthController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => await Mediator.Send(new GetSystemHealthRequest());
}

public record GetSystemHealthRequest : IRequest<IActionResult>;

public class GetSystemHealthHandler(
    AppDbContext db,
    JobRunRegistry registry,
    IHttpClientFactory httpFactory,
    IConfiguration config) : IRequestHandler<GetSystemHealthRequest, IActionResult>
{
    public async Task<IActionResult> Handle(GetSystemHealthRequest request, CancellationToken cancellationToken)
    {
        var health = await SystemHealthBuilder.BuildAsync(db, registry, httpFactory, config, cancellationToken);
        return new OkObjectResult(health);
    }
}
