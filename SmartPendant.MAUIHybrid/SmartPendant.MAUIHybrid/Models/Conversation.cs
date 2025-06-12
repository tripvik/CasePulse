namespace SmartPendant.MAUIHybrid.Models
{
    /// <summary>
    /// Represents the main data model for a single conversation record.
    /// This model is designed to hold all necessary information for display after being processed by an LLM.
    /// </summary>
    public class Conversation
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DurationSeconds { get; set; }

        /// <summary>
        /// The location where the conversation was recorded.
        /// Added to support the location view and detail headers.
        /// </summary>
        public string? Location { get; set; }

        public string? Summary { get; set; }
        public List<TranscriptEntry> Transcript { get; set; } = [];
        public AiInsights? AiInsights { get; set; }
        public List<TimelineEvent>? Timeline { get; set; }
    }

    /// <summary>
    /// Represents a single entry or utterance within the full transcript.
    /// </summary>
    public class TranscriptEntry
    {
        public string SpeakerId { get; set; } = string.Empty;
        public string? SpeakerLabel { get; set; }
        public string? Text { get; set; }
        public string? Initials { get; set; } = "AB";
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
    /// It now includes a reference back to its parent conversation.
    /// </summary>
    public class ActionItem
    {
        /// <summary>
        /// A reference to the parent conversation's ID.
        /// Essential for linking a task back to its source.
        /// </summary>
        public Guid ConversationId { get; set; }

        /// <summary>
        /// The title of the parent conversation.
        /// Stored for easy display in the Tasks view without needing to look up the conversation.
        /// </summary>
        public string? ConversationTitle { get; set; }

        public string? Task { get; set; }
        public string? Assignee { get; set; }

        /// <summary>
        /// The due date for the action item, if specified.
        /// Can be null.
        /// </summary>
        public string? DueDate { get; set; }
    }

    /// <summary>
    /// Represents a single, specific event to be plotted on the timeline.
    /// Simplified to match the LLM output and UI requirements.
    /// </summary>
    public class TimelineEvent
    {
        /// <summary>
        /// The timestamp of the event (e.g., "00:18").
        /// </summary>
        public string? Timestamp { get; set; }

        /// <summary>
        /// A brief description of the event (e.g., "Budget Approved").
        /// This directly maps to the 'event' field from the LLM output.
        /// </summary>
        public string? Description { get; set; }
    }
}