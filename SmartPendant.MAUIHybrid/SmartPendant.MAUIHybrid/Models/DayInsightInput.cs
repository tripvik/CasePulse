namespace SmartPendant.MAUIHybrid.Models
{
    /// <summary>
    /// Input model for daily insight generation.
    /// </summary>
    public class DayInsightInput
    {
        public DateTime Date { get; set; }
        public List<ConversationRecord> Conversations { get; set; } = [];
        public DayStats Stats { get; set; } = new();
        public List<LocationActivity> LocationActivities { get; set; } = [];
        public List<ActionItem> OpenActions { get; set; } = [];
        public List<ActionItem> CompletedActions { get; set; } = [];
        public List<PersonInteraction> PeopleInteractions { get; set; } = [];
    }
}
