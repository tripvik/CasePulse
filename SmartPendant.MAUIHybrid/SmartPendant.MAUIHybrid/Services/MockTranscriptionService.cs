using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.Timers;

namespace SmartPendant.MAUIHybrid.Services
{
    /// <summary>
    /// Mock implementation of ITranscriptionService for simulating transcript events.
    /// </summary>
    public class MockTranscriptionService : ITranscriptionService
    {
        #region Events
        public event EventHandler<TranscriptEntry>? RecognizingTranscriptReceived;
        public event EventHandler<TranscriptEntry>? TranscriptReceived;
        #endregion

        #region Fields
        private List<TranscriptEntry> _mockTranscriptSource = new();
        private System.Timers.Timer? _simulationTimer;
        private bool _isRecording = false;
        private int _currentIndex = 0;
        #endregion

        #region Constructor
        public MockTranscriptionService()
        {
            var conversation = GetSampleConversations().FirstOrDefault();
            _mockTranscriptSource = [.. conversation!.Transcript];
        }
        #endregion

        #region Public Methods
        public async Task InitializeAsync(WaveFormat micFormat)
        {
            // Delay to simulate async initialization
            await Task.Delay(10);
            _isRecording = true;
            StartSimulation();
        }

        public Task ProcessChunkAsync(byte[] audioData)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _isRecording = false;
            _simulationTimer?.Stop();
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            _simulationTimer?.Dispose();
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
        #endregion

        #region Private Methods
        private void StartSimulation()
        {
            _simulationTimer = new System.Timers.Timer(1500);
            _simulationTimer.Elapsed += OnTimerElapsed;
            _simulationTimer.AutoReset = true;
            _simulationTimer.Enabled = true;
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!_isRecording || _mockTranscriptSource.Count == 0)
                return;

            var nextEntry = _mockTranscriptSource[Random.Shared.Next(_mockTranscriptSource.Count)];
            _currentIndex++;
            TranscriptReceived?.Invoke(this, nextEntry);
        }

