using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Api.Mapping.Travel;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Exceptions;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Controllers.Travel;

public record ApproveAdjustmentRequest : IRequest<IActionResult>
{
    [JsonIgnore] public int UserId { get; set; }
    [JsonIgnore] public int AdjustmentId { get; set; }

    /// <summary>true = approve and promote to active; false = reject.</summary>
    public bool Approve { get; init; } = true;
}

public class ApproveAdjustmentValidator : AbstractValidator<ApproveAdjustmentRequest>
{
    public ApproveAdjustmentValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.AdjustmentId).GreaterThan(0);
    }
}

public class ApproveAdjustmentHandler(AppDbContext db, IAdjustmentPromoter promoter)
    : IRequestHandler<ApproveAdjustmentRequest, IActionResult>
{
    public async Task<IActionResult> Handle(ApproveAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var proposal = await db.ProposedAdjustments
            .FirstOrDefaultAsync(a => a.Id == request.AdjustmentId && a.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException($"Adjustment {request.AdjustmentId} not found.");

        if (proposal.Status != AdjustmentStatus.Proposed)
            throw new BadRequestException($"Adjustment {request.AdjustmentId} is already {proposal.Status}.");

        if (request.Approve)
        {
            await promoter.PromoteAsync(proposal, cancellationToken);
        }
        else
        {
            proposal.Status = AdjustmentStatus.Rejected;
            await db.SaveChangesAsync(cancellationToken);
        }

        return new OkObjectResult(proposal.ToResponse());
    }
}
