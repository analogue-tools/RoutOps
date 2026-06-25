namespace TravelOptimizer.Domain.DataHelpers;

/// <summary>
/// Well-known <see cref="Entities.Travel.PolicyWeight"/> keys. These weights are data (rows), not
/// constants, which is what lets Layer 3 tune them — but the keys themselves are fixed.
/// </summary>
public static class PolicyKeys
{
    /// <summary>Minutes-equivalent penalty applied per unit of (1 - confidence).</summary>
    public const string RiskWeight = "risk_weight";

    /// <summary>Hard safety margin: predicted arrival must beat eventStart by at least this (min).</summary>
    public const string MinBuffer = "min_buffer";

    /// <summary>Desired slack the optimizer aims for when equally cheap (min).</summary>
    public const string TargetBuffer = "target_buffer";

    /// <summary>Prefix for per-mode preference penalties, e.g. "pref:cycle".</summary>
    public const string PreferencePrefix = "pref:";

    public static string Preference(string mode) => PreferencePrefix + mode;

    /// <summary>Conservative starting weights seeded for every user.</summary>
    public static readonly IReadOnlyDictionary<string, double> Defaults = new Dictionary<string, double>
    {
        [RiskWeight] = 15.0,
        [MinBuffer] = 5.0,
        [TargetBuffer] = 10.0,
        // Composite journeys compete on the same terms by default (no preference penalty); Layer 3
        // can tune this knob up to nudge the user away from multi-transfer routes, or down to favour them.
        [PreferencePrefix + TravelMode.Mixed] = 0.0,
    };
}
