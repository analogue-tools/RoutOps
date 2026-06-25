using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Api.Mapping.Travel;
using TravelOptimizer.Domain.Exceptions;
using TravelOptimizer.Domain.Models.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Controllers.Travel;

public record GetItineraryRequest(int UserId, DateOnly Date) : IRequest<IActionResult>;

public class GetItineraryValidator : AbstractValidator<GetItineraryRequest>
{
    public GetItineraryValidator() => RuleFor(x => x.UserId).GreaterThan(0);
}

public class GetItineraryHandler(AppDbContext db) : IRequestHandler<GetItineraryRequest, IActionResult>
{
    public async Task<IActionResult> Handle(GetItineraryRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
                   ?? throw new NotFoundException($"User {request.UserId} not found.");

        var tz = ResolveTimeZone(user.TimeZone);
        var localStart = DateTime.SpecifyKind(request.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(localStart.AddDays(1), tz);

        var legs = await db.TravelLegs
            .Where(l => l.UserId == request.UserId && l.ArriveBy >= fromUtc && l.ArriveBy < toUtc)
            .Include(l => l.Predictions).ThenInclude(p => p.Segments)
            .Include(l => l.Decision)
            .OrderBy(l => l.ArriveBy)
            .ToListAsync(cancellationToken);

        var itinerary = new Itinerary
        {
            UserId = request.UserId,
            Date = request.Date,
            Legs = legs.Select(l => new ItineraryLeg
            {
                Leg = l,
                Decision = l.Decision,
                Predictions = l.Predictions.ToList(),
            }).ToList(),
        };

        return new OkObjectResult(itinerary.ToResponse());
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(id) ? "Europe/London" : id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Europe/London"); }
    }
}
