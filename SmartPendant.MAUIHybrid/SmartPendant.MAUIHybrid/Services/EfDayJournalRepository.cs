using Microsoft.EntityFrameworkCore;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Data;
using SmartPendant.MAUIHybrid.Helpers;
using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Services
{
    /// <summary>
    /// An implementation of IDayJournalRepository that uses Entity Framework Core
    /// for database persistence.
    /// </summary>
    public class EfDayJournalRepository : IDayJournalRepository
    {
        private readonly SmartPendantDbContext _context;

        public EfDayJournalRepository(SmartPendantDbContext context)
        {
            _context = context;
        }

        public async Task SaveAsync(DayRecord dayRecord)
        {
            if (dayRecord == null) return;

            try
            {
                var date = dayRecord.Date.Date; // Ensure we use date only
                var existingEntity = await _context.DayRecords
                    .Include(d => d.KeyTopics)
                    .Include(d => d.KeyDecisions)
                    .Include(d => d.ImportantMoments)
                    .Include(d => d.LearningsInsights)
                    .Include(d => d.KeyAccomplishments)
                    .Include(d => d.ImportantDecisions)
                    .Include(d => d.PeopleHighlights)
                    .Include(d => d.LearningsReflections)
                    .Include(d => d.TomorrowPreparations)
                    .Include(d => d.PeopleInteracted)
                        .ThenInclude(pi => pi.TopicsDiscussed)
                    .Include(d => d.PeopleInteracted)
                        .ThenInclude(pi => pi.ConversationTitles)
                    .Include(d => d.LocationActivities)
                        .ThenInclude(la => la.Topics)
                    .FirstOrDefaultAsync(d => d.Date == date);

                if (existingEntity != null)
                {
                    // Update existing entity
                    UpdateExistingEntity(existingEntity, dayRecord);
                }
                else
                {
                    // Add new entity
                    var newEntity = dayRecord.ToEntity();
                    _context.DayRecords.Add(newEntity);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save day record for {dayRecord.Date:d}: {ex.Message}", ex);
            }
        }

        public async Task<DayRecord?> GetAsync(DateTime date)
        {
            try
            {
                var dateOnly = date.Date; // Ensure we use date only
                var entity = await _context.DayRecords.AsNoTracking()
                    .Include(d => d.KeyTopics)
                    .Include(d => d.KeyDecisions)
                    .Include(d => d.ImportantMoments)
                    .Include(d => d.LearningsInsights)
                    .Include(d => d.KeyAccomplishments)
                    .Include(d => d.ImportantDecisions)
                    .Include(d => d.PeopleHighlights)
                    .Include(d => d.LearningsReflections)
                    .Include(d => d.TomorrowPreparations)
                    .Include(d => d.PeopleInteracted)
                        .ThenInclude(pi => pi.TopicsDiscussed)
                    .Include(d => d.PeopleInteracted)
                        .ThenInclude(pi => pi.ConversationTitles)
                    .Include(d => d.LocationActivities)
                        .ThenInclude(la => la.Topics)
                    .FirstOrDefaultAsync(d => d.Date == dateOnly);

                if (entity == null) return null;

                var dayRecord = entity.ToDto();

                // Get conversations for the day
                var conversations = await _context.Conversations.AsNoTracking()
                    .Where(c => c.CreatedAt.Date == dateOnly)
                    .Include(c => c.Tags)
                    .Include(c => c.Topics)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                dayRecord.Conversations = conversations.Select(c => c.ToDto()).ToList();

                // Get action items for the day from conversations
                var actionItems = await _context.ActionItems.AsNoTracking()
                    .Include(a => a.ConversationRecord)
                    .Where(a => a.ConversationRecord.CreatedAt.Date == dateOnly)
                    .ToListAsync();

                dayRecord.OpenActions = actionItems
                    .Where(a => a.Status == ActionStatus.Pending)
                    .Select(a => a.ToDto())
                    .ToList();

                dayRecord.CompletedActions = actionItems
                    .Where(a => a.Status == ActionStatus.Completed)
                    .Select(a => a.ToDto())
                    .ToList();

                return dayRecord;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve day record for {date:d}: {ex.Message}", ex);
            }
        }

        public async Task DeleteAsync(DateTime date)
        {
            try
            {
                var dateOnly = date.Date; // Ensure we use date only
                var entity = await _context.DayRecords.FindAsync(dateOnly);
                
                if (entity != null)
                {
                    _context.DayRecords.Remove(entity);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete day record for {date:d}: {ex.Message}", ex);
            }
        }

        #region Private Methods

        private void UpdateExistingEntity(DayRecordEntity existingEntity, DayRecord dayRecord)
        {
            // Update scalar properties
            existingEntity.DailySummary = dayRecord.Insights.DailySummary;
            existingEntity.MoodAnalysis = dayRecord.Insights.MoodAnalysis;
            existingEntity.ExecutiveSummary = dayRecord.Insights.JournalEntry.ExecutiveSummary;
            existingEntity.PersonalReflection = dayRecord.Insights.JournalEntry.PersonalReflection;
            existingEntity.TotalTalkTimeMinutes = dayRecord.Stats.TotalTalkTimeMinutes;
            existingEntity.TotalConversations = dayRecord.Stats.TotalConversations;
            existingEntity.UniqueLocations = dayRecord.Stats.UniqueLocations;
            existingEntity.UniquePeople = dayRecord.Stats.UniquePeople;
            existingEntity.AverageConversationLength = dayRecord.Stats.AverageConversationLength;
            existingEntity.MostActiveLocation = dayRecord.Stats.MostActiveLocation;
            existingEntity.LongestConversationTitle = dayRecord.Stats.LongestConversationTitle;
            existingEntity.FirstConversation = dayRecord.Stats.FirstConversation;
            existingEntity.LastConversation = dayRecord.Stats.LastConversation;

            // Update collections efficiently
            UpdateKeyTopics(existingEntity, dayRecord.Insights.KeyTopics);
            UpdateKeyDecisions(existingEntity, dayRecord.Insights.KeyDecisions);
            UpdateImportantMoments(existingEntity, dayRecord.Insights.ImportantMoments);
            UpdateLearningsInsights(existingEntity, dayRecord.Insights.LearningsInsights);
            UpdateKeyAccomplishments(existingEntity, dayRecord.Insights.JournalEntry.KeyAccomplishments);
            UpdateImportantDecisions(existingEntity, dayRecord.Insights.JournalEntry.ImportantDecisions);
            UpdatePeopleHighlights(existingEntity, dayRecord.Insights.JournalEntry.PeopleHighlights);
            UpdateLearningsReflections(existingEntity, dayRecord.Insights.JournalEntry.LearningsReflections);
            UpdateTomorrowPreparations(existingEntity, dayRecord.Insights.JournalEntry.TomorrowPreparation);
            UpdatePeopleInteracted(existingEntity, dayRecord.Insights.PeopleInteracted);
            UpdateLocationActivities(existingEntity, dayRecord.LocationActivities);
        }

        private void UpdateKeyTopics(DayRecordEntity entity, List<string>? newTopics)
        {
            if (newTopics == null) return;

            entity.KeyTopics.Clear();
            foreach (var topic in newTopics)
            {
                entity.KeyTopics.Add(new DayKeyTopicEntity { Topic = topic });
            }
        }

        private void UpdateKeyDecisions(DayRecordEntity entity, List<string>? newDecisions)
        {
            if (newDecisions == null) return;

            entity.KeyDecisions.Clear();
            foreach (var decision in newDecisions)
            {
                entity.KeyDecisions.Add(new DayKeyDecisionEntity { Decision = decision });
            }
        }

        private void UpdateImportantMoments(DayRecordEntity entity, List<string>? newMoments)
        {
            if (newMoments == null) return;

            entity.ImportantMoments.Clear();
            foreach (var moment in newMoments)
            {
                entity.ImportantMoments.Add(new DayImportantMomentEntity { Moment = moment });
            }
        }

        private void UpdateLearningsInsights(DayRecordEntity entity, List<string>? newLearnings)
        {
            if (newLearnings == null) return;

            entity.LearningsInsights.Clear();
            foreach (var learning in newLearnings)
            {
                entity.LearningsInsights.Add(new DayLearningInsightEntity { Learning = learning });
            }
        }

        private void UpdateKeyAccomplishments(DayRecordEntity entity, List<string>? newAccomplishments)
        {
            if (newAccomplishments == null) return;

            entity.KeyAccomplishments.Clear();
            foreach (var accomplishment in newAccomplishments)
            {
                entity.KeyAccomplishments.Add(new DayKeyAccomplishmentEntity { Accomplishment = accomplishment });
            }
        }

        private void UpdateImportantDecisions(DayRecordEntity entity, List<string>? newDecisions)
        {
            if (newDecisions == null) return;

            entity.ImportantDecisions.Clear();
            foreach (var decision in newDecisions)
            {
                entity.ImportantDecisions.Add(new DayImportantDecisionEntity { Decision = decision });
            }
        }

        private void UpdatePeopleHighlights(DayRecordEntity entity, List<string>? newHighlights)
        {
            if (newHighlights == null) return;

            entity.PeopleHighlights.Clear();
            foreach (var highlight in newHighlights)
            {
                entity.PeopleHighlights.Add(new DayPeopleHighlightEntity { Highlight = highlight });
            }
        }

        private void UpdateLearningsReflections(DayRecordEntity entity, List<string>? newReflections)
        {
            if (newReflections == null) return;

            entity.LearningsReflections.Clear();
            foreach (var reflection in newReflections)
            {
                entity.LearningsReflections.Add(new DayLearningReflectionEntity { Reflection = reflection });
            }
        }

        private void UpdateTomorrowPreparations(DayRecordEntity entity, List<string>? newPreparations)
        {
            if (newPreparations == null) return;

            entity.TomorrowPreparations.Clear();
            foreach (var preparation in newPreparations)
            {
                entity.TomorrowPreparations.Add(new DayTomorrowPreparationEntity { Preparation = preparation });
            }
        }

        private void UpdatePeopleInteracted(DayRecordEntity entity, List<PersonInteraction>? newPeople)
        {
            if (newPeople == null) return;

            entity.PeopleInteracted.Clear();
            foreach (var person in newPeople)
            {
                entity.PeopleInteracted.Add(person.ToEntity());
            }
        }

        private void UpdateLocationActivities(DayRecordEntity entity, List<LocationActivity>? newActivities)
        {
            if (newActivities == null) return;

            entity.LocationActivities.Clear();
            foreach (var activity in newActivities)
            {
                entity.LocationActivities.Add(activity.ToEntity());
            }
        }

        #endregion
    }
}