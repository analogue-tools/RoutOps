using System.Collections.Concurrent;

namespace TravelOptimizer.Api.Jobs;

/// <summary>
/// Tiny in-memory ledger of when each background job last ran (and whether it succeeded). Singleton,
/// thread-safe; surfaced by GET /api/health so the dashboard can show the scheduler's heartbeat.
/// </summary>
public class JobRunRegistry
{
    public sealed record JobRun(string Job, DateTime LastRunUtc, bool Success, string? Note, long DurationMs);

    private readonly ConcurrentDictionary<string, JobRun> _runs = new();

    public void Record(string job, bool success, string? note = null, long durationMs = 0) =>
        _runs[job] = new JobRun(job, DateTime.UtcNow, success, note, durationMs);

    public IReadOnlyCollection<JobRun> All() => _runs.Values.OrderBy(r => r.Job).ToList();

    /// <summary>Runs <paramref name="work"/>, timing it and recording success/failure under <paramref name="job"/>.</summary>
    public async Task TrackAsync(string job, Func<Task> work)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await work();
            sw.Stop();
            Record(job, true, durationMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Record(job, false, ex.Message, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
