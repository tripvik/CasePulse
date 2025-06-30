namespace SmartPendant.MAUIHybrid.Models
{
    public class DayRecord
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public List<ConversationRecord> Conversations { get; set; } = [];
        public DayInsights Insights { get; set; } = new();
        public DayStats Stats { get; set; } = new();
        public List<LocationActivity> LocationActivities { get; set; } = [];
        public List<ActionItem> OpenActions { get; set; } = [];
        public List<ActionItem> CompletedActions { get; set; } = [];
    }

    public class DayInsights
    {
        public string? DailySummary { get; set; }
        public List<string> KeyTopics { get; set; } = [];
        public List<string> KeyDecisions { get; set; } = [];
        public List<string> ImportantMoments { get; set; } = [];
        public List<PersonInteraction> PeopleInteracted { get; set; } = [];
        public string? MoodAnalysis { get; set; }
        public List<string> LearningsInsights { get; set; } = [];
        public DailyJournalEntry JournalEntry { get; set; } = new();
    }

    public class DayStats
    {
        public double TotalTalkTimeMinutes { get; set; }
        public int TotalConversations { get; set; }
        public int UniqueLocations { get; set; }
        public int UniquePeople { get; set; }
        public double AverageConversationLength { get; set; }
        public string? MostActiveLocation { get; set; }
        public string? LongestConversationTitle { get; set; }
        public TimeSpan FirstConversation { get; set; }
        public TimeSpan LastConversation { get; set; }
    }

    public class LocationActivity
    {
        public string? Location { get; set; }
        public int ConversationCount { get; set; }
        public double TotalTimeMinutes { get; set; }
        public List<string> Topics { get; set; } = [];
        public TimeSpan FirstActivity { get; set; }
        public TimeSpan LastActivity { get; set; }
    }

    public class PersonInteraction
    {
        public string? PersonName { get; set; }
        public string? PersonInitials { get; set; }
        public int InteractionCount { get; set; }
        public double TotalTimeMinutes { get; set; }
        public List<string> TopicsDiscussed { get; set; } = [];
        public List<string> ConversationTitles { get; set; } = [];
    }

    public class DailyJournalEntry
    {
        public DateTime Date { get; set; }
        public string? ExecutiveSummary { get; set; }
        public List<string> KeyAccomplishments { get; set; } = [];
        public List<string> ImportantDecisions { get; set; } = [];
        public List<string> PeopleHighlights { get; set; } = [];
        public List<string> LearningsReflections { get; set; } = [];
        public List<string> TomorrowPreparation { get; set; } = [];
        public string? PersonalReflection { get; set; }
    }
}
