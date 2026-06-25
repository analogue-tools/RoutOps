using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TravelOptimizer.Api.Mapping.Travel;
using TravelOptimizer.Domain.Interfaces.Travel;

namespace TravelOptimizer.Api.Controllers.Travel;

public record OptimizeDayRequest : IRequest<IActionResult>
{
    [JsonIgnore] public int UserId { get; set; }
    public DateOnly Date { get; init; }
}

public class OptimizeDayValidator : AbstractValidator<OptimizeDayRequest>
{
    public OptimizeDayValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Date).NotEqual(default(DateOnly));
    }
}

public class OptimizeDayHandler(IItineraryOptimizer optimizer) : IRequestHandler<OptimizeDayRequest, IActionResult>
{
    public async Task<IActionResult> Handle(OptimizeDayRequest request, CancellationToken cancellationToken)
    {
        var itinerary = await optimizer.OptimizeDayAsync(request.UserId, request.Date, cancellationToken);
        return new OkObjectResult(itinerary.ToResponse());
    }
}
