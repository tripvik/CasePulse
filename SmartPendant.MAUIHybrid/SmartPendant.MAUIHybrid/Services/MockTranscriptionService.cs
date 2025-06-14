using System;
using System.Collections.Generic;
using System.Timers;
using System.Threading.Tasks;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Models;
using SmartPendant.MAUIHybrid.Abstractions;

namespace SmartPendant.MAUIHybrid.Services
{
    /// <summary>
    /// Mock implementation of ITranscriptionService for simulating transcript events.
    /// </summary>
    public class MockTranscriptionService : ITranscriptionService
    {
        public event EventHandler<TranscriptEntry>? RecognizingTranscriptReceived;
        public event EventHandler<TranscriptEntry>? TranscriptReceived;

        private List<TranscriptEntry> _mockTranscriptSource = new();
        private System.Timers.Timer? _simulationTimer;
        private bool _isRecording = false;
        private int _currentIndex = 0;
        private readonly ConversationService _conversationService;

        public MockTranscriptionService(ConversationService conversationService)
        {
            _conversationService = conversationService;
            var conversation = _conversationService.GetSampleConversationAsync().Result;
            _mockTranscriptSource = conversation.Transcript.ToList();
        }

        public async Task InitializeAsync(WaveFormat micFormat)
        {
            _isRecording = true;
            StartSimulation();
        }

        public Task ProcessChunkAsync(byte[] audioData)
        {
            // No-op for mock
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _isRecording = false;
            _simulationTimer?.Stop();
            return Task.CompletedTask;
        }

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

        public ValueTask DisposeAsync()
        {
            _simulationTimer?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
