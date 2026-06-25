namespace TravelOptimizer.Domain.DataHelpers;

/// <summary>
/// Coarse time-of-day buckets used as part of the corridor learning key. Pools enough samples to
/// be meaningful while still separating peak/off-peak behaviour.
/// </summary>
public static class HourBucket
{
    public const string AmPeak = "am_peak";   // 07:00-09:59 local
    public const string Midday = "midday";    // 10:00-15:59 local
    public const string PmPeak = "pm_peak";   // 16:00-18:59 local
    public const string Evening = "evening";  // 19:00-22:59 local
    public const string Night = "night";      // 23:00-06:59 local

    public static readonly IReadOnlyList<string> All =
        new[] { AmPeak, Midday, PmPeak, Evening, Night };

    /// <summary>Maps a local hour-of-day (0-23) onto its bucket.</summary>
    public static string FromLocalHour(int hour) => hour switch
    {
        >= 7 and < 10 => AmPeak,
        >= 10 and < 16 => Midday,
        >= 16 and < 19 => PmPeak,
        >= 19 and < 23 => Evening,
        _ => Night,
    };
}
