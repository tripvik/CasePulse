using System.ComponentModel;

namespace SmartPendant.MAUIHybrid.Models
{
    /// <summary>
    /// Input data for generating conversation insights.
    /// </summary>
    public class ConversationInsightInput
    {
        /// <summary>
        /// The full transcript of the conversation with speaker diarization.
        /// </summary>
        [Description("Complete conversation transcript with speaker identification and timestamps")]
        public List<TranscriptEntry>? Transcript { get; set; }

        /// <summary>
        /// Location where the conversation took place.
        /// </summary>
        [Description("Geographic location or venue where the conversation occurred")]
        public string? Location { get; set; }

        /// <summary>
        /// When the conversation was recorded.
        /// </summary>
        [Description("Date and time when the conversation was recorded")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Duration of the conversation in minutes.
        /// </summary>
        [Description("Total length of the conversation in minutes")]
        public double DurationMinutes { get; set; }
    }
}
