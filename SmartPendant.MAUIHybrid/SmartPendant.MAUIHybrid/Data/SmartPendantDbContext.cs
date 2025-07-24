using Microsoft.EntityFrameworkCore;
using SmartPendant.MAUIHybrid.Data;

public class SmartPendantDbContext : DbContext
{
    // Define a DbSet for each aggregate root entity.
    // Related entities like TagEntity will be handled via navigation properties.
    public DbSet<ConversationRecordEntity> Conversations { get; set; }
    public DbSet<ActionItemEntity> ActionItems { get; set; }

    public SmartPendantDbContext(DbContextOptions<SmartPendantDbContext> options)
     : base(options) { }

    // Configure the entity relationships and data model.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- ConversationRecordEntity Relationships ---

        // One-to-One: ConversationRecord -> ConversationInsights
        modelBuilder.Entity<ConversationRecordEntity>()
            .HasOne(c => c.ConversationInsights)
            .WithOne(i => i.ConversationRecord)
            .HasForeignKey<ConversationInsightsEntity>(i => i.ConversationRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: ConversationRecord -> TranscriptEntries
        modelBuilder.Entity<ConversationRecordEntity>()
            .HasMany(c => c.Transcript)
            .WithOne(t => t.ConversationRecord)
            .HasForeignKey(t => t.ConversationRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: ConversationRecord -> Tags
        modelBuilder.Entity<ConversationRecordEntity>()
            .HasMany(c => c.Tags)
            .WithOne(t => t.ConversationRecord)
            .HasForeignKey(t => t.ConversationRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: ConversationRecord -> ActionItems
        modelBuilder.Entity<ConversationRecordEntity>()
            .HasMany(c => c.ActionItems)
            .WithOne(a => a.ConversationRecord)
            .HasForeignKey(a => a.ConversationRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: ConversationRecord -> TimelineEvents
        modelBuilder.Entity<ConversationRecordEntity>()
            .HasMany(c => c.Timeline)
            .WithOne(te => te.ConversationRecord)
            .HasForeignKey(te => te.ConversationRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- ConversationInsightsEntity Relationships ---

        // One-to-Many: ConversationInsights -> Topics
        modelBuilder.Entity<ConversationInsightsEntity>()
            .HasMany(i => i.Topics)
            .WithOne(t => t.ConversationInsights)
            .HasForeignKey(t => t.ConversationInsightsId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Data Type Conversions ---

        // Store the ActionStatus enum as a string in the database
        modelBuilder.Entity<ActionItemEntity>()
            .Property(a => a.Status)
            .HasConversion<string>();
    }
}