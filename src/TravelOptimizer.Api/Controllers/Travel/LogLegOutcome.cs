using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Exceptions;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Controllers.Travel;

/// <summary>
/// The actuals tap (spec §3/§7) — the single most important input to the whole learning loop.
/// One arrival tap writes a LegOutcome; we record it and fold it into Layer 1 at once.
/// </summary>
public record LogLegOutcomeRequest : IRequest<IActionResult>
{
    [JsonIgnore] public int UserId { get; set; }
    [JsonIgnore] public int DecisionId { get; set; }

    /// <summary>When the user actually arrived. Defaults to "now" for a thumb-speed tap.</summary>
    public DateTime? ActualArrival { get; init; }

    public string Source { get; init; } = LegOutcomeSource.ManualTap;
}

public class LogLegOutcomeValidator : AbstractValidator<LogLegOutcomeRequest>
{
    public LogLegOutcomeValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.DecisionId).GreaterThan(0);
        RuleFor(x => x.Source).Must(s => s is LegOutcomeSource.ManualTap or LegOutcomeSource.Inferred)
            .WithMessage("Source must be manual_tap or inferred.");
    }
}

public class LogLegOutcomeHandler(AppDbContext db, ICalibrationService calibration)
    : IRequestHandler<LogLegOutcomeRequest, IActionResult>
{
    public async Task<IActionResult> Handle(LogLegOutcomeRequest request, CancellationToken cancellationToken)
    {
        var decision = await db.TravelDecisions
            .Include(d => d.TravelLeg)
            .Include(d => d.Outcome)
            .FirstOrDefaultAsync(d => d.Id == request.DecisionId, cancellationToken)
            ?? throw new NotFoundException($"Decision {request.DecisionId} not found.");

        if (decision.TravelLeg.UserId != request.UserId)
            throw new NotFoundException($"Decision {request.DecisionId} not found.");

        if (decision.Outcome is not null)
            throw new BadRequestException("This leg already has a logged outcome.");

        var actualArrival = request.ActualArrival ?? DateTime.UtcNow;
        var actualDuration = (int)Math.Max(1, Math.Round((actualArrival - decision.RecommendedDeparture).TotalMinutes));

        var outcome = new LegOutcome
        {
            TravelDecisionId = decision.Id,
            ActualArrival = actualArrival,
            ActualDurationMin = actualDuration,
            Source = request.Source,
            ArrivedOnTime = actualArrival <= decision.TravelLeg.ArriveBy,
        };

        db.LegOutcomes.Add(outcome);
        await db.SaveChangesAsync(cancellationToken);

        // fold into Layer 1 immediately (idempotent — the hourly CalibrationJob is the safety net)
        await calibration.IngestOutcomeAsync(outcome);

        return new OkObjectResult(new
        {
            outcome.Id,
            outcome.ActualDurationMin,
            outcome.ArrivedOnTime,
            message = "Outcome recorded — thanks, the agent just learned from this trip.",
        });
    }
}
