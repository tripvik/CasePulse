using System.ComponentModel;

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
        public List<ActionItem>? ActionItems { get; set; }

        /// <summary>
        /// Timeline of significant events during the conversation.
        /// </summary>
        [Description("Chronological list of important events, decisions, or milestones mentioned in the conversation")]
        public List<TimelineEvent>? Timeline { get; set; }
    }
}
