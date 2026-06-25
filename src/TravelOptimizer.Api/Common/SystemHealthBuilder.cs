using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Api.Jobs;
using TravelOptimizer.Api.Mapping.Travel;
using TravelOptimizer.Persistence;

namespace TravelOptimizer.Api.Common;

/// <summary>
/// Builds the system-health snapshot (job heartbeats + dependency reachability) shared by the
/// /api/health endpoint and the dashboard overview. Reachability checks are best-effort and never throw.
/// </summary>
public static class SystemHealthBuilder
{
    public static async Task<SystemHealthResponse> BuildAsync(
        AppDbContext db,
        JobRunRegistry registry,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        CancellationToken ct)
    {
        bool dbOk = await TryAsync(async () => await db.Database.CanConnectAsync(ct));
        bool tflOk = await TryAsync(() => PingTflAsync(httpFactory, ct));
        bool llmOk = !string.IsNullOrWhiteSpace(config["Travel:OpenAI:ApiKey"]);

        var jobs = registry.All()
            .Select(r => new JobRunResponse(r.Job, r.LastRunUtc, r.Success, r.Note, r.DurationMs))
            .ToList();

        return new SystemHealthResponse(jobs, new ReachabilityResponse(dbOk, tflOk, llmOk), DateTime.UtcNow);
    }

    private static async Task<bool> PingTflAsync(IHttpClientFactory httpFactory, CancellationToken ct)
    {
        using var client = httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(4);
        using var resp = await client.GetAsync("https://api.tfl.gov.uk/Line/Meta/Modes", ct);
        return resp.IsSuccessStatusCode;
    }

    private static async Task<bool> TryAsync(Func<Task<bool>> work)
    {
        try { return await work(); }
        catch { return false; }
    }
}
