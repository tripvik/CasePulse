using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Abstractions
{
    /// <summary>
    /// Defines the contract for a service that handles the persistence
    /// of conversation data and related entities.
    /// </summary>
    public interface IConversationService
    {
        #region Conversation Methods
        /// <summary>
        /// Saves a single conversation.
        /// </summary>
        /// <param name="conversation">The conversation object to save.</param>
        Task SaveConversationAsync(ConversationModel conversation);

        /// <summary>
        /// Saves a collection of conversations.
        /// </summary>
        /// <param name="conversations">The collection of conversations to save.</param>
        Task SaveConversationsAsync(IEnumerable<ConversationModel> conversations);

        /// <summary>
        /// Retrieves a single conversation by its unique ID.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to retrieve.</param>
        /// <returns>The conversation object, or null if not found.</returns>
        Task<ConversationModel?> GetConversationAsync(Guid conversationId);

        /// <summary>
        /// Retrieves all saved conversations.
        /// </summary>
        /// <returns>A list of all conversation objects.</returns>
        Task<List<ConversationModel>> GetAllConversationsAsync();

        /// <summary>
        /// Retrieves all conversations recorded on a specific date.
        /// </summary>
        /// <param name="date">The date to filter conversations by.</param>
        /// <returns>A list of conversations from the specified date.</returns>
        Task<List<ConversationModel>> GetConversationsByDateAsync(DateTime date);

        /// <summary>
        /// Retrieves all conversations that contain a specific topic in their insights.
        /// </summary>
        /// <param name="topic">The topic to search for.</param>
        /// <returns>A list of conversations related to the specified topic.</returns>
        Task<List<ConversationModel>> GetConversationsByTopicAsync(string topic);

        /// <summary>
        /// Deletes a single conversation by its ID.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to delete.</param>
        Task DeleteConversationAsync(Guid conversationId);

        /// <summary>
        /// Deletes multiple conversations by their IDs.
        /// </summary>
        /// <param name="conversationIds">The collection of conversation IDs to delete.</param>
        Task DeleteConversationsAsync(IEnumerable<Guid> conversationIds);
        #endregion

        #region Task/ActionItem Methods
        /// <summary>
        /// Retrieves all action items from all conversations.
        /// </summary>
        /// <returns>A list of all action items.</returns>
        Task<List<ActionItem>> GetAllTasksAsync();

        /// <summary>
        /// Retrieves all action items assigned to a specific person.
        /// </summary>
        /// <param name="assignee">The name of the assignee.</param>
        /// <returns>A list of tasks assigned to the specified person.</returns>
        Task<List<ActionItem>> GetTasksByAssigneeAsync(string assignee);
        #endregion
    }
}