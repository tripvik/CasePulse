using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Services
{
    /// <summary>
    /// Provides dummy conversation objects with simulated data for development and testing purposes.
    /// </summary>
    public class ConversationService
    {
        private readonly List<Conversation> _conversations;

        public ConversationService()
        {
            _conversations = new List<Conversation>
            {
                GetSampleConversation()
            };
        }

        /// <summary>
        /// Retrieves a single dummy conversation with comprehensive data filled in.
        /// </summary>
        public Task<Conversation> GetSampleConversationAsync()
        {
            return Task.FromResult(_conversations.First());
        }

        /// <summary>
        /// Retrieves all available conversations.
        /// </summary>
        public Task<List<Conversation>> GetAllConversationsAsync()
        {
            return Task.FromResult(_conversations);
        }

        /// <summary>
        /// Retrieves all action items across conversations.
        /// </summary>
        public Task<List<ActionItem>> GetAllTasksAsync()
        {
            var allTasks = _conversations
                .Where(c => c.AiInsights?.ActionItems != null)
                .SelectMany(c => c.AiInsights!.ActionItems!)
                .ToList();

            return Task.FromResult(allTasks);
        }

        /// <summary>
        /// Retrieves conversations that occurred on a specific date.
        /// </summary>
        public Task<List<Conversation>> GetConversationsByDateAsync(DateTime date)
        {
            var matches = _conversations
                .Where(c => c.CreatedAt.Date == date.Date)
                .ToList();

            return Task.FromResult(matches);
        }

        /// <summary>
        /// Retrieves tasks assigned to a specific person.
        /// </summary>
        public Task<List<ActionItem>> GetTasksByAssigneeAsync(string assignee)
        {
            var tasks = _conversations
                .Where(c => c.AiInsights?.ActionItems != null)
                .SelectMany(c => c.AiInsights!.ActionItems!)
                .Where(a => string.Equals(a.Assignee, assignee, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult(tasks);
        }

        /// <summary>
        /// Retrieves conversations containing a specific topic.
        /// </summary>
        public Task<List<Conversation>> GetConversationsByTopicAsync(string topic)
        {
            var results = _conversations
                .Where(c => c.AiInsights?.Topics != null &&
                            c.AiInsights.Topics
                            .Any(t => string.Equals(t, topic, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Task.FromResult(results);
        }

        /// <summary>
        /// Provides the dummy sample conversation.
        /// </summary>
        private Conversation GetSampleConversation()
        {
            var conversationId = Guid.NewGuid();
            return new Conversation
            {
                Id = conversationId,
                UserId = "user_123",
                Title = "Team Sync: Project Atlas Launch",
                CreatedAt = DateTime.Now.AddDays(-2),
                DurationMinutes = 1800,
                Location = "Conference Room A",
                Summary = "The team discussed the upcoming launch of Project Atlas...",
                Transcript = new List<TranscriptEntry>
                {
                    new()
                    {
                        SpeakerId = "alice",
                        SpeakerLabel = "Alice",
                        Text = "Let's finalize the launch checklist today.",
                        Initials = "AL",
                        Timestamp = DateTime.Now.AddDays(-2).AddMinutes(1)
                    },
                    new()
                    {
                        SpeakerId = "bob",
                        SpeakerLabel = "Bob",
                        Text = "We also need to confirm deployment slots.",
                        Initials = "BO",
                        Timestamp = DateTime.Now.AddDays(-2).AddMinutes(3)
                    },
                    new()
                    {
                        SpeakerId = "vikash",
                        SpeakerLabel = "You",
                        Text = "I’ll handle communication with the ops team.",
                        Initials = "VK",
                        Timestamp = DateTime.Now.AddDays(-2).AddMinutes(5)
                    }
                },
                AiInsights = new AiInsights
                {
                    Topics = new List<string>
                    {
                        "Project Launch",
                        "Deployment Strategy",
                        "Team Coordination"
                    },
                    ActionItems = new List<ActionItem>
                    {
                        new()
                        {
                            ConversationId = conversationId,
                            ConversationTitle = "Team Sync: Project Atlas Launch",
                            Task = "Finalize launch checklist",
                            Assignee = "Alice",
                            DueDate = "2025-06-15"
                        },
                        new()
                        {
                            ConversationId = conversationId,
                            ConversationTitle = "Team Sync: Project Atlas Launch",
                            Task = "Coordinate with ops for deployment",
                            Assignee = "Vikash",
                            DueDate = "2025-06-14"
                        },
                        new()
                        {
                            ConversationId = conversationId,
                            ConversationTitle = "Team Sync: Project Atlas Launch",
                            Task = "Confirm deployment slots",
                            Assignee = "Bob",
                            DueDate = "2025-06-13"
                        }
                    }
                },
                Timeline = new List<TimelineEvent>
                {
                    new() { Timestamp = "00:02", Description = "Checklist discussion started" },
                    new() { Timestamp = "00:08", Description = "Deployment responsibilities assigned" },
                    new() { Timestamp = "00:15", Description = "Ops communication planned" }
                }
            };
        }
    }
}
