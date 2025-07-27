using Microsoft.EntityFrameworkCore;
using SmartPendant.MAUIHybrid.Abstractions;
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
            // This ensures the database file and schema are created on first use.
            _context.Database.EnsureCreated();
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
                    // For a simple update, we remove the old graph and add the new one.
                    // In a high-performance scenario, you might merge the changes instead.
                    _context.Conversations.Remove(existingEntity);
                    await _context.SaveChangesAsync();
                }

                var newEntity = conversation.ToEntity();
                _context.Conversations.Add(newEntity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB ERROR] SaveConversationAsync: {ex.Message}");
            }
        }

        public async Task SaveConversationsAsync(IEnumerable<ConversationRecord> conversations)
        {
            foreach (var conversation in conversations)
            {
                // This calls the single-save method in a loop.
                // For bulk operations, further optimizations are possible.
                await SaveConversationAsync(conversation);
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
