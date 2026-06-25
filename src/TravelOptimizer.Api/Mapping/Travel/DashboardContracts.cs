namespace TravelOptimizer.Api.Mapping.Travel;

public record SourceHealthResponse(
    string Mode,
    string State,
    double EwmaSuccessRate,
    int ConsecutiveFailures,
    double AvgMape,
    DateTime? LastSuccessAt,
    DateTime? LastFailureAt,
    DateTime? DisabledUntil,
    DateTime UpdatedAt);

public record CorridorSummaryResponse(
    string CorridorKey,
    IReadOnlyList<string> Modes,
    int ModelCount,
    int TotalSampleCount,
    int RecentSampleCount,
    double AvgMape,
    DateTime? LastSampledAt);

public record CorridorSampleResponse(
    string Mode,
    string DayType,
    string HourBucket,
    int PredictedDurationMin,
    int WaitMin,
    double Confidence,
    DateTime SampledAt);

public record JobRunResponse(
    string Job,
    DateTime LastRunUtc,
    bool Success,
    string? Note,
    long DurationMs);

public record ReachabilityResponse(bool Database, bool Tfl, bool Llm);

public record SystemHealthResponse(
    IReadOnlyList<JobRunResponse> Jobs,
    ReachabilityResponse Reachability,
    DateTime ServerTimeUtc);

public record DashboardOverviewResponse(
    DateOnly Date,
    int LegCount,
    int TotalPredictedWastedMin,
    int MixedOptionCount,
    int CorridorsLearned,
    int CorridorSampleCount,
    int PendingAdjustments,
    double ShadowImprovementMin,
    IReadOnlyList<SourceHealthResponse> Sources,
    SystemHealthResponse System);
