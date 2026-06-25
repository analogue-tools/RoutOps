using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Models.Travel;

namespace TravelOptimizer.Api.Mapping.Travel;

public static class ItineraryMapper
{
    public static ItineraryResponse ToResponse(this Itinerary it) => new(
        it.UserId,
        it.Date,
        it.TotalPredictedWastedMin,
        it.Legs.Select(l => new ItineraryLegResponse(
            l.Leg.Id,
            l.Leg.FromLabel,
            l.Leg.FromLat,
            l.Leg.FromLng,
            l.Leg.ToLabel,
            l.Leg.ToLat,
            l.Leg.ToLng,
            l.Leg.NotBefore,
            l.Leg.ArriveBy,
            l.Leg.CorridorKey,
            l.Decision is null ? null : l.Decision.ToResponse(l.Predictions),
            MapsLink.ForLeg(l.Leg.FromLat, l.Leg.FromLng, l.Leg.ToLat, l.Leg.ToLng,
                l.Decision?.ChosenMode ?? TravelMode.Tube),
            l.Predictions.Select(p => p.ToResponse()).ToList())).ToList());

    public static DecisionResponse ToResponse(this TravelDecision d, IReadOnlyList<TravelPrediction> options)
    {
        var chosen = options.FirstOrDefault(p => p.Mode == d.ChosenMode);
        var segments = chosen is null ? [] : chosen.Segments.OrderBy(s => s.Order).Select(ToResponse).ToList();
        return new DecisionResponse(
            d.Id,
            d.ChosenMode,
            d.RecommendedDeparture,
            d.PredictedArrival,
            d.PredictedWastedMin,
            d.WasExploration,
            d.PolicyVersion,
            d.Rationale,
            segments);
    }

    public static PredictionResponse ToResponse(this TravelPrediction p) => new(
        p.Mode,
        p.RawDurationMin,
        p.CalibratedDurationMin,
        p.WaitMin,
        p.Confidence,
        p.Feasible,
        p.Rationale,
        p.Segments.OrderBy(s => s.Order).Select(ToResponse).ToList());

    public static SegmentResponse ToResponse(this PredictionSegment s) => new(
        s.Order,
        s.Mode,
        s.DurationMin,
        s.FromLabel,
        s.ToLabel,
        s.Summary,
        s.FromLat,
        s.FromLng,
        s.ToLat,
        s.ToLng);

    public static ProposedAdjustmentResponse ToResponse(this ProposedAdjustment a) => new(
        a.Id,
        a.Kind,
        a.Target,
        a.Change,
        a.Rationale,
        a.ShadowImprovementMin,
        a.Status,
        a.CreatedAt);
}
