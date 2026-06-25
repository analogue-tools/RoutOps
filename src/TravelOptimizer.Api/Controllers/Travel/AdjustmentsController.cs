using Microsoft.AspNetCore.Mvc;
using TravelOptimizer.Api.Common;
using TravelOptimizer.Domain.DataHelpers;

namespace TravelOptimizer.Api.Controllers.Travel;

[Route("api/adjustments")]
public class AdjustmentsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string status = AdjustmentStatus.Proposed) =>
        await Mediator.Send(new GetProposedAdjustmentsRequest(GetUserId(), status));

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveAdjustmentRequest? request)
    {
        request ??= new ApproveAdjustmentRequest();
        request.UserId = GetUserId();
        request.AdjustmentId = id;
        return await Mediator.Send(request);
    }
}
