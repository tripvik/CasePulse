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
}
