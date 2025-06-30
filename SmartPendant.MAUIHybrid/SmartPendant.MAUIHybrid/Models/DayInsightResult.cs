namespace SmartPendant.MAUIHybrid.Models
{
    /// <summary>
    /// Result model for daily insight generation.
    /// </summary>
    public class DayInsightResult
    {
        public string? DailySummary { get; set; }
        public List<string> KeyTopics { get; set; } = [];
        public List<string> KeyDecisions { get; set; } = [];
        public List<string> ImportantMoments { get; set; } = [];
        public string? MoodAnalysis { get; set; }
        public List<string> LearningsInsights { get; set; } = [];
        public DailyJournalEntryResult? JournalEntry { get; set; }
    }

    /// <summary>
    /// Result model for daily journal entry.
    /// </summary>
    public class DailyJournalEntryResult
    {
        public string? ExecutiveSummary { get; set; }
        public List<string> KeyAccomplishments { get; set; } = [];
        public List<string> ImportantDecisions { get; set; } = [];
        public List<string> PeopleHighlights { get; set; } = [];
        public List<string> LearningsReflections { get; set; } = [];
        public List<string> TomorrowPreparation { get; set; } = [];
        public string? PersonalReflection { get; set; }
    }
}
