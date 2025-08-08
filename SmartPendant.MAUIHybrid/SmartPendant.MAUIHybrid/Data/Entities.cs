using SmartPendant.MAUIHybrid.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartPendant.MAUIHybrid.Data
{
    /// <summary>
    /// Represents a conversation record in the database.
    /// </summary>
    [Table("Conversations")]
    public class ConversationRecordEntity
    {
        [Key]
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public double DurationMinutes { get; set; }
        public string? AudioFilePath { get; set; }
        public string? Location { get; set; }
        public string? Summary { get; set; }
        public List<TranscriptEntryEntity> Transcript { get; set; } = [];
        public List<TimelineEventEntity> Timeline { get; set; } = [];
        public List<ActionItemEntity> ActionItems { get; set; } = [];
        public List<TopicEntity> Topics { get; set; } = [];
        public List<TagEntity> Tags { get; set; } = [];
    }

    [Table("Tags")]
    public class TagEntity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public Guid ConversationRecordId { get; set; }
        public ConversationRecordEntity ConversationRecord { get; set; } = null!;
    }

    [Table("TranscriptEntries")]
    public class TranscriptEntryEntity
    {
        [Key]
        public int Id { get; set; }
        public string SpeakerId { get; set; } = string.Empty;
        public string? SpeakerLabel { get; set; }
        public string? Text { get; set; }
        public string? Initials { get; set; }
        public DateTime Timestamp { get; set; }

        public Guid ConversationRecordId { get; set; }
        public ConversationRecordEntity ConversationRecord { get; set; } = null!;
    }

    [Table("ActionItems")]
    public class ActionItemEntity
    {
        [Key]
        public Guid Id { get; set; }
        public string? Description { get; set; }
        public ActionStatus Status { get; set; }
        public string? Assignee { get; set; }
        public DateTime? DueDate { get; set; }

        public Guid ConversationRecordId { get; set; }
        public ConversationRecordEntity ConversationRecord { get; set; } = null!;
    }

    [Table("TimelineEvents")]
    public class TimelineEventEntity
    {
        [Key]
        public int Id { get; set; }
        public TimeSpan Timestamp { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }

        public Guid ConversationRecordId { get; set; }
        public ConversationRecordEntity ConversationRecord { get; set; } = null!;
    }

    [Table("Topics")]
    public class TopicEntity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public Guid ConversationRecordId { get; set; }
        public ConversationRecordEntity ConversationRecord { get; set; } = null!;
    }

    #region DayRecord Entities

    /// <summary>
    /// Represents a day record in the database.
    /// </summary>
    [Table("DayRecords")]
    public class DayRecordEntity
    {
        [Key]
        public DateTime Date { get; set; }
        public string? DailySummary { get; set; }
        public string? MoodAnalysis { get; set; }
        public string? ExecutiveSummary { get; set; }
        public string? PersonalReflection { get; set; }
        public double TotalTalkTimeMinutes { get; set; }
        public int TotalConversations { get; set; }
        public int UniqueLocations { get; set; }
        public int UniquePeople { get; set; }
        public double AverageConversationLength { get; set; }
        public string? MostActiveLocation { get; set; }
        public string? LongestConversationTitle { get; set; }
        public TimeSpan FirstConversation { get; set; }
        public TimeSpan LastConversation { get; set; }

        // Navigation properties
        public List<DayKeyTopicEntity> KeyTopics { get; set; } = [];
        public List<DayKeyDecisionEntity> KeyDecisions { get; set; } = [];
        public List<DayImportantMomentEntity> ImportantMoments { get; set; } = [];
        public List<DayLearningInsightEntity> LearningsInsights { get; set; } = [];
        public List<DayKeyAccomplishmentEntity> KeyAccomplishments { get; set; } = [];
        public List<DayImportantDecisionEntity> ImportantDecisions { get; set; } = [];
        public List<DayPeopleHighlightEntity> PeopleHighlights { get; set; } = [];
        public List<DayLearningReflectionEntity> LearningsReflections { get; set; } = [];
        public List<DayTomorrowPreparationEntity> TomorrowPreparations { get; set; } = [];
        public List<PersonInteractionEntity> PeopleInteracted { get; set; } = [];
        public List<LocationActivityEntity> LocationActivities { get; set; } = [];
    }

    [Table("DayKeyTopics")]
    public class DayKeyTopicEntity
    {
        [Key]
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;

        public DateTime DayRecordDate { get; set; }
        public DayRecordEntity DayRecord { get; set; } = null!;
    }

    [Table("DayKeyDecisions")]
    public class DayKeyDecisionEntity
    {
        [Key]
        public int Id { get; set; }
        public string Decision { get; set; } = string.Empty;

        public DateTime DayRecordDate { get; set; }
        public DayRecordEntity DayRecord { get; set; } = null!;
    }

    [Table("DayImportantMoments")]
    public class DayImportantMomentEntity
    {
        [Key]
        public int Id { get; set; }
        public string Moment { get; set; } = string.Empty;

        public DateTime DayRecordDate { get; set; }
        public DayRecordEntity DayRecord { get; set; } = null!;
    }

    [Table("DayLearningsInsights")]
    public class DayLearningInsightEntity
    {
        [Key]
        public int Id { get; set; }
        public string Learning { get; set; } = string.Empty;

        public DateTime DayRecordDate { get; set; }
        public DayRecordEntity DayRecord { get; set; } = null!;
    }

    [Table("DayKeyAccomplishments")]
    public class DayKeyAccomplishmentEntity
    {
        [Key]
        public int Id { get; set; }
        public string Accomplishment { get; set; } = string.Empty;

        public DateTime DayRecordDate { get; set; }
        public DayRecordEntity DayRecord { get; set; } = null!;
    }

    [Table("DayImportantDecisions")]
    public class DayImportantDecisionEntity
    {
        [Key]
        public int Id { get; set; }
        public string Decision { get; set; } = string.Empty;

        public DateTime DayRecordDate { get; set; }
        public DayRecordEntity DayRecord { get; set; } = null!;
    }

    [Table("DayPeopleHighlights")]
    public class DayPeopleHighlightEntity
    {
        [Key]
        public int Id { get; set; }
        public string Highlight { get; set; } = string.Empty;

        public DateTime DayRecordDate { get; set; }
        public DayRecordEntity DayRecord { get; set; } = null!;
    }

    [Table("DayLearningsReflections")]
    public class DayLearningReflectionEntity
    {
        [Key]
        public int Id { get; set; }
        public string Reflection { get; set; } = string.Empty;

        public DateTime DayRecordDate { get; set; }
        public DayRecordEntity DayRecord { get; set; } = null!;
    }

    [Table("DayTomorrowPreparations")]
    public class DayTomorrowPreparationEntity
    {
        [Key]
        public int Id { get; set; }
        public string Preparation { get; set; } = string.Empty;

        public DateTime DayRecordDate { get; set; }
        public DayRecordEntity DayRecord { get; set; } = null!;
    }

    [Table("PersonInteractions")]
    public class PersonInteractionEntity
    {
        [Key]
        public int Id { get; set; }
        public string? PersonName { get; set; }
        public string? PersonInitials { get; set; }
        public int InteractionCount { get; set; }
        public double TotalTimeMinutes { get; set; }

        public DateTime DayRecordDate { get; set; }
        public DayRecordEntity DayRecord { get; set; } = null!;

        // Navigation properties for collections
        public List<PersonTopicEntity> TopicsDiscussed { get; set; } = [];
        public List<PersonConversationTitleEntity> ConversationTitles { get; set; } = [];
    }

    [Table("PersonTopics")]
    public class PersonTopicEntity
    {
        [Key]
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;

        public int PersonInteractionId { get; set; }
        public PersonInteractionEntity PersonInteraction { get; set; } = null!;
    }

    [Table("PersonConversationTitles")]
    public class PersonConversationTitleEntity
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        public int PersonInteractionId { get; set; }
        public PersonInteractionEntity PersonInteraction { get; set; } = null!;
    }

    [Table("LocationActivities")]
    public class LocationActivityEntity
    {
        [Key]
        public int Id { get; set; }
        public string? Location { get; set; }
        public int ConversationCount { get; set; }
        public double TotalTimeMinutes { get; set; }
        public TimeSpan FirstActivity { get; set; }
        public TimeSpan LastActivity { get; set; }

        public DateTime DayRecordDate { get; set; }
        public DayRecordEntity DayRecord { get; set; } = null!;

        // Navigation properties for collections
        public List<LocationTopicEntity> Topics { get; set; } = [];
    }

    [Table("LocationTopics")]
    public class LocationTopicEntity
    {
        [Key]
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;

        public int LocationActivityId { get; set; }
        public LocationActivityEntity LocationActivity { get; set; } = null!;
    }

    #endregion
}
