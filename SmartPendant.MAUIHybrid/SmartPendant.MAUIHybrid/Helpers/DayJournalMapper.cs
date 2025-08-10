using SmartPendant.MAUIHybrid.Data;
using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Helpers
{
    /// <summary>
    /// A static class to handle mapping between DayRecord DTOs and Entities.
    /// </summary>
    public static class DayJournalMapper
    {
        #region To DTO Mappers

        public static DayRecord ToDto(this DayRecordEntity entity)
        {
            return new DayRecord
            {
                Date = entity.Date,
                Conversations = [], // This will be populated separately as needed
                Insights = new DayInsights
                {
                    DailySummary = entity.DailySummary,
                    KeyTopics = entity.KeyTopics?.Select(kt => kt.Topic).ToList() ?? [],
                    KeyDecisions = entity.KeyDecisions?.Select(kd => kd.Decision).ToList() ?? [],
                    ImportantMoments = entity.ImportantMoments?.Select(im => im.Moment).ToList() ?? [],
                    PeopleInteracted = entity.PeopleInteracted?.Select(pi => pi.ToDto()).ToList() ?? [],
                    MoodAnalysis = entity.MoodAnalysis,
                    LearningsInsights = entity.LearningsInsights?.Select(li => li.Learning).ToList() ?? [],
                    JournalEntry = new DailyJournalEntry
                    {
                        Date = entity.Date,
                        ExecutiveSummary = entity.ExecutiveSummary,
                        KeyAccomplishments = entity.KeyAccomplishments?.Select(ka => ka.Accomplishment).ToList() ?? [],
                        ImportantDecisions = entity.ImportantDecisions?.Select(id => id.Decision).ToList() ?? [],
                        PeopleHighlights = entity.PeopleHighlights?.Select(ph => ph.Highlight).ToList() ?? [],
                        LearningsReflections = entity.LearningsReflections?.Select(lr => lr.Reflection).ToList() ?? [],
                        TomorrowPreparation = entity.TomorrowPreparations?.Select(tp => tp.Preparation).ToList() ?? [],
                        PersonalReflection = entity.PersonalReflection
                    }
                },
                Stats = new DayStats
                {
                    TotalTalkTimeMinutes = entity.TotalTalkTimeMinutes,
                    TotalConversations = entity.TotalConversations,
                    UniqueLocations = entity.UniqueLocations,
                    UniquePeople = entity.UniquePeople,
                    AverageConversationLength = entity.AverageConversationLength,
                    MostActiveLocation = entity.MostActiveLocation,
                    LongestConversationTitle = entity.LongestConversationTitle,
                    FirstConversation = entity.FirstConversation,
                    LastConversation = entity.LastConversation
                },
                LocationActivities = entity.LocationActivities?.Select(la => la.ToDto()).ToList() ?? [],
                OpenActions = [], // This will be populated separately from ActionItem entities
                CompletedActions = [] // This will be populated separately from ActionItem entities
            };
        }

        public static PersonInteraction ToDto(this PersonInteractionEntity entity) => new()
        {
            PersonName = entity.PersonName,
            PersonInitials = entity.PersonInitials,
            InteractionCount = entity.InteractionCount,
            TotalTimeMinutes = entity.TotalTimeMinutes,
            TopicsDiscussed = entity.TopicsDiscussed?.Select(td => td.Topic).ToList() ?? [],
            ConversationTitles = entity.ConversationTitles?.Select(ct => ct.Title).ToList() ?? []
        };

        public static LocationActivity ToDto(this LocationActivityEntity entity) => new()
        {
            Location = entity.Location,
            ConversationCount = entity.ConversationCount,
            TotalTimeMinutes = entity.TotalTimeMinutes,
            Topics = entity.Topics?.Select(t => t.Topic).ToList() ?? [],
            FirstActivity = entity.FirstActivity,
            LastActivity = entity.LastActivity
        };

        #endregion

        #region To Entity Mappers

        public static DayRecordEntity ToEntity(this DayRecord dto)
        {
            return new DayRecordEntity
            {
                Date = dto.Date.Date, // Ensure we use date only
                DailySummary = dto.Insights.DailySummary,
                MoodAnalysis = dto.Insights.MoodAnalysis,
                ExecutiveSummary = dto.Insights.JournalEntry.ExecutiveSummary,
                PersonalReflection = dto.Insights.JournalEntry.PersonalReflection,
                TotalTalkTimeMinutes = dto.Stats.TotalTalkTimeMinutes,
                TotalConversations = dto.Stats.TotalConversations,
                UniqueLocations = dto.Stats.UniqueLocations,
                UniquePeople = dto.Stats.UniquePeople,
                AverageConversationLength = dto.Stats.AverageConversationLength,
                MostActiveLocation = dto.Stats.MostActiveLocation,
                LongestConversationTitle = dto.Stats.LongestConversationTitle,
                FirstConversation = dto.Stats.FirstConversation,
                LastConversation = dto.Stats.LastConversation,
                KeyTopics = dto.Insights.KeyTopics?.Select(kt => new DayKeyTopicEntity { Topic = kt }).ToList() ?? [],
                KeyDecisions = dto.Insights.KeyDecisions?.Select(kd => new DayKeyDecisionEntity { Decision = kd }).ToList() ?? [],
                ImportantMoments = dto.Insights.ImportantMoments?.Select(im => new DayImportantMomentEntity { Moment = im }).ToList() ?? [],
                LearningsInsights = dto.Insights.LearningsInsights?.Select(li => new DayLearningInsightEntity { Learning = li }).ToList() ?? [],
                KeyAccomplishments = dto.Insights.JournalEntry.KeyAccomplishments?.Select(ka => new DayKeyAccomplishmentEntity { Accomplishment = ka }).ToList() ?? [],
                ImportantDecisions = dto.Insights.JournalEntry.ImportantDecisions?.Select(id => new DayImportantDecisionEntity { Decision = id }).ToList() ?? [],
                PeopleHighlights = dto.Insights.JournalEntry.PeopleHighlights?.Select(ph => new DayPeopleHighlightEntity { Highlight = ph }).ToList() ?? [],
                LearningsReflections = dto.Insights.JournalEntry.LearningsReflections?.Select(lr => new DayLearningReflectionEntity { Reflection = lr }).ToList() ?? [],
                TomorrowPreparations = dto.Insights.JournalEntry.TomorrowPreparation?.Select(tp => new DayTomorrowPreparationEntity { Preparation = tp }).ToList() ?? [],
                PeopleInteracted = dto.Insights.PeopleInteracted?.Select(pi => pi.ToEntity()).ToList() ?? [],
                LocationActivities = dto.LocationActivities?.Select(la => la.ToEntity()).ToList() ?? []
            };
        }

        public static PersonInteractionEntity ToEntity(this PersonInteraction dto) => new()
        {
            PersonName = dto.PersonName,
            PersonInitials = dto.PersonInitials,
            InteractionCount = dto.InteractionCount,
            TotalTimeMinutes = dto.TotalTimeMinutes,
            TopicsDiscussed = dto.TopicsDiscussed?.Select(td => new PersonTopicEntity { Topic = td }).ToList() ?? [],
            ConversationTitles = dto.ConversationTitles?.Select(ct => new PersonConversationTitleEntity { Title = ct }).ToList() ?? []
        };

        public static LocationActivityEntity ToEntity(this LocationActivity dto) => new()
        {
            Location = dto.Location,
            ConversationCount = dto.ConversationCount,
            TotalTimeMinutes = dto.TotalTimeMinutes,
            FirstActivity = dto.FirstActivity,
            LastActivity = dto.LastActivity,
            Topics = dto.Topics?.Select(t => new LocationTopicEntity { Topic = t }).ToList() ?? []
        };

        #endregion
    }
}