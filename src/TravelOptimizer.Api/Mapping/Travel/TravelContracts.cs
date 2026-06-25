namespace TravelOptimizer.Api.Mapping.Travel;

public record SegmentResponse(
    int Order,
    string Mode,
    int DurationMin,
    string FromLabel,
    string ToLabel,
    string Summary,
    double? FromLat,
    double? FromLng,
    double? ToLat,
    double? ToLng);

public record PredictionResponse(
    string Mode,
    int RawDurationMin,
    int CalibratedDurationMin,
    int WaitMin,
    double Confidence,
    bool Feasible,
    string Rationale,
    IReadOnlyList<SegmentResponse> Segments);

public record DecisionResponse(
    int DecisionId,
    string ChosenMode,
    DateTime RecommendedDeparture,
    DateTime PredictedArrival,
    int PredictedWastedMin,
    bool WasExploration,
    int PolicyVersion,
    string Rationale,
    IReadOnlyList<SegmentResponse> Segments);

public record ItineraryLegResponse(
    int LegId,
    string FromLabel,
    double FromLat,
    double FromLng,
    string ToLabel,
    double ToLat,
    double ToLng,
    DateTime NotBefore,
    DateTime ArriveBy,
    string CorridorKey,
    DecisionResponse? Decision,
    string MapsUrl,
    IReadOnlyList<PredictionResponse> Options);

public record ItineraryResponse(
    int UserId,
    DateOnly Date,
    int TotalPredictedWastedMin,
    IReadOnlyList<ItineraryLegResponse> Legs);

public record ProposedAdjustmentResponse(
    int Id,
    string Kind,
    string Target,
    string Change,
    string Rationale,
    double ShadowImprovementMin,
    string Status,
    DateTime CreatedAt);
