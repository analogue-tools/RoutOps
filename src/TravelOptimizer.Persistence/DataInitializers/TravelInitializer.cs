using Microsoft.EntityFrameworkCore;
using TravelOptimizer.Domain.Entities;
using TravelOptimizer.Domain.Entities.Travel;

namespace TravelOptimizer.Persistence.DataInitializers;

/// <summary>
/// All Travel-area schema config (indexes/constraints/FKs) lives here per DATABASE.md, not as
/// annotations on the entities. Called from AppDbContext.OnModelCreating.
/// </summary>
public static class TravelInitializer
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<CalendarEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.StartUtc });
            // upsert key for provider sync (filtered: only externally-sourced events)
            e.HasIndex(x => new { x.UserId, x.ExternalId }).IsUnique()
                .HasFilter("\"ExternalId\" <> ''");
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<GoogleCalendarConnection>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TravelLeg>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TravelPrediction>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TravelLegId);
            e.HasOne(x => x.TravelLeg)
                .WithMany(l => l.Predictions)
                .HasForeignKey(x => x.TravelLegId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PredictionSegment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TravelPredictionId, x.Order });
            e.HasOne(x => x.TravelPrediction)
                .WithMany(p => p.Segments)
                .HasForeignKey(x => x.TravelPredictionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TravelDecision>(e =>
        {
            e.HasKey(x => x.Id);
            // one decision per leg
            e.HasIndex(x => x.TravelLegId).IsUnique();
            e.HasOne(x => x.TravelLeg)
                .WithOne(l => l.Decision!)
                .HasForeignKey<TravelDecision>(x => x.TravelLegId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<LegOutcome>(e =>
        {
            e.HasKey(x => x.Id);
            // one outcome per decision
            e.HasIndex(x => x.TravelDecisionId).IsUnique();
            e.HasOne(x => x.TravelDecision)
                .WithOne(d => d.Outcome!)
                .HasForeignKey<LegOutcome>(x => x.TravelDecisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CorridorModel>(e =>
        {
            e.HasKey(x => x.Id);
            // the learning key — unique so each bucket has exactly one model row
            e.HasIndex(x => new { x.Mode, x.CorridorKey, x.DayType, x.HourBucket }).IsUnique();
        });

        b.Entity<CorridorSample>(e =>
        {
            e.HasKey(x => x.Id);
            // time-series read pattern: latest samples for a corridor/mode
            e.HasIndex(x => new { x.CorridorKey, x.Mode, x.SampledAt });
        });

        b.Entity<SourceHealth>(e =>
        {
            e.HasKey(x => x.Id);
            // one row per mode
            e.HasIndex(x => x.Mode).IsUnique();
        });

        b.Entity<PolicyWeight>(e =>
        {
            e.HasKey(x => x.Id);
            // fast active-weight lookup
            e.HasIndex(x => new { x.UserId, x.Key, x.IsActive });
        });

        b.Entity<ProposedAdjustment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.Status });
        });
    }
}
