using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SmartPendant.MAUIHybrid.Models
{
    /// <summary>
    /// Represents the main data model for a single conversation record.
    /// This model is designed to hold all necessary information for display after being processed by an LLM.
    /// </summary>
    public class Conversation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? UserId { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public double DurationMinutes { get; set; }

        /// <summary>
        /// The location where the conversation was recorded.
        /// Added to support the location view and detail headers.
        /// </summary>
        public string? Location { get; set; }

        public string? Summary { get; set; }
        public List<string> Tags { get; set; } = [];
        public List<TranscriptEntry> Transcript { get; set; } = [];
        public TranscriptEntry? RecognizingEntry { get; set; }
        public AiInsights? AiInsights { get; set; } = new();
        public List<TimelineEvent>? Timeline { get; set; }
    }

    /// <summary>
    /// Represents a single entry or utterance within the full transcript.
    /// </summary>
    public class TranscriptEntry
    {
        /// <summary>
        /// Unique identifier for the speaker (from Azure Speech Service).
        /// </summary>
        [Description("Unique speaker identifier from speech recognition service")]
        public string SpeakerId { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable speaker label (e.g., "Guest 1", "Host").
        /// </summary>
        [Description("Descriptive label for the speaker")]
        public string? SpeakerLabel { get; set; }

        /// <summary>
        /// The actual spoken text.
        /// </summary>
        [Description("Transcribed text of what the speaker said")]
        public string? Text { get; set; }

        /// <summary>
        /// Speaker initials for display purposes.
        /// </summary>
        [Description("Two-letter initials representing the speaker")]
        public string? Initials { get; set; } = "AB";

        /// <summary>
        /// When this utterance occurred.
        /// </summary>
        [Description("Timestamp when this part of the conversation occurred")]
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// A container for all AI-generated insights.
    /// </summary>
    public class AiInsights
    {
        public List<string>? Topics { get; set; }
        public List<ActionItem>? ActionItems { get; set; }
    }

    /// <summary>
    /// Represents a single action item with a task, assignee, and due date.
    /// </summary>
    public class ActionItem
    {
        /// <summary>
        /// The id of the parent conversation for easy reference.
        /// </summary>
        [JsonIgnore]
        [Description("Id of the conversation where this action item was identified")]
        public Guid ConversationId { get; set; }

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
        /// Current status of the Task.
        /// </summary>
        [Description("Current status of the Task")]
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// The person responsible for completing this action.
        /// </summary>
        [Description("Name or identifier of the person assigned to complete this task")]
        [JsonPropertyName("assignee")]
        public string? Assignee { get; set; }

        /// <summary>
        /// The due date for the action item, if specified.
        /// </summary>
        [Description("When this task should be completed (can be relative like 'next week' or absolute date)")]
        [JsonPropertyName("dueDate")]
        public string? DueDate { get; set; }
    }

    /// <summary>
    /// Represents a significant event in the conversation timeline.
    /// </summary>
    public class TimelineEvent
    {
        /// <summary>
        /// The timestamp of the event relative to conversation start (e.g., "00:18", "05:42").
        /// </summary>
        [Description("Timestamp in MM:SS format indicating when this event occurred in the conversation")]
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        /// <summary>
        /// Title of the event (e.g., "Budget Approved", "Decision Made").
        /// </summary>
        [Description("Title of this event")]
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// A brief description of the event (e.g., "Budget Approved", "Decision Made").
        /// </summary>
        [Description("Brief description of what happened at this point in the conversation")]
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}