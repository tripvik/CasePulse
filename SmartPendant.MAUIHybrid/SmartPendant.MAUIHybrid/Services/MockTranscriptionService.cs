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
        private readonly ConversationService _conversationService;
        #endregion

        #region Constructor
        public MockTranscriptionService(ConversationService conversationService)
        {
            _conversationService = conversationService;
            var conversation = _conversationService.GetSampleConversationAsync().Result;
            _mockTranscriptSource = [.. conversation.Transcript];
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
        #endregion
    }
}