        private List<ConversationModel> GetSampleConversations()
        {
            var conversations = new List<ConversationModel>();

            // Conversation 1: Project Atlas Launch
            var conversation1Id = Guid.NewGuid();
            conversations.Add(new ConversationModel
            {
                Id = conversation1Id,
                UserId = "user_123",
                Title = "Team Sync: Project Atlas Launch",
                CreatedAt = DateTime.Now.AddDays(-2),
                DurationMinutes = 45,
                Location = "Conference Room A",
                Summary = "The team discussed the upcoming launch of Project Atlas, finalizing the checklist and deployment strategy.",
                Transcript = new List<TranscriptEntry>
            {
                new() { SpeakerId = "alice", SpeakerLabel = "Alice", Text = "Let's finalize the launch checklist today.", Initials = "AL", Timestamp = DateTime.Now.AddDays(-2).AddMinutes(1) },
                new() { SpeakerId = "bob", SpeakerLabel = "Bob", Text = "We also need to confirm deployment slots.", Initials = "BO", Timestamp = DateTime.Now.AddDays(-2).AddMinutes(3) },
                new() { SpeakerId = "vikash", SpeakerLabel = "You", Text = "I’ll handle communication with the ops team.", Initials = "VK", Timestamp = DateTime.Now.AddDays(-2).AddMinutes(5) },
                new() { SpeakerId = "alice", SpeakerLabel = "Alice", Text = "Great, I'll update the status document by end of day.", Initials = "AL", Timestamp = DateTime.Now.AddDays(-2).AddMinutes(10) }
            },
                AiInsights = new AiInsights
                {
                    Topics = new List<string> { "Project Launch", "Deployment Strategy", "Team Coordination" },
                    ActionItems = new List<ActionItem>
                    {
                        new() {ConversationTitle = "Team Sync: Project Atlas Launch", Task = "Finalize launch checklist", Assignee = "Alice", DueDate = DateTime.Parse("2025-06-15") },
                        new() {ConversationTitle = "Team Sync: Project Atlas Launch", Task = "Coordinate with ops for deployment", Assignee = "Vikash", DueDate = DateTime.Parse("2025-06-14") },
                        new() {ConversationTitle = "Team Sync: Project Atlas Launch", Task = "Confirm deployment slots", Assignee = "Bob", DueDate = DateTime.Parse("2025-06-13") }
                    }
                },
                Timeline = new List<TimelineEvent>
            {
                new() { Timestamp = "00:02", Description = "Checklist discussion started" },
                new() { Timestamp = "00:08", Description = "Deployment responsibilities assigned" },
                new() { Timestamp = "00:15", Description = "Ops communication planned" },
                new() { Timestamp = "00:20", Description = "Status document update discussed" }
            }
            });

            // Conversation 2: Q3 Marketing Campaign Brainstorm
            var conversation2Id = Guid.NewGuid();
            conversations.Add(new ConversationModel
            {
                Id = conversation2Id,
                UserId = "user_456",
                Title = "Q3 Marketing Campaign Brainstorm",
                CreatedAt = DateTime.Now.AddDays(-5),
                DurationMinutes = 60,
                Location = "Online Meeting (Zoom)",
                Summary = "Brainstorming session for the Q3 marketing campaign, focusing on new ad channels and content ideas.",
                Transcript = new List<TranscriptEntry>
            {
                new() { SpeakerId = "charlie", SpeakerLabel = "Charlie", Text = "What new ad channels should we explore for Q3?", Initials = "CH", Timestamp = DateTime.Now.AddDays(-5).AddMinutes(2) },
                new() { SpeakerId = "diana", SpeakerLabel = "Diana", Text = "I think TikTok and Instagram Reels have great potential.", Initials = "DI", Timestamp = DateTime.Now.AddDays(-5).AddMinutes(5) },
                new() { SpeakerId = "vikash", SpeakerLabel = "You", Text = "We should also consider a series of short video testimonials.", Initials = "VK", Timestamp = DateTime.Now.AddDays(-5).AddMinutes(8) },
                new() { SpeakerId = "charlie", SpeakerLabel = "Charlie", Text = "Good idea, I'll research some tools for video creation.", Initials = "CH", Timestamp = DateTime.Now.AddDays(-5).AddMinutes(15) }
            },
                AiInsights = new AiInsights
                {
                    Topics = new List<string> { "Marketing Strategy", "Ad Channels", "Content Creation", "Video Marketing" },
                    ActionItems = new List<ActionItem>
                {
                    new() { ConversationTitle = "Q3 Marketing Campaign Brainstorm", Task = "Research new ad channels", Assignee = "Charlie", DueDate = DateTime.Parse("2025-06-18") },
                    new() { ConversationTitle = "Q3 Marketing Campaign Brainstorm", Task = "Draft video testimonial concept", Assignee = "You", DueDate = DateTime.Parse("2025-06-20") }
                }
                },
                Timeline = new List<TimelineEvent>
            {
                new() { Timestamp = "00:03", Description = "Discussion on new ad channels" },
                new() { Timestamp = "00:09", Description = "Video testimonial idea proposed" },
                new() { Timestamp = "00:16", Description = "Action item: research video tools" }
            }
            });

            // Conversation 3: Weekly Product Development Standup
            var conversation3Id = Guid.NewGuid();
            conversations.Add(new ConversationModel
            {
                Id = conversation3Id,
                UserId = "user_789",
                Title = "Weekly Product Development Standup",
                CreatedAt = DateTime.Now.AddDays(-1),
                DurationMinutes = 30,
                Location = "Dev Team Office",
                Summary = "Quick standup covering sprint progress, blockers, and upcoming tasks for the product development team.",
                Transcript = new List<TranscriptEntry>
            {
                new() { SpeakerId = "eve", SpeakerLabel = "Eve", Text = "I've completed the user authentication module.", Initials = "EV", Timestamp = DateTime.Now.AddDays(-1).AddMinutes(1) },
                new() { SpeakerId = "frank", SpeakerLabel = "Frank", Text = "Still working on the backend API integration, facing a minor dependency issue.", Initials = "FR", Timestamp = DateTime.Now.AddDays(-1).AddMinutes(3) },
                new() { SpeakerId = "vikash", SpeakerLabel = "You", Text = "I can help with the dependency issue after my current task.", Initials = "VK", Timestamp = DateTime.Now.AddDays(-1).AddMinutes(5) },
                new() { SpeakerId = "eve", SpeakerLabel = "Eve", Text = "Thanks, Vikash! That would be great.", Initials = "EV", Timestamp = DateTime.Now.AddDays(-1).AddMinutes(6) }
            },
                AiInsights = new AiInsights
                {
                    Topics = new List<string> { "Sprint Progress", "Blockers", "API Integration", "User Authentication" },
                    ActionItems = new List<ActionItem>
                {
                    new() { ConversationTitle = "Weekly Product Development Standup", Task = "Assist Frank with dependency issue", Assignee = "You", DueDate = DateTime.Parse("2025-06-14") },
                    new() { ConversationTitle = "Weekly Product Development Standup", Task = "Review user authentication module", Assignee = "Frank", DueDate = DateTime.Parse("2025-06-16") }
                }
                },
                Timeline = new List<TimelineEvent>
            {
                new() { Timestamp = "00:01", Description = "User authentication update" },
                new() { Timestamp = "00:04", Description = "Backend API blocker discussed" },
                new() { Timestamp = "00:05", Description = "Offer to assist with dependency" }
            }
            });

            // Conversation 4: Client Feedback Session - New Feature
            var conversation4Id = Guid.NewGuid();
            conversations.Add(new ConversationModel
            {
                Id = conversation4Id,
                UserId = "user_101",
                Title = "Client Feedback Session - New Feature",
                CreatedAt = DateTime.Now.AddDays(-7),
                DurationMinutes = 90,
                Location = "Client Site",
                Summary = "Gathering feedback from the client on the newly implemented reporting feature.",
                Transcript = new List<TranscriptEntry>
            {
                new() { SpeakerId = "grace", SpeakerLabel = "Grace (Client)", Text = "We really like the new dashboard, especially the customizable reports.", Initials = "GR", Timestamp = DateTime.Now.AddDays(-7).AddMinutes(2) },
                new() { SpeakerId = "vikash", SpeakerLabel = "You", Text = "That's great to hear! Are there any specific improvements you'd suggest?", Initials = "VK", Timestamp = DateTime.Now.AddDays(-7).AddMinutes(5) },
                new() { SpeakerId = "henry", SpeakerLabel = "Henry (Client)", Text = "A way to export reports directly to PDF would be very helpful.", Initials = "HE", Timestamp = DateTime.Now.AddDays(-7).AddMinutes(10) },
                new() { SpeakerId = "grace", SpeakerLabel = "Grace (Client)", Text = "And perhaps more filtering options for date ranges.", Initials = "GR", Timestamp = DateTime.Now.AddDays(-7).AddMinutes(12) }
            },
                AiInsights = new AiInsights
                {
                    Topics = new List<string> { "Client Feedback", "New Features", "Reporting", "Export Functionality" },
                    ActionItems = new List<ActionItem>
                {
                    new() { ConversationTitle = "Client Feedback Session - New Feature", Task = "Investigate PDF export for reports", Assignee = "You", DueDate = DateTime.Parse("2025-06-21") },
                    new() { ConversationTitle = "Client Feedback Session - New Feature", Task = "Add more date range filtering options", Assignee = "Development Team", DueDate = DateTime.Parse("2025-06-28") }
                }
                },
                Timeline = new List<TimelineEvent>
            {
                new() { Timestamp = "00:03", Description = "Positive feedback on dashboard" },
                new() { Timestamp = "00:11", Description = "Feature request: PDF export" },
                new() { Timestamp = "00:13", Description = "Feature request: improved date filters" }
            }
            });

            // Conversation 5: HR Onboarding Session
            var conversation5Id = Guid.NewGuid();
            conversations.Add(new ConversationModel
            {
                Id = conversation5Id,
                UserId = "user_222",
                Title = "HR Onboarding Session",
                CreatedAt = DateTime.Now.AddMonths(-1),
                DurationMinutes = 120,
                Location = "HR Department",
                Summary = "Onboarding session for new employees, covering company policies, benefits, and payroll.",
                Transcript = new List<TranscriptEntry>
            {
                new() { SpeakerId = "irene", SpeakerLabel = "Irene (HR)", Text = "Welcome to the team! Today we'll review essential company policies.", Initials = "IR", Timestamp = DateTime.Now.AddMonths(-1).AddMinutes(2) },
                new() { SpeakerId = "john", SpeakerLabel = "John (New Hire)", Text = "Could you clarify the vacation policy?", Initials = "JO", Timestamp = DateTime.Now.AddMonths(-1).AddMinutes(15) },
                new() { SpeakerId = "vikash", SpeakerLabel = "You", Text = "I'll send out a detailed FAQ document regarding benefits and time off.", Initials = "VK", Timestamp = DateTime.Now.AddMonths(-1).AddMinutes(20) },
                new() { SpeakerId = "irene", SpeakerLabel = "Irene (HR)", Text = "Perfect, Vikash. And please remember to complete your W-4 forms by Friday.", Initials = "IR", Timestamp = DateTime.Now.AddMonths(-1).AddMinutes(30) }
            },
                AiInsights = new AiInsights
                {
                    Topics = new List<string> { "HR Onboarding", "Company Policies", "Employee Benefits", "Payroll" },
                    ActionItems = new List<ActionItem>
                {
                    new() { ConversationTitle = "HR Onboarding Session", Task = "Send benefits and time off FAQ", Assignee = "You", DueDate = DateTime.Parse("2025-06-14") },
                    new() { ConversationTitle = "HR Onboarding Session", Task = "Complete W-4 forms", Assignee = "New Hires", DueDate = DateTime.Parse("2025-06-13") }
                }
                },
                Timeline = new List<TimelineEvent>
            {
                new() { Timestamp = "00:03", Description = "Welcome and policy overview" },
                new() { Timestamp = "00:16", Description = "Vacation policy question" },
                new() { Timestamp = "00:21", Description = "Action item: send FAQ document" },
                new() { Timestamp = "00:31", Description = "Reminder: W-4 forms" }
            }
            });

            return conversations;
        }
        #endregion
    }
}