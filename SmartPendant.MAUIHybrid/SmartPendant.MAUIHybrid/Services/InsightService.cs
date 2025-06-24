using Microsoft.Extensions.AI;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Constants;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;
using System.Text;

namespace SmartPendant.MAUIHybrid.Services
{
    /// <summary>
    /// Provides services for generating AI-driven insights from conversation transcripts.
    /// </summary>
    public class InsightService : IInsightService
    {
        #region Fields

        private readonly IChatClient _chatClient;

        #endregion

        #region Constructor

        public InsightService(IChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates AI insights for a given conversation and applies them to the conversation object.
        /// </summary>
        /// <param name="conversation">The conversation object to process.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if the conversation is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the conversation has invalid data.</exception>
        public async Task GenerateAndApplyInsightAsync(Conversation conversation, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(conversation);

            if (!IsValidConversation(conversation))
            {
                Debug.WriteLine($"GenerateAndApplyInsightAsync called with invalid conversation data. ConversationId: {conversation.Id}");
                return;
            }

            try
            {
                // Calculate duration more safely
                CalculateConversationDuration(conversation);

                var insightInput = CreateInsightInputFromConversation(conversation);
                var insightResult = await GetInsightAsync(insightInput, cancellationToken);

                if (insightResult != null)
                {
                    ApplyInsightsToConversation(conversation, insightResult);
                    Debug.WriteLine($"Successfully generated insights for conversation {conversation.Id}");
                }
                else
                {
                    Debug.WriteLine($"No insights generated for conversation {conversation.Id}");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Insight generation was cancelled for conversation {conversation.Id}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to generate insights for conversation {conversation.Id}. Error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates if the conversation has the minimum required data for insight generation.
        /// </summary>
        /// <param name="conversation">The conversation to validate.</param>
        /// <returns>True if the conversation is valid for processing.</returns>
        private static bool IsValidConversation(Conversation conversation)
        {
            return conversation.Transcript?.Any() == true &&
                   conversation.Transcript.All(t => !string.IsNullOrWhiteSpace(t.Text));
        }

        /// <summary>
        /// Calculates and sets the conversation duration based on transcript timestamps.
        /// </summary>
        /// <param name="conversation">The conversation to calculate duration for.</param>
        private static void CalculateConversationDuration(Conversation conversation)
        {
            if (conversation.Transcript?.Count > 1)
            {
                var orderedTranscript = conversation.Transcript
                    .OrderBy(t => t.Timestamp)
                    .ToList();

                conversation.DurationMinutes = (orderedTranscript[^1].Timestamp - orderedTranscript[0].Timestamp).TotalMinutes;
            }
        }

        /// <summary>
        /// Applies the generated insights to the conversation object.
        /// </summary>
        /// <param name="conversation">The conversation to update.</param>
        /// <param name="insightResult">The insights to apply.</param>
        private static void ApplyInsightsToConversation(Conversation conversation, InsightResult insightResult)
        {
            conversation.AiInsights = new AiInsights
            {
                ActionItems = insightResult.ActionItems?
                    .Where(item => item != null)
                    .Select(result => MapToActionItem(result, conversation.Id))
                    .ToList() ?? [],
                Topics = insightResult.Topics ?? []
            };

            // Ensure all action items have the correct conversation ID
            foreach (var item in conversation.AiInsights.ActionItems)
            {
                item.ConversationId = conversation.Id;
            }

            conversation.Summary = !string.IsNullOrWhiteSpace(insightResult.Summary)
                ? insightResult.Summary.Trim()
                : null;

            conversation.Title = !string.IsNullOrWhiteSpace(insightResult.Title)
                ? insightResult.Title.Trim()
                : $"Conversation {conversation.CreatedAt:yyyy-MM-dd HH:mm}";

            conversation.Timeline = insightResult.Timeline;
        }

        /// <summary>
        /// Calls the AI chat client to get an <see cref="InsightResult"/> based on the provided input.
        /// </summary>
        /// <param name="insightInput">The input data for insight generation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The generated <see cref="InsightResult"/>, or null if an error occurs.</returns>
        private async Task<InsightResult?> GetInsightAsync(InsightInput insightInput, CancellationToken cancellationToken)
        {
            try
            {
                var prompt = GenerateInsightsPrompt(insightInput);

                Debug.WriteLine($"Generating insights with prompt length: {prompt.Length} characters");

                var response = await _chatClient.GetResponseAsync<InsightResult>(
                    chatMessage: prompt,
                    cancellationToken: cancellationToken);

                return response.Result;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("AI insight generation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting insights from AI client: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates an <see cref="InsightInput"/> object from a <see cref="Conversation"/>.
        /// </summary>
        /// <param name="conversation">The conversation to convert.</param>
        /// <returns>A new InsightInput instance.</returns>
        private static InsightInput CreateInsightInputFromConversation(Conversation conversation)
        {
            return new InsightInput
            {
                Transcript = conversation.Transcript?.ToList() ?? new List<TranscriptEntry>(),
                Location = conversation.Location,
                CreatedAt = conversation.CreatedAt,
                DurationMinutes = conversation.DurationMinutes
            };
        }

        /// <summary>
        /// Generates the final prompt string to be sent to the AI model.
        /// </summary>
        /// <param name="input">The input data for prompt generation.</param>
        /// <returns>The formatted prompt string.</returns>
        private static string GenerateInsightsPrompt(InsightInput input)
        {
            var transcriptBuilder = new StringBuilder();

            if (input.Transcript?.Any() == true)
            {
                // Sort transcript by timestamp to ensure chronological order
                var sortedTranscript = input.Transcript
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Text))
                    .OrderBy(entry => entry.Timestamp)
                    .ToList();

                foreach (var entry in sortedTranscript)
                {
                    var speaker = !string.IsNullOrWhiteSpace(entry.SpeakerLabel)
                        ? entry.SpeakerLabel
                        : entry.SpeakerId ?? "Unknown";

                    transcriptBuilder.AppendLine($"[{entry.Timestamp:HH:mm:ss}] {speaker}: {entry?.Text?.Trim()}");
                }
            }

            var formattedPrompt = string.Format(
                Prompts.INSIGHTS_PROMPT_TEMPLATE,
                input.Location ?? "Unknown",
                input.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                Math.Round(input.DurationMinutes, 1),
                transcriptBuilder.ToString());

            return formattedPrompt;
        }

        /// <summary>
        /// Maps an ActionItemResult to an ActionItem.
        /// </summary>
        /// <param name="result">The ActionItemResult object to map from.</param>
        /// <param name="conversationId">The associated ConversationId to assign.</param>
        /// <returns>A new ActionItem instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when result is null.</exception>
        private static ActionItem MapToActionItem(ActionItemResult result, Guid conversationId)
        {
            ArgumentNullException.ThrowIfNull(result);

            return new ActionItem
            {
                TaskId = Guid.NewGuid(),
                ConversationId = conversationId,
                ConversationTitle = result.ConversationTitle?.Trim(),
                Task = result.Task?.Trim() ?? string.Empty,
                Status = result.Status,
                Assignee = result.Assignee?.Trim(),
                DueDate = result.DueDate
            };
        }

        #endregion
    }
}