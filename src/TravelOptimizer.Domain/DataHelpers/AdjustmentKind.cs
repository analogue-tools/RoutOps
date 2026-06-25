namespace TravelOptimizer.Domain.DataHelpers;

/// <summary>The kinds of change a Layer 3 reflection may propose.</summary>
public static class AdjustmentKind
{
    public const string PreferenceRule = "preference_rule";
    public const string WeightChange = "weight_change";
    public const string DataSourceFlag = "data_source_flag";
    public const string CorridorNote = "corridor_note";

    public static readonly IReadOnlyList<string> All =
        new[] { PreferenceRule, WeightChange, DataSourceFlag, CorridorNote };
}
