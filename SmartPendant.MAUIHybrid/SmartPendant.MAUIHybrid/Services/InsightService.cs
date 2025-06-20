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
            _chatClient = chatClient;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates AI insights for a given conversation and applies them to the conversation object.
        /// </summary>
        /// <param name="conversation">The conversation object to process.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if the conversation is null.</exception>
        public async Task GenerateAndApplyInsightAsync(Conversation conversation, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(conversation);

            if (!conversation.Transcript.Any())
            {
                Debug.WriteLine("GenerateAndApplyInsightAsync called with an empty transcript.");
                return;
            }

            if (conversation.Transcript.Count > 1)
            {
                conversation.DurationMinutes = (conversation.Transcript[^1].Timestamp - conversation.Transcript[0].Timestamp).TotalMinutes;
            }

            var insightInput = CreateInsightInputFromConversation(conversation);
            var insightResult = await GetInsightAsync(insightInput, cancellationToken);

            if (insightResult != null)
            {
                conversation.AiInsights ??= new AiInsights();
                conversation.AiInsights.ActionItems = insightResult.ActionItems;
                conversation.AiInsights.Topics = insightResult.Topics;
                conversation.Summary = insightResult.Summary;
                conversation.Title = insightResult.Title;
                conversation.Timeline = insightResult.Timeline;
            }
        }

        #endregion

        #region Private Methods

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
                var response = await _chatClient.GetResponseAsync<InsightResult>(chatMessage: prompt, cancellationToken: cancellationToken);
                return response.Result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting insights from AI client. {ex.Message}");
                // Re-throw the exception to be handled by the caller (e.g., the UI layer).
                throw;
            }
        }

        /// <summary>
        /// Creates an <see cref="InsightInput"/> object from a <see cref="Conversation"/>.
        /// </summary>
        private static InsightInput CreateInsightInputFromConversation(Conversation conversation)
        {
            return new InsightInput
            {
                Transcript = conversation.Transcript,
                Location = conversation.Location,
                CreatedAt = conversation.CreatedAt,
                DurationMinutes = conversation.DurationMinutes
            };
        }

        /// <summary>
        /// Generates the final prompt string to be sent to the AI model.
        /// </summary>
        private static string GenerateInsightsPrompt(InsightInput input)
        {
            var transcriptBuilder = new StringBuilder();
            if (input.Transcript != null)
            {
                foreach (var entry in input.Transcript)
                {
                    var speaker = entry.SpeakerLabel ?? entry.SpeakerId;
                    transcriptBuilder.AppendLine($"[{entry.Timestamp:HH:mm:ss}] {speaker}: {entry.Text}");
                }
            }

            return string.Format(Prompts.INSIGHTS_PROMPT_TEMPLATE,
                input.Location ?? "Unknown",
                input.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                input.DurationMinutes,
                transcriptBuilder.ToString());
        }

        #endregion
    }
}
