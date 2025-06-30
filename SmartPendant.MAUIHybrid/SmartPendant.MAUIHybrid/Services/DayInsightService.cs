using Microsoft.Extensions.AI;
using SmartPendant.MAUIHybrid.Constants;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;
using System.Text;

namespace SmartPendant.MAUIHybrid.Services
{
    /// <summary>
    /// Provides services for generating AI-driven insights from a full day's conversations.
    /// </summary>
    public class DayInsightService
    {
        #region Fields

        private readonly IChatClient _chatClient;

        #endregion

        #region Constructor

        public DayInsightService(IChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates AI insights for a given day and applies them to the day model object.
        /// </summary>
        /// <param name="dayModel">The day model object to process.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if the dayModel is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the dayModel has invalid data.</exception>
        public async Task GenerateAndApplyDailyInsightAsync(DayModel dayModel, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dayModel);

            if (!IsValidDayModel(dayModel))
            {
                Debug.WriteLine($"GenerateAndApplyDailyInsightAsync called with invalid day data. Date: {dayModel.Date:yyyy-MM-dd}");
                return;
            }

            try
            {
                // Calculate day statistics
                CalculateDayStats(dayModel);

                // Organize action items
                OrganizeActionItems(dayModel);

                // Group location activities
                GroupLocationActivities(dayModel);

                // Generate AI insights
                var dayInsightInput = CreateDayInsightInputFromModel(dayModel);
                var insightResult = await GetDayInsightAsync(dayInsightInput, cancellationToken);

                if (insightResult != null)
                {
                    ApplyInsightsToDayModel(dayModel, insightResult);
                    Debug.WriteLine($"Successfully generated daily insights for {dayModel.Date:yyyy-MM-dd}");
                }
                else
                {
                    Debug.WriteLine($"No insights generated for day {dayModel.Date:yyyy-MM-dd}");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Daily insight generation was cancelled for {dayModel.Date:yyyy-MM-dd}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to generate daily insights for {dayModel.Date:yyyy-MM-dd}. Error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Private Methods - Validation

        /// <summary>
        /// Validates if the day model has the minimum required data for insight generation.
        /// </summary>
        /// <param name="dayModel">The day model to validate.</param>
        /// <returns>True if the day model is valid for processing.</returns>
        private static bool IsValidDayModel(DayModel dayModel)
        {
            return dayModel.Conversations?.Any() == true &&
                   dayModel.Conversations.All(c => c.Transcript?.Any() == true);
        }

        #endregion

        #region Private Methods - Statistics Calculation

        /// <summary>
        /// Calculates and sets the day statistics based on conversations.
        /// </summary>
        /// <param name="dayModel">The day model to calculate statistics for.</param>
        private static void CalculateDayStats(DayModel dayModel)
        {
            if (!dayModel.Conversations?.Any() == true)
            {
                dayModel.Stats = new DayStats();
                return;
            }

            var conversations = dayModel.Conversations;
            var totalMinutes = conversations.Sum(c => c.DurationMinutes);
            var locations = conversations.Where(c => !string.IsNullOrWhiteSpace(c.Location))
                                      .Select(c => c.Location!.Trim())
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .ToList();

            var people = conversations.SelectMany(c => c.Transcript ?? [])
                                    .Where(t => !string.IsNullOrWhiteSpace(t.SpeakerLabel))
                                    .Select(t => t.SpeakerLabel!.Trim())
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToList();

            var conversationTimes = conversations.Select(c => c.CreatedAt.TimeOfDay).OrderBy(t => t).ToList();
            var longestConversation = conversations.OrderByDescending(c => c.DurationMinutes).FirstOrDefault();

            // Find most active location
            var locationGroups = conversations.Where(c => !string.IsNullOrWhiteSpace(c.Location))
                                           .GroupBy(c => c.Location!.Trim(), StringComparer.OrdinalIgnoreCase)
                                           .ToList();
            var mostActiveLocation = locationGroups.OrderByDescending(g => g.Sum(c => c.DurationMinutes))
                                                 .FirstOrDefault()?.Key;

            dayModel.Stats = new DayStats
            {
                TotalTalkTimeMinutes = totalMinutes,
                TotalConversations = conversations.Count,
                UniqueLocations = locations.Count,
                UniquePeople = people.Count,
                AverageConversationLength = conversations.Count > 0 ? totalMinutes / conversations.Count : 0,
                MostActiveLocation = mostActiveLocation,
                LongestConversationTitle = longestConversation?.Title,
                FirstConversation = conversationTimes.FirstOrDefault(),
                LastConversation = conversationTimes.LastOrDefault()
            };
        }

        /// <summary>
        /// Organizes action items into open and completed categories.
        /// </summary>
        /// <param name="dayModel">The day model to organize action items for.</param>
        private static void OrganizeActionItems(DayModel dayModel)
        {
            var allActionItems = dayModel.Conversations
                .Where(c => c.ConversationInsights?.ActionItems?.Any() == true)
                .SelectMany(c => c.ConversationInsights.ActionItems)
                .Where(a => a != null)
                .ToList();

            dayModel.OpenActions = allActionItems.Where(a => a.Status == ActionStatus.Pending).ToList();
            dayModel.CompletedActions = allActionItems.Where(a => a.Status == ActionStatus.Completed).ToList();
        }

        /// <summary>
        /// Groups conversations by location and calculates location-based activities.
        /// </summary>
        /// <param name="dayModel">The day model to group location activities for.</param>
        private static void GroupLocationActivities(DayModel dayModel)
        {
            var locationGroups = dayModel.Conversations
                .Where(c => !string.IsNullOrWhiteSpace(c.Location))
                .GroupBy(c => c.Location!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            dayModel.LocationActivities = locationGroups.Select(group =>
            {
                var conversations = group.ToList();
                var topics = conversations.SelectMany(c => c.ConversationInsights?.Topics ?? [])
                                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                        .ToList();
                var times = conversations.Select(c => c.CreatedAt.TimeOfDay).OrderBy(t => t).ToList();

                return new LocationActivity
                {
                    Location = group.Key,
                    ConversationCount = conversations.Count,
                    TotalTimeMinutes = conversations.Sum(c => c.DurationMinutes),
                    Topics = topics,
                    FirstActivity = times.FirstOrDefault(),
                    LastActivity = times.LastOrDefault()
                };
            }).OrderByDescending(la => la.TotalTimeMinutes).ToList();
        }

        #endregion

        #region Private Methods - AI Processing

        /// <summary>
        /// Calls the AI chat client to get daily insights based on the provided input.
        /// </summary>
        /// <param name="dayInsightInput">The input data for daily insight generation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The generated daily insights, or null if an error occurs.</returns>
        private async Task<DayInsightResult?> GetDayInsightAsync(DayInsightInput dayInsightInput, CancellationToken cancellationToken)
        {
            try
            {
                var prompt = GenerateDayInsightsPrompt(dayInsightInput);

                Debug.WriteLine($"Generating daily insights with prompt length: {prompt.Length} characters");

                var chatMessages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.System, Prompts.DAILY_INSIGHTS_SYSTEM_PROMPT),
                    new ChatMessage(ChatRole.User, prompt)
                };

                var response = await _chatClient.GetResponseAsync<DayInsightResult>(
                    messages: chatMessages,
                    cancellationToken: cancellationToken);

                return response.Result;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("AI daily insight generation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting daily insights from AI client: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a DayInsightInput object from a DayModel.
        /// </summary>
        /// <param name="dayModel">The day model to convert.</param>
        /// <returns>A new DayInsightInput instance.</returns>
        private static DayInsightInput CreateDayInsightInputFromModel(DayModel dayModel)
        {
            var peopleInteractions = CalculatePeopleInteractions(dayModel.Conversations);

            return new DayInsightInput
            {
                Date = dayModel.Date,
                Conversations = dayModel.Conversations.ToList(),
                Stats = dayModel.Stats,
                LocationActivities = dayModel.LocationActivities.ToList(),
                OpenActions = dayModel.OpenActions.ToList(),
                CompletedActions = dayModel.CompletedActions.ToList(),
                PeopleInteractions = peopleInteractions
            };
        }

        /// <summary>
        /// Calculates people interactions from conversations.
        /// </summary>
        /// <param name="conversations">The conversations to analyze.</param>
        /// <returns>A list of person interactions.</returns>
        private static List<PersonInteraction> CalculatePeopleInteractions(List<ConversationModel> conversations)
        {
            var speakerGroups = conversations
                .SelectMany(c => c.Transcript?.Where(t => !string.IsNullOrWhiteSpace(t.SpeakerLabel)) ?? [])
                .GroupBy(t => t.SpeakerLabel!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            return speakerGroups.Select(group =>
            {
                var speaker = group.Key;
                var relatedConversations = conversations
                    .Where(c => c.Transcript?.Any(t => string.Equals(t.SpeakerLabel?.Trim(), speaker, StringComparison.OrdinalIgnoreCase)) == true)
                    .ToList();

                var topics = relatedConversations
                    .SelectMany(c => c.ConversationInsights?.Topics ?? [])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new PersonInteraction
                {
                    PersonName = speaker,
                    PersonInitials = GetInitials(speaker),
                    InteractionCount = relatedConversations.Count,
                    TotalTimeMinutes = relatedConversations.Sum(c => c.DurationMinutes),
                    TopicsDiscussed = topics,
                    ConversationTitles = relatedConversations.Select(c => c.Title ?? "Untitled").ToList()
                };
            }).OrderByDescending(pi => pi.TotalTimeMinutes).ToList();
        }

        /// <summary>
        /// Generates initials from a full name.
        /// </summary>
        /// <param name="name">The full name to generate initials from.</param>
        /// <returns>Two-character initials, or "UN" if name is invalid.</returns>
        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "UN"; // Unknown

            var words = name.Trim()
                           .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                           .Where(w => !string.IsNullOrWhiteSpace(w))
                           .ToArray();

            return words.Length switch
            {
                0 => "UN",
                1 => (words[0].Length >= 2 ? words[0][..2] : words[0].PadRight(2, 'X')).ToUpper(),
                _ => $"{words[0][0]}{words[^1][0]}".ToUpper()
            };
        }

        /// <summary>
        /// Generates the final prompt string to be sent to the AI model for daily insights.
        /// </summary>
        /// <param name="input">The input data for prompt generation.</param>
        /// <returns>The formatted prompt string.</returns>
        private static string GenerateDayInsightsPrompt(DayInsightInput input)
        {
            // Build conversation details
            var conversationDetails = new StringBuilder();
            foreach (var conversation in input.Conversations.OrderBy(c => c.CreatedAt))
            {
                conversationDetails.AppendLine($"[{conversation.CreatedAt:HH:mm}] {conversation.Title ?? "Untitled"}");
                conversationDetails.AppendLine($"  Location: {conversation.Location ?? "Unknown"}");
                conversationDetails.AppendLine($"  Duration: {conversation.DurationMinutes:F1} min");
                if (!string.IsNullOrWhiteSpace(conversation.Summary))
                {
                    conversationDetails.AppendLine($"  Summary: {conversation.Summary}");
                }
                if (conversation.ConversationInsights?.Topics?.Any() == true)
                {
                    conversationDetails.AppendLine($"  Topics: {string.Join(", ", conversation.ConversationInsights.Topics)}");
                }
                conversationDetails.AppendLine();
            }

            // Build location activities
            var locationActivities = new StringBuilder();
            if (input.LocationActivities.Any())
            {
                foreach (var location in input.LocationActivities)
                {
                    locationActivities.AppendLine($"📍 {location.Location}: {location.ConversationCount} conversations, {location.TotalTimeMinutes:F1} min");
                    if (location.Topics.Any())
                    {
                        locationActivities.AppendLine($"   Topics: {string.Join(", ", location.Topics)}");
                    }
                }
            }
            else
            {
                locationActivities.AppendLine("No location data available");
            }

            // Build people interactions
            var peopleInteractions = new StringBuilder();
            if (input.PeopleInteractions.Any())
            {
                foreach (var person in input.PeopleInteractions.Take(10)) // Limit to top 10
                {
                    peopleInteractions.AppendLine($"👤 {person.PersonName}: {person.InteractionCount} conversations, {person.TotalTimeMinutes:F1} min");
                    if (person.TopicsDiscussed.Any())
                    {
                        peopleInteractions.AppendLine($"   Topics: {string.Join(", ", person.TopicsDiscussed.Take(5))}");
                    }
                }
            }
            else
            {
                peopleInteractions.AppendLine("No people interaction data available");
            }

            var formattedPrompt = string.Format(
                Prompts.DAY_INSIGHTS_PROMPT_TEMPLATE,
                input.Date.ToString("yyyy-MM-dd dddd"),                    // {0} - Date
                input.Stats.TotalConversations,                           // {1} - Total Conversations
                input.Stats.TotalTalkTimeMinutes,                         // {2} - Total Talk Time
                input.Stats.UniqueLocations,                              // {3} - Unique Locations
                input.Stats.UniquePeople,                                 // {4} - Unique People
                input.Stats.MostActiveLocation ?? "Unknown",              // {5} - Most Active Location
                conversationDetails.ToString(),                           // {6} - Conversation Details
                locationActivities.ToString(),                            // {7} - Location Activities
                peopleInteractions.ToString(),                            // {8} - People Interactions
                input.OpenActions.Count,                                  // {9} - Open Actions count
                input.CompletedActions.Count                              // {10} - Completed Actions count
            );

            return formattedPrompt;
        }

        /// <summary>
        /// Applies the generated daily insights to the day model object.
        /// </summary>
        /// <param name="dayModel">The day model to update.</param>
        /// <param name="insightResult">The insights to apply.</param>
        private static void ApplyInsightsToDayModel(DayModel dayModel, DayInsightResult insightResult)
        {
            // Apply main insights
            dayModel.Insights = new DayInsights
            {
                DailySummary = insightResult.DailySummary?.Trim(),
                KeyTopics = insightResult.KeyTopics ?? [],
                KeyDecisions = insightResult.KeyDecisions ?? [],
                ImportantMoments = insightResult.ImportantMoments ?? [],
                PeopleInteracted = dayModel.LocationActivities.SelectMany(la =>
                    dayModel.Conversations.Where(c => c.Location == la.Location)
                        .SelectMany(c => c.Transcript?.Where(t => !string.IsNullOrWhiteSpace(t.SpeakerLabel)) ?? [])
                        .Select(t => t.SpeakerLabel!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(name => new PersonInteraction
                        {
                            PersonName = name,
                            PersonInitials = GetInitials(name)
                        }))
                    .GroupBy(p => p.PersonName, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList(),
                MoodAnalysis = insightResult.MoodAnalysis?.Trim(),
                LearningsInsights = insightResult.LearningsInsights ?? [],
                JournalEntry = new DailyJournalEntry
                {
                    Date = dayModel.Date,
                    ExecutiveSummary = insightResult.JournalEntry?.ExecutiveSummary?.Trim(),
                    KeyAccomplishments = insightResult.JournalEntry?.KeyAccomplishments ?? [],
                    ImportantDecisions = insightResult.JournalEntry?.ImportantDecisions ?? [],
                    PeopleHighlights = insightResult.JournalEntry?.PeopleHighlights ?? [],
                    LearningsReflections = insightResult.JournalEntry?.LearningsReflections ?? [],
                    TomorrowPreparation = insightResult.JournalEntry?.TomorrowPreparation ?? [],
                    PersonalReflection = insightResult.JournalEntry?.PersonalReflection?.Trim()
                }
            };

            Debug.WriteLine($"Applied daily insights to day model for {dayModel.Date:yyyy-MM-dd}");
        }

        #endregion
    }
}