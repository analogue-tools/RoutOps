using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Domain.Entities;
using TravelOptimizer.Domain.Entities.Travel;
using TravelOptimizer.Persistence.DataInitializers;

namespace TravelOptimizer.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<GoogleCalendarConnection> GoogleCalendarConnections => Set<GoogleCalendarConnection>();

    public DbSet<TravelLeg> TravelLegs => Set<TravelLeg>();
    public DbSet<TravelPrediction> TravelPredictions => Set<TravelPrediction>();
    public DbSet<PredictionSegment> PredictionSegments => Set<PredictionSegment>();
    public DbSet<TravelDecision> TravelDecisions => Set<TravelDecision>();
    public DbSet<LegOutcome> LegOutcomes => Set<LegOutcome>();
    public DbSet<CorridorModel> CorridorModels => Set<CorridorModel>();
    public DbSet<CorridorSample> CorridorSamples => Set<CorridorSample>();
    public DbSet<PolicyWeight> PolicyWeights => Set<PolicyWeight>();
    public DbSet<ProposedAdjustment> ProposedAdjustments => Set<ProposedAdjustment>();
    public DbSet<SourceHealth> SourceHealth => Set<SourceHealth>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        TravelInitializer.Configure(modelBuilder);
    }
}
