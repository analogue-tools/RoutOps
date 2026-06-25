using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Api.Mapping.Travel;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Controllers.Travel;

public record GetProposedAdjustmentsRequest(int UserId, string Status) : IRequest<IActionResult>;

public class GetProposedAdjustmentsValidator : AbstractValidator<GetProposedAdjustmentsRequest>
{
    public GetProposedAdjustmentsValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Status).Must(AdjustmentStatus.All.Contains)
            .WithMessage($"Status must be one of: {string.Join(", ", AdjustmentStatus.All)}.");
    }
}

public class GetProposedAdjustmentsHandler(AppDbContext db)
    : IRequestHandler<GetProposedAdjustmentsRequest, IActionResult>
{
    public async Task<IActionResult> Handle(GetProposedAdjustmentsRequest request, CancellationToken cancellationToken)
    {
        var rows = await db.ProposedAdjustments
            .Where(a => a.UserId == request.UserId && a.Status == request.Status)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return new OkObjectResult(rows.Select(a => a.ToResponse()).ToList());
    }
}
