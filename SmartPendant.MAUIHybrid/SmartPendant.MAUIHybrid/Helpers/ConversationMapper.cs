using SmartPendant.MAUIHybrid.Data;
using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Helpers
{
    /// <summary>
    /// A static class to handle mapping between Conversation DTOs and Entities.
    /// </summary>
    public static class ConversationMapper
    {
        // --- To DTO (Entity -> DTO) ---
        // (This part remains the same as before)
        #region To DTO Mappers
        public static ConversationRecord ToDto(this ConversationRecordEntity entity)
        {
            return new ConversationRecord
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Title = entity.Title,
                CreatedAt = entity.CreatedAt,
                DurationMinutes = entity.DurationMinutes,
                AudioFilePath = entity.AudioFilePath,
                Location = entity.Location,
                Summary = entity.Summary,
                Tags = entity.Tags?.Select(t => t.Name).ToList() ?? [],
                Transcript = entity.Transcript?.Select(t => t.ToDto()).ToList() ?? [],
                ConversationInsights = new()
                {
                    ActionItems = entity.ActionItems?.Select(a => a.ToDto()).ToList() ?? [],
                    Topics = entity.Topics?.Select(t => t.Name).ToList() ?? []
                },
                Timeline = entity.Timeline?.Select(t => t.ToDto()).ToList() ?? []
            };
        }

        public static TranscriptEntry ToDto(this TranscriptEntryEntity entity) => new()
        {
            SpeakerId = entity.SpeakerId,
            SpeakerLabel = entity.SpeakerLabel,
            Text = entity.Text,
            Initials = entity.Initials,
            Timestamp = entity.Timestamp
        };

        public static ActionItem ToDto(this ActionItemEntity entity) => new()
        {
            TaskId = entity.Id,
            Description = entity.Description,
            Status = (Models.ActionStatus)entity.Status,
            Assignee = entity.Assignee,
            DueDate = entity.DueDate,
            ConversationId = entity.ConversationRecordId,
            ConversationTitle = entity.ConversationRecord?.Title
        };

        public static TimelineEvent ToDto(this TimelineEventEntity entity) => new()
        {
            Timestamp = entity.Timestamp.ToString(@"mm\:ss"),
            Title = entity.Title,
            Description = entity.Description
        };
        #endregion


        // --- To Entity (DTO -> Entity) ---
        #region To Entity Mappers

        /// <summary>
        /// FIXED: Maps a ConversationRecord DTO to a ConversationRecordEntity.
        /// </summary>
        public static ConversationRecordEntity ToEntity(this ConversationRecord dto)
        {
            return new ConversationRecordEntity
            {
                Id = dto.Id,
                UserId = dto.UserId,
                Title = dto.Title,
                CreatedAt = dto.CreatedAt,
                AudioFilePath = dto.AudioFilePath,
                DurationMinutes = dto.DurationMinutes,
                Location = dto.Location,
                Summary = dto.Summary,
                Tags = dto.Tags?.Select(t => new TagEntity { Name = t }).ToList() ?? [],
                Transcript = dto.Transcript?.Select(t => t.ToEntity()).ToList() ?? [],
                Topics = dto.ConversationInsights?.Topics?.Select(t => new TopicEntity { Name = t }).ToList() ?? [],
                ActionItems = dto.ConversationInsights?.ActionItems?.Select(a => a.ToEntity()).ToList() ?? [],
                Timeline = dto.Timeline?.Select(t => t.ToEntity()).ToList() ?? []
            };
        }

        /// <summary>
        /// NEW: Helper method to map a TimelineEvent DTO to its Entity.
        /// </summary>
        public static TimelineEventEntity ToEntity(this TimelineEvent dto)
        {
            // Safely parse the string timestamp into a TimeSpan.
            TimeSpan.TryParse(dto.Timestamp, out var parsedTimestamp);

            return new TimelineEventEntity
            {
                Title = dto.Title,
                Description = dto.Description,
                Timestamp = parsedTimestamp
            };
        }

        public static TranscriptEntryEntity ToEntity(this TranscriptEntry dto) => new()
        {
            SpeakerId = dto.SpeakerId,
            SpeakerLabel = dto.SpeakerLabel,
            Text = dto.Text,
            Initials = dto.Initials,
            Timestamp = dto.Timestamp
        };

        public static ActionItemEntity ToEntity(this ActionItem dto) => new()
        {
            Id = dto.TaskId,
            Description = dto.Description,
            Status = dto.Status,
            Assignee = dto.Assignee,
            DueDate = dto.DueDate
        };
        #endregion
    }
}
