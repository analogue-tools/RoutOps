using Microsoft.AspNetCore.Mvc;
using TravelOptimizer.Api.Common;

namespace TravelOptimizer.Api.Controllers.Travel;

[Route("api/legs")]
public class LegsController : ApiControllerBase
{
    /// <summary>★ The actuals tap — the input the whole learning loop runs on (spec §7).</summary>
    [HttpPost("{decisionId:int}/outcome")]
    public async Task<IActionResult> LogOutcome(int decisionId, [FromBody] LogLegOutcomeRequest request)
    {
        request.UserId = GetUserId();
        request.DecisionId = decisionId;
        return await Mediator.Send(request);
    }
}
