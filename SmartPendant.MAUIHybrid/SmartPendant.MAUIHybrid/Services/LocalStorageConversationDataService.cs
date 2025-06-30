using Blazored.LocalStorage;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Services
{
    /// <summary>
    /// An implementation of IConversationService that uses LocalStorage
    /// for client-side data persistence.
    /// </summary>
    public class LocalStorageConversationDataService : IConversationDataService
    {
        private readonly ILocalStorageService _localStorage;
        private const string ConversationListKey = "conversation_list";

        public LocalStorageConversationDataService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        #region Conversation Methods
        public async Task SaveConversationAsync(ConversationModel conversation)
        {
            if (conversation is null) return;
            try
            {
                var conversationIds = await GetConversationIdListAsync();
                if (!conversationIds.Contains(conversation.Id))
                {
                    conversationIds.Add(conversation.Id);
                    await _localStorage.SetItemAsync(ConversationListKey, conversationIds);
                }
                await _localStorage.SetItemAsync(conversation.Id.ToString(), conversation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving conversation {conversation.Id}: {ex.Message}");
            }
        }

        public async Task SaveConversationsAsync(IEnumerable<ConversationModel> conversations)
        {
            foreach (var conversation in conversations)
            {
                await SaveConversationAsync(conversation);
            }
        }

        public async Task<ConversationModel?> GetConversationAsync(Guid conversationId)
        {
            try
            {
                return await _localStorage.GetItemAsync<ConversationModel>(conversationId.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting conversation {conversationId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<ConversationModel>> GetAllConversationsAsync()
        {
            var conversations = new List<ConversationModel>();
            var conversationIds = await GetConversationIdListAsync();
            foreach (var id in conversationIds)
            {
                var conversation = await GetConversationAsync(id);
                if (conversation != null)
                {
                    conversations.Add(conversation);
                }
            }
            return conversations;
        }

        public async Task<List<ConversationModel>> GetConversationsByDateAsync(DateTime date)
        {
            var allConversations = await GetAllConversationsAsync();
            return allConversations.Where(c => c.CreatedAt.Date == date.Date).ToList();
        }

        public async Task<List<ConversationModel>> GetConversationsByTopicAsync(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic)) return new List<ConversationModel>();
            var allConversations = await GetAllConversationsAsync();
            return allConversations
                .Where(c => c.ConversationInsights?.Topics?.Contains(topic, StringComparer.OrdinalIgnoreCase) ?? false)
                .ToList();
        }

        public async Task<ConversationModel?> GetConversationByDateAsync(DateTime date)
        {
            var allConversations = await GetAllConversationsAsync();
            return allConversations
                .Where(c => c.CreatedAt.Date == date.Date)
                .OrderBy(c => c.CreatedAt)
                .FirstOrDefault();
        }

        public async Task DeleteConversationAsync(Guid conversationId)
        {
            try
            {
                var conversationIds = await GetConversationIdListAsync();
                if (conversationIds.Remove(conversationId))
                {
                    await _localStorage.SetItemAsync(ConversationListKey, conversationIds);
                }
                await _localStorage.RemoveItemAsync(conversationId.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting conversation {conversationId}: {ex.Message}");
            }
        }

        public async Task DeleteConversationsAsync(IEnumerable<Guid> conversationIds)
        {
            foreach (var id in conversationIds)
            {
                await DeleteConversationAsync(id);
            }
        }
        #endregion

        #region Task/ActionItem Methods
        public async Task<List<ActionItem>> GetAllTasksAsync()
        {
            var allConversations = await GetAllConversationsAsync();
            return allConversations
                .SelectMany(c => c.ConversationInsights?.ActionItems ?? Enumerable.Empty<ActionItem>())
                .ToList();
        }

        public async Task<List<ActionItem>> GetTasksByAssigneeAsync(string assignee)
        {
            if (string.IsNullOrWhiteSpace(assignee)) return new List<ActionItem>();
            var allTasks = await GetAllTasksAsync();
            return allTasks
                .Where(t => string.Equals(t.Assignee, assignee, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        #endregion

        #region Private Helper Methods
        private async Task<List<Guid>> GetConversationIdListAsync()
        {
            try
            {
                return await _localStorage.GetItemAsync<List<Guid>>(ConversationListKey) ?? new List<Guid>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving conversation ID list: {ex.Message}");
                return new List<Guid>(); // Return empty list on error to prevent crashing.
            }
        }
        #endregion
    }
}
