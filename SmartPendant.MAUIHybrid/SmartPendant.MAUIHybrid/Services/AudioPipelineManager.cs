using MudBlazor;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;
using System.Threading.Channels;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SmartPendant.MAUIHybrid.Services
{
    /// <summary>
    /// Manages the audio data pipeline from connection to transcription,
    /// and includes an inactivity timer to automatically save and reset conversations.
    /// </summary>
    public class AudioPipelineManager : IAsyncDisposable
    {
        #region Fields
        private const double INACTIVITY_TIMEOUT_SECONDS = 300;

        private readonly IConnectionService _connectionService;
        private readonly ITranscriptionService _transcriptionService;

        private readonly Channel<byte[]> _audioDataChannel;
        private CancellationTokenSource? _pipelineCts;
        private Timer? _inactivityTimer;

        public Conversation CurrentConversation { get; private set; } = new();
        #endregion

        #region Events
        public event EventHandler? StateHasChanged;
        public event EventHandler<(string message, Severity severity)>? Notify;
        public event EventHandler<Conversation>? ConversationCompleted;
        #endregion

        #region Constructor
        public AudioPipelineManager(IConnectionService connectionService, ITranscriptionService transcriptionService)
        {
            _connectionService = connectionService;
            _transcriptionService = transcriptionService;

            // Using a channel to decouple the bluetooth data receiver from the transcription processor.
            _audioDataChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(500)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });
        }
        #endregion

        #region Public Control Methods
        public async Task<(bool success, string? errorMessage)> StartPipelineAsync()
        {
            // 1. Connect to Device
            var (connected, ex) = await _connectionService.ConnectAsync();
            if (!connected)
            {
                var message = $"Failed to connect: {ex?.Message ?? "Unknown error."}";
                Debug.WriteLine(message);
                return (false, message);
            }

            // 2. Initialize Device Characteristics
            var initialized = await _connectionService.InitializeAsync();
            if (!initialized)
            {
                var message = "Failed to initialize Bluetooth service.";
                Debug.WriteLine(message);
                return (false, message);
            }

            CurrentConversation = new Conversation { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };

            // 3. Initialize Transcription Service
            await _transcriptionService.InitializeAsync(new WaveFormat(16000, 16, 1));

            // 4. Subscribe to events
            SubscribeToEvents();

            // 5. Start background processing task
            _pipelineCts = new CancellationTokenSource();
            _ = ProcessAudioDataFromChannelAsync(_pipelineCts.Token);

            // 6. Start the inactivity timer
            StartInactivityTimer();

            return (true, null);
        }

        public async Task StopPipelineAsync()
        {
            // Stop the timer first to prevent race conditions.
            _inactivityTimer?.Stop();
            _inactivityTimer?.Dispose();
            _inactivityTimer = null;

            // Save the current conversation if it has content before stopping.
            if (CurrentConversation != null && CurrentConversation.Transcript.Any())
            {
                Debug.WriteLine("StopPipelineAsync called. Saving final conversation...");
                if (CurrentConversation.Transcript.Any())
                {
                    ConversationCompleted?.Invoke(this, CurrentConversation);
                }
            }

            // Cancel the processing loop
            if (_pipelineCts is not null)
            {
                _pipelineCts.Cancel();
                _pipelineCts.Dispose();
                _pipelineCts = null;
            }

            // Unsubscribe from events
            UnsubscribeFromEvents();

            // Stop services in reverse order of start
            await _transcriptionService.StopAsync();
            await _connectionService.DisconnectAsync();
        }
        #endregion

        #region Event Subscription
        private void SubscribeToEvents()
        {
            _connectionService.DataReceived += OnDataReceived;
            _connectionService.ConnectionLost += OnConnectionLost;
            _connectionService.Disconnected += OnDisconnected;
            _transcriptionService.TranscriptReceived += OnTranscriptReceived;
            _transcriptionService.RecognizingTranscriptReceived += OnRecognizingTranscriptReceived;
        }

        private void UnsubscribeFromEvents()
        {
            _connectionService.DataReceived -= OnDataReceived;
            _connectionService.ConnectionLost -= OnConnectionLost;
            _connectionService.Disconnected -= OnDisconnected;
            _transcriptionService.TranscriptReceived -= OnTranscriptReceived;
            _transcriptionService.RecognizingTranscriptReceived -= OnRecognizingTranscriptReceived;
        }
        #endregion

        #region Event Handlers
        private async void OnDataReceived(object? sender, byte[] data)
        {
            try
            {
                // Write received data to the channel for processing on a separate thread.
                await _audioDataChannel.Writer.WriteAsync(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to audio channel: {ex.Message}");
            }
        }

        private void OnConnectionLost(object? sender, string reason)
        {
            var message = $"Connection Lost: {reason}";
            Debug.WriteLine(message);
            Notify?.Invoke(this, (message, Severity.Error));
            // The Orchestrator should handle the high-level StopAsync call.
        }

        private void OnDisconnected(object? sender, string reason)
        {
            Debug.WriteLine($"Disconnected: {reason}");
        }

        private void OnTranscriptReceived(object? sender, TranscriptEntry entry)
        {
            CurrentConversation.RecognizingEntry = null;
            CurrentConversation.Transcript.Add(entry);
            StateHasChanged?.Invoke(this, EventArgs.Empty);

            // Any new transcript resets the inactivity timer.
            ResetInactivityTimer();
        }

        private void OnRecognizingTranscriptReceived(object? sender, TranscriptEntry entry)
        {
            CurrentConversation.RecognizingEntry = entry;
            StateHasChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnInactivityTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            Debug.WriteLine("Inactivity timer elapsed. Checking for conversation to save.");

            // 1. Save the conversation if it has content
            if (CurrentConversation.Transcript.Any())
            {
                ConversationCompleted?.Invoke(this, CurrentConversation);
            }

            // 2. Reset for a new conversation
            CurrentConversation = new Conversation { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };

            // 3. Notify the UI to update/clear the old transcript
            ConversationCompleted?.Invoke(this, null!);

            // 4. Restart the timer to monitor the new conversation for inactivity
            _inactivityTimer?.Start();
            Debug.WriteLine("Conversation reset. Inactivity timer restarted.");
        }
        #endregion

        #region Timer Management
        private void StartInactivityTimer()
        {
            _inactivityTimer = new Timer(INACTIVITY_TIMEOUT_SECONDS * 1000)
            {
                AutoReset = false // We only want it to fire once per period of inactivity.
            };
            _inactivityTimer.Elapsed += OnInactivityTimerElapsed;
            _inactivityTimer.Start();
            Debug.WriteLine("Inactivity timer started.");
        }

        private void ResetInactivityTimer()
        {
            if (_inactivityTimer != null)
            {
                _inactivityTimer.Stop();
                _inactivityTimer.Start();
            }
        }
        #endregion

        #region Private Methods
        private async Task ProcessAudioDataFromChannelAsync(CancellationToken token)
        {
            Debug.WriteLine("Audio processing loop started.");
            try
            {
                await foreach (var audioData in _audioDataChannel.Reader.ReadAllAsync(token))
                {
                    await _transcriptionService.ProcessChunkAsync(audioData);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Audio processing loop canceled normally.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unhandled exception in audio processing loop: {ex.Message}");
                Notify?.Invoke(this, ("A critical error occurred in the audio pipeline.", Severity.Error));
            }
            finally
            {
                Debug.WriteLine("Audio processing loop finished.");
            }
        }
        #endregion

        #region IAsyncDisposable
        public async ValueTask DisposeAsync()
        {
            await StopPipelineAsync();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}