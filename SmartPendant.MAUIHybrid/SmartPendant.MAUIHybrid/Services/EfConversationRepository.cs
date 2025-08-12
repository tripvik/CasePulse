using Microsoft.EntityFrameworkCore;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Data;
using SmartPendant.MAUIHybrid.Helpers;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Services
{
    public class EfConversationRepository : IConversationRepository
    {
        private readonly SmartPendantDbContext _context;

        public EfConversationRepository(SmartPendantDbContext context)
        {
            _context = context;
        }

        #region Conversation Methods

        public async Task SaveConversationAsync(ConversationRecord conversation)
        {
            if (conversation is null) return;

            try
            {
                var existingEntity = await _context.Conversations
                    .Include(c => c.Tags)
                    .Include(c => c.Transcript)
                    .Include(c => c.ActionItems)
                    .Include(c => c.Timeline)
                    .Include(c => c.Topics)
                    .FirstOrDefaultAsync(c => c.Id == conversation.Id);

                if (existingEntity != null)
                {
                    // Update existing entity
                    UpdateExistingEntity(existingEntity, conversation);
                }
                else
                {
                    // Add new entity
                    var newEntity = conversation.ToEntity();
                    _context.Conversations.Add(newEntity);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Use ILogger instead of Debug.WriteLine
                throw new InvalidOperationException($"Failed to save conversation: {ex.Message}", ex);
            }
        }

        public async Task SaveConversationsAsync(IEnumerable<ConversationRecord> conversations)
        {
            if (!conversations.Any()) return;

            try
            {
                // Use a transaction for bulk operations
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                foreach (var conversation in conversations)
                {
                    var existingEntity = await _context.Conversations
                        .Include(c => c.Tags)
                        .Include(c => c.Transcript)
                        .Include(c => c.ActionItems)
                        .Include(c => c.Timeline)
                        .Include(c => c.Topics)
                        .FirstOrDefaultAsync(c => c.Id == conversation.Id);

                    if (existingEntity != null)
                    {
                        UpdateExistingEntity(existingEntity, conversation);
                    }
                    else
                    {
                        var newEntity = conversation.ToEntity();
                        _context.Conversations.Add(newEntity);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save conversations: {ex.Message}", ex);
            }
        }

        private void UpdateExistingEntity(ConversationRecordEntity existingEntity, ConversationRecord conversation)
        {
            // Update scalar properties
            existingEntity.Title = conversation.Title;
            existingEntity.Summary = conversation.Summary;
            existingEntity.DurationMinutes = conversation.DurationMinutes;
            existingEntity.Location = conversation.Location;
            existingEntity.UserId = conversation.UserId;

            // Update collections efficiently
            UpdateTags(existingEntity, conversation.Tags);
            UpdateTopics(existingEntity, conversation.ConversationInsights?.Topics);
            UpdateTranscript(existingEntity, conversation.Transcript);
            UpdateActionItems(existingEntity, conversation.ConversationInsights?.ActionItems);
            UpdateTimeline(existingEntity, conversation.Timeline);
        }

        private void UpdateTags(ConversationRecordEntity entity, List<string>? newTags)
        {
            if (newTags == null) return;

            // Remove tags that are no longer present
            var tagsToRemove = entity.Tags.Where(t => !newTags.Contains(t.Name)).ToList();
            foreach (var tag in tagsToRemove)
            {
                entity.Tags.Remove(tag);
            }

            // Add new tags
            var existingTagNames = entity.Tags.Select(t => t.Name).ToHashSet();
            foreach (var tagName in newTags.Where(t => !existingTagNames.Contains(t)))
            {
                entity.Tags.Add(new TagEntity { Name = tagName });
            }
        }

        private void UpdateTopics(ConversationRecordEntity entity, List<string>? newTopics)
        {
            if (newTopics == null) return;

            // Clear and rebuild topics (simpler for this use case)
            entity.Topics.Clear();
            foreach (var topicName in newTopics)
            {
                entity.Topics.Add(new TopicEntity { Name = topicName });
            }
        }

        private void UpdateTranscript(ConversationRecordEntity entity, List<TranscriptEntry>? newTranscript)
        {
            if (newTranscript == null) return;

            // Clear and rebuild transcript (transcript entries are typically immutable)
            entity.Transcript.Clear();
            foreach (var entry in newTranscript)
            {
                entity.Transcript.Add(entry.ToEntity());
            }
        }

        private void UpdateActionItems(ConversationRecordEntity entity, List<ActionItem>? newActionItems)
        {
            if (newActionItems == null) return;

            // Update existing action items and add new ones
            var existingItems = entity.ActionItems.ToDictionary(a => a.Id);
            var updatedIds = new HashSet<Guid>();

            foreach (var item in newActionItems)
            {
                if (existingItems.TryGetValue(item.TaskId, out var existingItem))
                {
                    // Update existing
                    existingItem.Description = item.Description;
                    existingItem.Status = item.Status;
                    existingItem.Assignee = item.Assignee;
                    existingItem.DueDate = item.DueDate;
                    updatedIds.Add(item.TaskId);
                }
                else
                {
                    // Add new
                    entity.ActionItems.Add(item.ToEntity());
                    updatedIds.Add(item.TaskId);
                }
            }

            // Remove items that are no longer present
            var itemsToRemove = entity.ActionItems.Where(a => !updatedIds.Contains(a.Id)).ToList();
            foreach (var item in itemsToRemove)
            {
                entity.ActionItems.Remove(item);
            }
        }

        private void UpdateTimeline(ConversationRecordEntity entity, List<TimelineEvent>? newTimeline)
        {
            if (newTimeline == null) return;

            // Clear and rebuild timeline (timeline events are typically immutable)
            entity.Timeline.Clear();
            foreach (var timelineEvent in newTimeline)
            {
                entity.Timeline.Add(timelineEvent.ToEntity());
            }
        }

        public async Task<ConversationRecord?> GetConversationAsync(Guid conversationId)
        {
            var entity = await _context.Conversations.AsNoTracking()
                .Include(c => c.Tags)
                .Include(c => c.Transcript)
                .Include(c => c.ActionItems)
                .Include(c => c.Timeline)
                .Include(c => c.Topics)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            return entity?.ToDto();
        }

        public async Task<List<ConversationRecord>> GetAllConversationsAsync()
        {
            var entities = await _context.Conversations.AsNoTracking()
                .Include(c => c.Tags)
                .Include(c => c.Topics)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return entities.Select(e => e.ToDto()).ToList();
        }

        public async Task<List<ConversationRecord>> GetConversationsByDateAsync(DateTime date)
        {
            // This query is highly efficient as it filters in the database.
            var entities = await _context.Conversations.AsNoTracking()
                .Where(c => c.CreatedAt.Date == date.Date)
                .Include(c => c.Transcript)
                .Include(c => c.Tags)
                .Include(c => c.Topics)
                .ToListAsync();

            return entities.Select(e => e.ToDto()).ToList();
        }

        public async Task<List<ConversationRecord>> GetConversationsByTopicAsync(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic)) return new List<ConversationRecord>();

            var entities = await _context.Conversations.AsNoTracking()
                // Efficiently query based on a child collection's property.
                .Where(c => c.Topics.Any(t => t.Name.ToLower() == topic.ToLower()))
                .Include(c => c.Tags)
                .Include(c => c.Topics)
                .ToListAsync();

            return entities.Select(e => e.ToDto()).ToList();
        }

        public async Task<ConversationRecord?> GetConversationByDateAsync(DateTime date)
        {
            var entity = await _context.Conversations.AsNoTracking()
                .Where(c => c.CreatedAt.Date == date.Date)
                .Include(c => c.Tags)
                .Include(c => c.Transcript)
                .Include(c => c.ActionItems)
                .Include(c => c.Timeline)
                .Include(c => c.Topics)
                .OrderBy(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            return entity?.ToDto();
        }

        public async Task DeleteConversationAsync(Guid conversationId)
        {
            var entity = await _context.Conversations.FindAsync(conversationId);
            if (entity != null)
            {
                _context.Conversations.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteConversationsAsync(IEnumerable<Guid> conversationIds)
        {
            var entitiesToDelete = await _context.Conversations
                .Where(c => conversationIds.Contains(c.Id))
                .ToListAsync();

            if (entitiesToDelete.Any())
            {
                _context.Conversations.RemoveRange(entitiesToDelete);
                await _context.SaveChangesAsync();
            }
        }
        #endregion

        #region Task/ActionItem Methods

        public async Task<List<ActionItem>> GetAllTasksAsync()
        {
            var entities = await _context.ActionItems.AsNoTracking()
                .Include(a => a.ConversationRecord) // Include parent for the title
                .OrderBy(a => a.Status)
                .ThenBy(a => a.DueDate)
                .ToListAsync();

            return entities.Select(e => e.ToDto()).ToList();
        }

        public async Task<List<ActionItem>> GetTasksByAssigneeAsync(string assignee)
        {
            if (string.IsNullOrWhiteSpace(assignee)) return new List<ActionItem>();

            var entities = await _context.ActionItems.AsNoTracking()
                .Include(a => a.ConversationRecord)
                .Where(a => a.Assignee != null && a.Assignee.ToLower() == assignee.ToLower())
                .OrderBy(a => a.Status)
                .ThenBy(a => a.DueDate)
                .ToListAsync();

            return entities.Select(e => e.ToDto()).ToList();
        }
        #endregion
    }
}
