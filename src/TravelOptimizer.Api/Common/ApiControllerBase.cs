using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TravelOptimizer.Api.Common;

/// <summary>
/// Thin base for area controllers (HANDLERS.md): exposes the mediator and the current user id.
/// In the wider app GetUserId() reads the authenticated principal; here it falls back to a header
/// ("X-User-Id") so the endpoints are exercisable without the full auth stack.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private IMediator? _mediator;
    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected int GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
        if (int.TryParse(claim, out var fromClaim)) return fromClaim;

        if (Request.Headers.TryGetValue("X-User-Id", out var header) && int.TryParse(header, out var fromHeader))
            return fromHeader;

        return 1; // dev fallback — single-user default
    }
}
