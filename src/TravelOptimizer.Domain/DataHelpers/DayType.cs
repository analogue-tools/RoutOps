namespace TravelOptimizer.Domain.DataHelpers;

/// <summary>Weekday vs Weekend split for the corridor learning key.</summary>
public static class DayType
{
    public const string Weekday = "weekday";
    public const string Weekend = "weekend";

    public static string FromLocalDate(DateOnly date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? Weekend : Weekday;
}
