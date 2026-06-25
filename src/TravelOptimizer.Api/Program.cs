using Coravel;
using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Api;
using TravelOptimizer.Api.Common;
using TravelOptimizer.Api.Jobs;
using TravelOptimizer.Domain.Interfaces.Travel;
using TravelOptimizer.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.RegisterServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Serve the build-free dashboard SPA from wwwroot (index.html as the default document).
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Hourly schedule — each job gates on the user's local hour internally (TieringJob pattern, JOBS.md)
app.Services.UseScheduler(scheduler =>
{
    scheduler.Schedule<OptimizeDayJob>().EveryMinute().PreventOverlapping(nameof(OptimizeDayJob));
    scheduler.Schedule<CalibrationJob>().Hourly();
    scheduler.Schedule<ReflectionJob>().Hourly();
    scheduler.Schedule<CalendarSyncJob>().EveryThirtyMinutes();
    scheduler.Schedule<MonitorJob>().EveryThirtyMinutes();
    scheduler.Schedule<ProbeJob>().Cron("*/20 * * * *").PreventOverlapping(nameof(ProbeJob));
    scheduler.Schedule<HealthJob>().EveryFiveMinutes().PreventOverlapping(nameof(HealthJob));
});

// Best-effort schema sync on startup; the app still boots if the DB is unavailable.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        await TravelOptimizer.Persistence.DataInitializers.TravelSeeder.SeedAsync(db);

        // rehydrate the in-memory source-health state from its durable mirror
        await scope.ServiceProvider.GetRequiredService<ISourceHealthService>().SeedFromDbAsync(CancellationToken.None);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database migration on startup was skipped (DB unavailable?)");
    }
}

// Build today's + tomorrow's itineraries once at boot (fire-and-forget so it doesn't block the
// listener), then the hourly OptimizeDayJob keeps them fresh. Lets the client just GET the result.
app.Lifetime.ApplicationStarted.Register(() => _ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OptimizeDayJob>().Invoke();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Startup itinerary optimize was skipped");
    }
}));

app.Run();
