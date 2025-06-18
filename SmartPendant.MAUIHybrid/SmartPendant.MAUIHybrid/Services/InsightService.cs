using Microsoft.Extensions.AI;
using SmartPendant.MAUIHybrid.Constants;
using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Services
{
    public class InsightService
    {
        private readonly IChatClient _chatClient;

        public InsightService(IChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        public async Task<InsightResult> GetInsightAsync(InsightInput insightInput)
        {
            try
            {
                var prompt = GenerateInsightsPrompt(insightInput);
                var response = await _chatClient.GetResponseAsync<InsightResult>(prompt);
                return response.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting insights: {ex.Message}");
                return default!;
            }
        }
        private static string GenerateInsightsPrompt(InsightInput input)
        {
            var transcriptText = string.Join("\n",
                input.Transcript?.Select(t => $"[{t.Timestamp:HH:mm:ss}] {t.SpeakerLabel ?? t.SpeakerId}: {t.Text}") ?? []);

            return string.Format(Prompts.INSIGHTS_PROMPT_TEMPLATE,
                input.Location ?? "Unknown",
                input.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                input.DurationMinutes,
                transcriptText);
        }
    }
}
