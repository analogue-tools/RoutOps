namespace TravelOptimizer.Domain.DataHelpers;

/// <summary>
/// Travel modes the source agents can produce estimates for. String constants per ENTITIES.md
/// (entities never store raw mode strings inline).
/// </summary>
public static class TravelMode
{
    public const string Tube = "tube";
    public const string Bus = "bus";
    public const string Rail = "rail";
    public const string Cycle = "cycle";
    public const string Walk = "walk";

    /// <summary>Composite door-to-door journey combining several modes (e.g. walk → tube → bus).</summary>
    public const string Mixed = "mixed";

    public static readonly IReadOnlyList<string> All = new[] { Tube, Bus, Rail, Cycle, Walk, Mixed };

    public static bool IsValid(string mode) => All.Contains(mode);

    /// <summary>
    /// Maps a TfL leg mode id (as seen in journeys[].legs[].mode.id) onto our mode names. Common ids
    /// are normalised to <see cref="Tube"/>/<see cref="Bus"/>/<see cref="Rail"/>/<see cref="Cycle"/>/
    /// <see cref="Walk"/>; anything we don't recognise is kept verbatim so new TfL modes still render.
    /// </summary>
    public static string FromTflModeId(string tflModeId) => tflModeId switch
    {
        "walking" => Walk,
        "tube" => Tube,
        "bus" => Bus,
        "national-rail" => Rail,
        "overground" => "overground",
        "dlr" => "dlr",
        "cycle" => Cycle,
        _ => tflModeId,
    };
}
