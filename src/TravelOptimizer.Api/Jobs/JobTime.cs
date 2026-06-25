using TravelOptimizer.Domain.Entities;

namespace TravelOptimizer.Api.Jobs;

/// <summary>
/// Shared local-time helper for the hourly jobs. Each job runs hourly and acts only when the user's
/// local hour matches its target (the TieringJob pattern, JOBS.md).
/// </summary>
public static class JobTime
{
    public static TimeZoneInfo Zone(User user)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(user.TimeZone) ? "Europe/London" : user.TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        }
    }

    public static DateTime LocalNow(User user) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone(user));

    public static int LocalHour(User user) => LocalNow(user).Hour;

    public static DateOnly LocalToday(User user) => DateOnly.FromDateTime(LocalNow(user));
}
