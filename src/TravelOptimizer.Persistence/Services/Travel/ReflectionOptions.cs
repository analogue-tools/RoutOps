namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>Bound from configuration ("Travel:Reflection"). Controls the Layer 3 gate (spec §11.2).</summary>
public class ReflectionOptions
{
    public int LookbackDays { get; set; } = 14;

    /// <summary>Backtest improvement (min/day) a low-risk proposal must clear to auto-promote.</summary>
    public double AutoPromoteThresholdMinPerDay { get; set; } = 3.0;

    /// <summary>Corridors above this MAPE are surfaced to the LLM as systematic errors.</summary>
    public double HighMapeThreshold { get; set; } = 0.25;

    public int MinCorridorSamples { get; set; } = 5;
}
