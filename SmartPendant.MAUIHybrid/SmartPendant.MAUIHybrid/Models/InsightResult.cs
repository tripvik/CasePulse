using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SmartPendant.MAUIHybrid.Models
{
    /// <summary>
    /// Represents the AI-generated insights from a conversation transcript.
    /// </summary>
    public class InsightResult
    {
        /// <summary>
        /// A concise summary of the conversation's main points and outcomes.
        /// </summary>
        [Description("A brief summary of the conversation highlighting key points, decisions, and outcomes in markdown format")]
        public string? Summary { get; set; }

        /// <summary>
        /// A descriptive title that captures the essence of the conversation.
        /// </summary>
        [Description("A clear, concise title that represents the main topic or purpose of the conversation")]
        public string? Title { get; set; }

        /// <summary>
        /// Key topics discussed during the conversation.
        /// </summary>
        [Description("List of main topics, themes, or subjects discussed in the conversation")]
        public List<string>? Topics { get; set; }

        /// <summary>
        /// Action items identified from the conversation.
        /// </summary>
        [Description("Tasks, action items, or follow-ups mentioned or agreed upon during the conversation")]
        public List<ActionItemResult>? ActionItems { get; set; }

        /// <summary>
        /// Timeline of significant events during the conversation.
        /// </summary>
        [Description("Chronological list of important events, decisions, or milestones mentioned in the conversation")]
        public List<TimelineEvent>? Timeline { get; set; }

        /// <summary>
        /// Mappings of diarized speaker labels (e.g., "Guest-1") to actual user identities based on conversation context.
        /// </summary>
        [Description("AI-generated mappings of diarized speaker labels (e.g., 'Guest-1') to actual usernames or identities.")]
        public List<UsernameMapping>? UsernameMappings { get; set; }
    }

    public class ActionItemResult
    {
        /// <summary>
        /// The title of the parent conversation for easy reference.
        /// </summary>
        [Description("Title of the conversation where this action item was identified")]
        public string? ConversationTitle { get; set; }
        /// <summary>
        /// The specific task or action to be completed.
        /// </summary>
        [Description("Clear description of the task or action that needs to be completed")]
        [JsonPropertyName("task")]
        public string? Task { get; set; }

        /// <summary>
        /// Gets or sets the current status of the task. Must be either "Pending" or "Completed".
        /// Use "Completed" only if the task is clearly marked as done; otherwise, default to "Pending".
        /// </summary>
        [Description("Current status of the task. Must be either 'Pending' or 'Completed'.")]
        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ActionStatus Status { get; set; }

        /// <summary>
        /// The person responsible for completing this action.
        /// </summary>
        [Description("Name or identifier of the person assigned to complete this task")]
        [JsonPropertyName("assignee")]
        public string? Assignee { get; set; }

        /// <summary>
        /// The due date for the action item, if specified. Use ISO 8601 format (e.g., 2025-06-24T00:00:00).
        /// If no due date is available, use null. Do not use empty strings or natural language.
        /// </summary>
        [Description("Due date in ISO 8601 format or null if not specified.")]
        [JsonPropertyName("dueDate")]
        public DateTime? DueDate { get; set; }
    }

    public class UsernameMapping
    {
        /// <summary>
        /// The diarized speaker label (e.g., "Guest-1", "Speaker-2") automatically assigned by Azure Speech.
        /// </summary>
        [Description("The diarized speaker label to map, such as 'Guest-1' or 'Speaker-2'.")]
        public string? Label { get; set; }

        /// <summary>
        /// The actual name or username of the person identified from the conversation context.
        /// </summary>
        [Description("The actual username or person name corresponding to the diarized speaker label.")]
        public string? Name { get; set; }
    }
}
