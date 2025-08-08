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

    // DayRecord entities
    public DbSet<DayRecordEntity> DayRecords { get; set; }
    public DbSet<DayKeyTopicEntity> DayKeyTopics { get; set; }
    public DbSet<DayKeyDecisionEntity> DayKeyDecisions { get; set; }
    public DbSet<DayImportantMomentEntity> DayImportantMoments { get; set; }
    public DbSet<DayLearningInsightEntity> DayLearningsInsights { get; set; }
    public DbSet<DayKeyAccomplishmentEntity> DayKeyAccomplishments { get; set; }
    public DbSet<DayImportantDecisionEntity> DayImportantDecisions { get; set; }
    public DbSet<DayPeopleHighlightEntity> DayPeopleHighlights { get; set; }
    public DbSet<DayLearningReflectionEntity> DayLearningsReflections { get; set; }
    public DbSet<DayTomorrowPreparationEntity> DayTomorrowPreparations { get; set; }
    public DbSet<PersonInteractionEntity> PersonInteractions { get; set; }
    public DbSet<PersonTopicEntity> PersonTopics { get; set; }
    public DbSet<PersonConversationTitleEntity> PersonConversationTitles { get; set; }
    public DbSet<LocationActivityEntity> LocationActivities { get; set; }
    public DbSet<LocationTopicEntity> LocationTopics { get; set; }

    public SmartPendantDbContext(DbContextOptions<SmartPendantDbContext> options)
     : base(options) { }

    // Configure the entity relationships and data model.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureConversationEntity(modelBuilder);
        ConfigureActionItemEntity(modelBuilder);
        ConfigureDayRecordEntity(modelBuilder);
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

    private static void ConfigureDayRecordEntity(ModelBuilder modelBuilder)
    {
        var dayRecordEntity = modelBuilder.Entity<DayRecordEntity>();

        // Configure properties
        dayRecordEntity.Property(d => d.DailySummary).HasMaxLength(4000);
        dayRecordEntity.Property(d => d.MoodAnalysis).HasMaxLength(1000);
        dayRecordEntity.Property(d => d.ExecutiveSummary).HasMaxLength(4000);
        dayRecordEntity.Property(d => d.PersonalReflection).HasMaxLength(2000);
        dayRecordEntity.Property(d => d.MostActiveLocation).HasMaxLength(200);
        dayRecordEntity.Property(d => d.LongestConversationTitle).HasMaxLength(500);

        // Configure relationships for DayRecord collections
        dayRecordEntity
            .HasMany(d => d.KeyTopics)
            .WithOne(kt => kt.DayRecord)
            .HasForeignKey(kt => kt.DayRecordDate)
            .OnDelete(DeleteBehavior.Cascade);

        dayRecordEntity
            .HasMany(d => d.KeyDecisions)
            .WithOne(kd => kd.DayRecord)
            .HasForeignKey(kd => kd.DayRecordDate)
            .OnDelete(DeleteBehavior.Cascade);

        dayRecordEntity
            .HasMany(d => d.ImportantMoments)
            .WithOne(im => im.DayRecord)
            .HasForeignKey(im => im.DayRecordDate)
            .OnDelete(DeleteBehavior.Cascade);

        dayRecordEntity
            .HasMany(d => d.LearningsInsights)
            .WithOne(li => li.DayRecord)
            .HasForeignKey(li => li.DayRecordDate)
            .OnDelete(DeleteBehavior.Cascade);

        dayRecordEntity
            .HasMany(d => d.KeyAccomplishments)
            .WithOne(ka => ka.DayRecord)
            .HasForeignKey(ka => ka.DayRecordDate)
            .OnDelete(DeleteBehavior.Cascade);

        dayRecordEntity
            .HasMany(d => d.ImportantDecisions)
            .WithOne(id => id.DayRecord)
            .HasForeignKey(id => id.DayRecordDate)
            .OnDelete(DeleteBehavior.Cascade);

        dayRecordEntity
            .HasMany(d => d.PeopleHighlights)
            .WithOne(ph => ph.DayRecord)
            .HasForeignKey(ph => ph.DayRecordDate)
            .OnDelete(DeleteBehavior.Cascade);

        dayRecordEntity
            .HasMany(d => d.LearningsReflections)
            .WithOne(lr => lr.DayRecord)
            .HasForeignKey(lr => lr.DayRecordDate)
            .OnDelete(DeleteBehavior.Cascade);

        dayRecordEntity
            .HasMany(d => d.TomorrowPreparations)
            .WithOne(tp => tp.DayRecord)
            .HasForeignKey(tp => tp.DayRecordDate)
            .OnDelete(DeleteBehavior.Cascade);

        dayRecordEntity
            .HasMany(d => d.PeopleInteracted)
            .WithOne(pi => pi.DayRecord)
            .HasForeignKey(pi => pi.DayRecordDate)
            .OnDelete(DeleteBehavior.Cascade);

        dayRecordEntity
            .HasMany(d => d.LocationActivities)
            .WithOne(la => la.DayRecord)
            .HasForeignKey(la => la.DayRecordDate)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure PersonInteraction relationships
        var personInteractionEntity = modelBuilder.Entity<PersonInteractionEntity>();
        personInteractionEntity.Property(pi => pi.PersonName).HasMaxLength(200);
        personInteractionEntity.Property(pi => pi.PersonInitials).HasMaxLength(10);

        personInteractionEntity
            .HasMany(pi => pi.TopicsDiscussed)
            .WithOne(pt => pt.PersonInteraction)
            .HasForeignKey(pt => pt.PersonInteractionId)
            .OnDelete(DeleteBehavior.Cascade);

        personInteractionEntity
            .HasMany(pi => pi.ConversationTitles)
            .WithOne(pct => pct.PersonInteraction)
            .HasForeignKey(pct => pct.PersonInteractionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure LocationActivity relationships
        var locationActivityEntity = modelBuilder.Entity<LocationActivityEntity>();
        locationActivityEntity.Property(la => la.Location).HasMaxLength(200);

        locationActivityEntity
            .HasMany(la => la.Topics)
            .WithOne(lt => lt.LocationActivity)
            .HasForeignKey(lt => lt.LocationActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure text field lengths for string collections
        modelBuilder.Entity<DayKeyTopicEntity>().Property(x => x.Topic).HasMaxLength(500);
        modelBuilder.Entity<DayKeyDecisionEntity>().Property(x => x.Decision).HasMaxLength(1000);
        modelBuilder.Entity<DayImportantMomentEntity>().Property(x => x.Moment).HasMaxLength(1000);
        modelBuilder.Entity<DayLearningInsightEntity>().Property(x => x.Learning).HasMaxLength(1000);
        modelBuilder.Entity<DayKeyAccomplishmentEntity>().Property(x => x.Accomplishment).HasMaxLength(1000);
        modelBuilder.Entity<DayImportantDecisionEntity>().Property(x => x.Decision).HasMaxLength(1000);
        modelBuilder.Entity<DayPeopleHighlightEntity>().Property(x => x.Highlight).HasMaxLength(1000);
        modelBuilder.Entity<DayLearningReflectionEntity>().Property(x => x.Reflection).HasMaxLength(1000);
        modelBuilder.Entity<DayTomorrowPreparationEntity>().Property(x => x.Preparation).HasMaxLength(1000);
        modelBuilder.Entity<PersonTopicEntity>().Property(x => x.Topic).HasMaxLength(500);
        modelBuilder.Entity<PersonConversationTitleEntity>().Property(x => x.Title).HasMaxLength(500);
        modelBuilder.Entity<LocationTopicEntity>().Property(x => x.Topic).HasMaxLength(500);
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

        // DayRecord indexes
        modelBuilder.Entity<DayRecordEntity>()
            .HasIndex(d => d.Date)
            .HasDatabaseName("IX_DayRecords_Date");

        modelBuilder.Entity<PersonInteractionEntity>()
            .HasIndex(pi => pi.PersonName)
            .HasDatabaseName("IX_PersonInteractions_PersonName");

        modelBuilder.Entity<LocationActivityEntity>()
            .HasIndex(la => la.Location)
            .HasDatabaseName("IX_LocationActivities_Location");
    }
}