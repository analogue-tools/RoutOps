namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>Bound from configuration ("Travel:SourceHealth"). Tunes the self-healing state machine.</summary>
public class SourceHealthOptions
{
    /// <summary>EWMA smoothing for the success-rate signal (higher = reacts faster).</summary>
    public double Alpha { get; set; } = 0.2;

    /// <summary>healthy → degraded once the EWMA success rate falls below this.</summary>
    public double DegradeBelowSuccessRate { get; set; } = 0.7;

    /// <summary>degraded → disabled once the EWMA success rate falls below this.</summary>
    public double DisableBelowSuccessRate { get; set; } = 0.3;

    /// <summary>degraded → disabled once this many consecutive failures pile up.</summary>
    public int DisableAfterConsecutiveFailures { get; set; } = 5;

    /// <summary>How long a disabled source stays in cooldown before a recovery probe is allowed.</summary>
    public int CooldownMinutes { get; set; } = 15;

    /// <summary>Recovering source needs the EWMA back above this (with no failures) to return healthy.</summary>
    public double RecoverAboveSuccessRate { get; set; } = 0.7;

    /// <summary>Corridor MAPE above this (with enough samples) degrades an otherwise-reachable source.</summary>
    public double HighMapeThreshold { get; set; } = 0.30;
}
