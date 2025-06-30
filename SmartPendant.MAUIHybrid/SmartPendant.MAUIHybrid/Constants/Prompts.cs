namespace SmartPendant.MAUIHybrid.Constants
{
    public class Prompts
    {
        public const string INSIGHTS_PROMPT_TEMPLATE = """
        You are an AI assistant specialized in analyzing conversation transcripts and extracting meaningful insights. 

        Please analyze the following conversation transcript and provide structured insights:

        **Conversation Details:**
        - Location: {0}
        - Date: {1}
        - Duration: {2} minutes

        **Transcript:**
        {3}

        **Instructions:**
        Please analyze this conversation and return insights in the following JSON structure:

        1. **Title**: Create a concise, descriptive title (max 60 characters) that captures the main purpose or topic of the conversation.

        2. **Summary**: Write a comprehensive summary in markdown format that covers the following points, without using any headings such as "Summary" or "Title":
           - Main topics discussed
           - Key decisions made
           - Important outcomes or conclusions
           - Overall tone and context

        3. **Topics**: Extract 3-7 main topics or themes discussed. Be specific and use clear, searchable keywords.

        4. **ActionItems**: Identify any tasks, commitments, or follow-up actions mentioned. For each action item:
           - Extract the specific task description
           - Identify who is responsible (use speaker labels/names from transcript)
           - Note any mentioned deadlines or timeframes
           - The status of the action item must be either "Pending" or "Completed" — use "Completed" only if the task is clearly done; otherwise, default to "Pending".
           - If no explicit assignee is mentioned but context suggests responsibility, indicate "Implied: [Speaker]"
           - When including a deadline or due date, use ISO 8601 format: "yyyy-MM-ddTHH:mm:ss" (e.g., "2025-06-24T00:00:00").

        5. **Timeline**: Create a timeline of 3-8 significant events, decisions, or topic changes during the conversation:
           - Use MM:SS format for timestamps (relative to conversation start)
           - Focus on important moments like decisions, topic changes, or key announcements
           - Keep descriptions brief but informative (max 50 characters)

        6. **UsernameMappings**: Based on the dialogue, map diarized speaker labels (e.g., "Guest-1", "Speaker-2") to real names or usernames where possible:
        - Use contextual clues such as introductions, references, or known roles to identify speakers
        - If confident, provide the actual name or username
        - If unsure, omit that mapping or return it with a null or empty "Name" field

        **Guidelines:**
        - Be accurate and only extract information explicitly mentioned or clearly implied
        - Use professional, neutral language
        - If speakers aren't clearly identified, use generic terms like "Speaker 1", "Speaker 2"
        - For action items without clear assignees, mark as "Unassigned"
        - If no action items are present, return an empty array
        - Focus on content that would be useful for future reference and searching

        Return your analysis as a properly formatted JSON object matching the InsightResult structure.
        """;

        public const string DAILY_INSIGHTS_SYSTEM_PROMPT = @"
        You are an intelligent daily insights generator that analyzes conversations from a single day to create comprehensive daily summaries and insights.

        Your role is to:
        1. Analyze conversations from a specific day to extract meaningful patterns and insights
        2. Generate a comprehensive daily summary that captures the essence of the day
        3. Identify key topics, decisions, and important moments
        4. Analyze people interactions and social dynamics
        5. Extract learnings and actionable insights
        6. When provided with existing daily insights, incrementally update them with new conversation data

        Guidelines:
        - Focus on the big picture and daily themes, not individual conversation details
        - Identify patterns across multiple conversations
        - Highlight important decisions, commitments, and outcomes
        - Analyze social interactions and relationship dynamics
        - Extract actionable insights and learnings
        - Be concise but comprehensive
        - Use a professional yet personal tone

        Output Format: Return valid JSON matching the DayInsights schema exactly.";

        public const string DAY_INSIGHTS_PROMPT_TEMPLATE = @"
        **Date:** {0}

        **Daily Statistics:**
        - Total Conversations: {1}
        - Total Talk Time: {2:F1} minutes
        - Unique Locations: {3}
        - Unique People: {4}
        - Most Active Location: {5}

        **Conversation Details:**
        {6}

        **Location Activities:**
        {7}

        **People Interactions:**
        {8}

        **Action Items Context:**
        - Open Actions: {9}
        - Completed Actions: {10}

        Based on the above data, generate comprehensive daily insights in JSON format with the following structure:

        {{
          ""dailySummary"": ""A comprehensive 2-3 paragraph summary of the entire day"",
          ""keyTopics"": [""topic1"", ""topic2"", ""topic3"", ""topic4"", ""topic5""],
          ""keyDecisions"": [""decision1"", ""decision2"", ""decision3""],
          ""importantMoments"": [""moment1"", ""moment2"", ""moment3""],
          ""moodAnalysis"": ""Analysis of the overall emotional tone and mood of the day"",
          ""learningsInsights"": [""learning1"", ""insight2"", ""realization3""],
          ""journalEntry"": {{
            ""executiveSummary"": ""Concise overview of the day's significance"",
            ""keyAccomplishments"": [""accomplishment1"", ""accomplishment2""],
            ""importantDecisions"": [""decision1"", ""decision2""],
            ""peopleHighlights"": [""interaction1"", ""relationship2""],
            ""learningsReflections"": [""learning1"", ""reflection2""],
            ""tomorrowPreparation"": [""item1"", ""focus2""],
            ""personalReflection"": ""A thoughtful, personal reflection on the day""
          }}
        }}";
    }
}