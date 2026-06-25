using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TravelOptimizer.Domain.DataHelpers;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Domain.Interfaces.Travel.Models;

namespace TravelOptimizer.Persistence.Services.Travel;

/// <summary>
/// Singleton, in-memory implementation of the source-health state machine. Every agent call folds a
/// single EWMA step in here, so reads (and the optimizer's skip/down-weight decisions) are O(1) and
/// allocation-free. Transitions are applied inline on success/failure; HealthJob mirrors the result
/// to the durable <see cref="SourceHealth"/> table and feeds in corridor MAPE.
/// </summary>
public class SourceHealthState(IOptions<SourceHealthOptions> options) : ISourceHealthState
{
    private readonly SourceHealthOptions _opts = options.Value;
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    private sealed class Entry
    {
        public string State = SourceHealthStatus.Healthy;
        public double Ewma = 1.0;
        public int ConsecutiveFailures;
        public double AvgMape;
        public DateTime? LastSuccessAt;
        public DateTime? LastFailureAt;
        public DateTime? DisabledUntil;
        public DateTime UpdatedAt = DateTime.UtcNow;
        public readonly object Gate = new();
    }

    private Entry GetEntry(string mode) => _entries.GetOrAdd(mode, _ => new Entry());

    public void RecordSuccess(string mode)
    {
        var e = GetEntry(mode);
        lock (e.Gate)
        {
            var now = DateTime.UtcNow;
            e.Ewma = (1 - _opts.Alpha) * e.Ewma + _opts.Alpha * 1.0;
            e.ConsecutiveFailures = 0;
            e.LastSuccessAt = now;
            e.UpdatedAt = now;

            // recovery path: a disabled source whose cooldown elapsed gets a probe; success on it
            // moves it to degraded, then back to healthy once the EWMA has recovered.
            if (e.State == SourceHealthStatus.Disabled)
            {
                if (e.DisabledUntil is null || e.DisabledUntil <= now)
                {
                    e.State = SourceHealthStatus.Degraded;
                    e.DisabledUntil = null;
                }
            }
            else if (e.State == SourceHealthStatus.Degraded && e.Ewma >= _opts.RecoverAboveSuccessRate)
            {
                e.State = SourceHealthStatus.Healthy;
            }
        }
    }

    public void RecordFailure(string mode)
    {
        var e = GetEntry(mode);
        lock (e.Gate)
        {
            var now = DateTime.UtcNow;
            e.Ewma = (1 - _opts.Alpha) * e.Ewma + _opts.Alpha * 0.0;
            e.ConsecutiveFailures += 1;
            e.LastFailureAt = now;
            e.UpdatedAt = now;
            ApplyDegradeOrDisable(e, now);
        }
    }

    public void RecordMape(string mode, double avgMape)
    {
        var e = GetEntry(mode);
        lock (e.Gate)
        {
            e.AvgMape = avgMape;
            e.UpdatedAt = DateTime.UtcNow;
            // systematically wrong (not just unreachable): degrade a still-healthy source.
            if (e.State == SourceHealthStatus.Healthy && avgMape > _opts.HighMapeThreshold)
                e.State = SourceHealthStatus.Degraded;
        }
    }

    private void ApplyDegradeOrDisable(Entry e, DateTime now)
    {
        bool shouldDisable = e.Ewma < _opts.DisableBelowSuccessRate
                             || e.ConsecutiveFailures >= _opts.DisableAfterConsecutiveFailures;
        if (shouldDisable)
        {
            e.State = SourceHealthStatus.Disabled;
            e.DisabledUntil = now.AddMinutes(_opts.CooldownMinutes);
            return;
        }

        if (e.State == SourceHealthStatus.Healthy && e.Ewma < _opts.DegradeBelowSuccessRate)
            e.State = SourceHealthStatus.Degraded;
    }

    public string GetState(string mode) =>
        _entries.TryGetValue(mode, out var e) ? e.State : SourceHealthStatus.Healthy;

    public bool IsDisabled(string mode, out DateTime? disabledUntil)
    {
        disabledUntil = null;
        if (!_entries.TryGetValue(mode, out var e)) return false;
        lock (e.Gate)
        {
            disabledUntil = e.DisabledUntil;
            return e.State == SourceHealthStatus.Disabled;
        }
    }

    public SourceHealthSnapshot Get(string mode) => Snapshot(mode, GetEntry(mode));

    public IReadOnlyCollection<SourceHealthSnapshot> All() =>
        _entries.Select(kv => Snapshot(kv.Key, kv.Value)).ToList();

    public void Seed(IEnumerable<SourceHealth> rows)
    {
        foreach (var r in rows)
        {
            var e = GetEntry(r.Mode);
            lock (e.Gate)
            {
                e.State = string.IsNullOrWhiteSpace(r.State) ? SourceHealthStatus.Healthy : r.State;
                e.Ewma = r.EwmaSuccessRate;
                e.ConsecutiveFailures = r.ConsecutiveFailures;
                e.AvgMape = r.AvgMape;
                e.LastSuccessAt = r.LastSuccessAt;
                e.LastFailureAt = r.LastFailureAt;
                e.DisabledUntil = r.DisabledUntil;
                e.UpdatedAt = r.UpdatedAt;
            }
        }
    }

    private static SourceHealthSnapshot Snapshot(string mode, Entry e)
    {
        lock (e.Gate)
        {
            return new SourceHealthSnapshot(
                mode, e.State, e.Ewma, e.ConsecutiveFailures, e.AvgMape,
                e.LastSuccessAt, e.LastFailureAt, e.DisabledUntil, e.UpdatedAt);
        }
    }
}
