using Microsoft.EntityFrameworkCore;
using SmartPendant.MAUIHybrid.Data;

public class SmartPendantDbContext : DbContext
{
    // Define a DbSet for each aggregate root entity.
    // Related entities like TagEntity will be handled via navigation properties.
    public DbSet<ConversationRecordEntity> Conversations { get; set; }
    public DbSet<ActionItemEntity> ActionItems { get; set; }
    public DbSet<TagEntity> Tags { get; set; }
    public DbSet<TopicEntity> Topics { get; set; }
    public DbSet<TranscriptEntryEntity> TranscriptEntries { get; set; }
    public DbSet<TimelineEventEntity> TimelineEvents { get; set; }

    public SmartPendantDbContext(DbContextOptions<SmartPendantDbContext> options)
     : base(options) { }

    // Configure the entity relationships and data model.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureConversationEntity(modelBuilder);
        ConfigureActionItemEntity(modelBuilder);
        ConfigureIndexes(modelBuilder);
    }

    private static void ConfigureConversationEntity(ModelBuilder modelBuilder)
    {
        var conversationEntity = modelBuilder.Entity<ConversationRecordEntity>();

        // Configure properties
        conversationEntity.Property(c => c.Title).HasMaxLength(500);
        conversationEntity.Property(c => c.UserId).HasMaxLength(100);
        conversationEntity.Property(c => c.Location).HasMaxLength(200);
        conversationEntity.Property(c => c.Summary).HasMaxLength(2000);

        // One-to-Many: ConversationRecord -> TranscriptEntries
        conversationEntity
            .HasMany(c => c.Transcript)
            .WithOne(t => t.ConversationRecord)
            .HasForeignKey(t => t.ConversationRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: ConversationRecord -> Tags
        conversationEntity
            .HasMany(c => c.Tags)
            .WithOne(t => t.ConversationRecord)
            .HasForeignKey(t => t.ConversationRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: ConversationRecord -> ActionItems
        conversationEntity
            .HasMany(c => c.ActionItems)
            .WithOne(a => a.ConversationRecord)
            .HasForeignKey(a => a.ConversationRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: ConversationRecord -> Topics
        conversationEntity
            .HasMany(c => c.Topics)
            .WithOne(a => a.ConversationRecord)
            .HasForeignKey(a => a.ConversationRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: ConversationRecord -> TimelineEvents
        conversationEntity
            .HasMany(c => c.Timeline)
            .WithOne(te => te.ConversationRecord)
            .HasForeignKey(te => te.ConversationRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureActionItemEntity(ModelBuilder modelBuilder)
    {
        var actionItemEntity = modelBuilder.Entity<ActionItemEntity>();

        // Configure properties
        actionItemEntity.Property(a => a.Description).HasMaxLength(1000);
        actionItemEntity.Property(a => a.Assignee).HasMaxLength(100);

        // Store the ActionStatus enum as a string in the database
        actionItemEntity
            .Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Add indexes for common query patterns
        modelBuilder.Entity<ConversationRecordEntity>()
            .HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_Conversations_CreatedAt");

        modelBuilder.Entity<ConversationRecordEntity>()
            .HasIndex(c => c.UserId)
            .HasDatabaseName("IX_Conversations_UserId");

        modelBuilder.Entity<ActionItemEntity>()
            .HasIndex(a => a.Status)
            .HasDatabaseName("IX_ActionItems_Status");

        modelBuilder.Entity<ActionItemEntity>()
            .HasIndex(a => a.Assignee)
            .HasDatabaseName("IX_ActionItems_Assignee");

        modelBuilder.Entity<ActionItemEntity>()
            .HasIndex(a => a.DueDate)
            .HasDatabaseName("IX_ActionItems_DueDate");

        modelBuilder.Entity<TagEntity>()
            .HasIndex(t => t.Name)
            .HasDatabaseName("IX_Tags_Name");

        modelBuilder.Entity<TopicEntity>()
            .HasIndex(t => t.Name)
            .HasDatabaseName("IX_Topics_Name");
    }
}