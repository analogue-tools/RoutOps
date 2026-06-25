namespace TravelOptimizer.Domain.Interfaces.Travel.Models;

/// <summary>
/// The data packed into the reflection prompt (spec §9 user message): recent decisions + outcomes,
/// every user override (the gold signal), high-MAPE corridors, and modes that keep losing.
/// </summary>
public record ReflectionContext(
    int UserId,
    int LookbackDays,
    IReadOnlyList<ReflectionDecision> Decisions,
    IReadOnlyList<ReflectionCorridor> HighErrorCorridors,
    IReadOnlyList<ReflectionWeight> ActiveWeights);

public record ReflectionDecision(
    int DecisionId,
    string CorridorKey,
    string DayType,
    string HourBucket,
    string ChosenMode,
    int PredictedWastedMin,
    bool WasExploration,
    int? ActualDurationMin,
    bool? ArrivedOnTime,
    bool UserOverrode);

public record ReflectionCorridor(
    string Mode,
    string CorridorKey,
    string DayType,
    string HourBucket,
    double CorrectionFactor,
    double Mape,
    int SampleCount);

public record ReflectionWeight(string Key, double Value);
