using MudBlazor;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;
using System.Threading.Channels;
using Timer = System.Timers.Timer;

namespace SmartPendant.MAUIHybrid.Services;

/// <summary>
/// Manages the audio data pipeline from connection to transcription,
/// and includes an inactivity timer to automatically save and reset conversations.
/// </summary>
public class AudioPipelineManager : IAsyncDisposable
{
    #region Constants

    private const double INACTIVITY_TIMEOUT_SECONDS = 120;

    #endregion

    #region Fields

    private readonly IConnectionService _connectionService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly Channel<byte[]> _audioDataChannel;
    private CancellationTokenSource? _pipelineCts;
    private Timer? _inactivityTimer;
    private Task? _processingTask;

    #endregion

    #region Properties

    public Conversation CurrentConversation { get; private set; } = new();

    #endregion

    #region Events

    public event EventHandler? StateHasChanged;
    public event EventHandler<(string message, Severity severity)>? Notify;
    public event EventHandler? ConversationCompleted;

    #endregion

    #region Constructor

    public AudioPipelineManager(IConnectionService connectionService, ITranscriptionService transcriptionService)
    {
        _connectionService = connectionService;
        _transcriptionService = transcriptionService;
        CurrentConversation = new Conversation();

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
        Debug.WriteLine("Starting audio pipeline...");

        // Connect and initialize device
        var (connected, connEx) = await _connectionService.ConnectAsync();
        if (!connected)
        {
            var message = $"Failed to connect: {connEx?.Message ?? "Unknown error."}";
            Debug.WriteLine($"Error: {message}");
            return (false, message);
        }

        if (!await _connectionService.InitializeAsync())
        {
            const string message = "Failed to initialize Bluetooth service.";
            Debug.WriteLine($"Error: {message}");
            await _connectionService.DisconnectAsync();
            return (false, message);
        }

        CurrentConversation = new Conversation();
        await _transcriptionService.InitializeAsync(new WaveFormat(16000, 16, 1));

        SubscribeToEvents();

        _pipelineCts = new CancellationTokenSource();
        _processingTask = ProcessAudioDataFromChannelAsync(_pipelineCts.Token);

        StartInactivityTimer();

        Debug.WriteLine("Audio pipeline started successfully.");
        return (true, null);
    }

    public async Task StopPipelineAsync()
    {
        Debug.WriteLine("Stopping audio pipeline...");

        // Stop timer first to prevent race conditions.
        _inactivityTimer?.Stop();

        // Gracefully complete the pipeline.
        if (_pipelineCts is not null)
        {
            _audioDataChannel.Writer.TryComplete();
            if (_processingTask is not null) await _processingTask;

            _pipelineCts.Cancel();
            _pipelineCts.Dispose();
            _pipelineCts = null;
        }

        UnsubscribeFromEvents();

        // Finalize conversation if there's content.
        if (CurrentConversation.Transcript.Any())
        {
            Debug.WriteLine("Finalizing conversation upon stopping.");
            ConversationCompleted?.Invoke(this, EventArgs.Empty);
        }

        await _transcriptionService.StopAsync();
        await _connectionService.DisconnectAsync();

        Debug.WriteLine("Audio pipeline stopped.");
    }

    #endregion

    #region Event Subscription

    private void SubscribeToEvents()
    {
        _connectionService.DataReceived += OnDataReceived;
        _connectionService.ConnectionLost += OnConnectionLost;
        _transcriptionService.TranscriptReceived += OnTranscriptReceived;
        _transcriptionService.RecognizingTranscriptReceived += OnRecognizingTranscriptReceived;
    }

    private void UnsubscribeFromEvents()
    {
        _connectionService.DataReceived -= OnDataReceived;
        _connectionService.ConnectionLost -= OnConnectionLost;
        _transcriptionService.TranscriptReceived -= OnTranscriptReceived;
        _transcriptionService.RecognizingTranscriptReceived -= OnRecognizingTranscriptReceived;
    }

    #endregion

    #region Service Event Handlers

    private async void OnDataReceived(object? sender, byte[] data)
    {
        try
        {
            if (_pipelineCts is not null && !_pipelineCts.IsCancellationRequested)
            {
                await _audioDataChannel.Writer.WriteAsync(data, _pipelineCts.Token);
            }
        }
        catch (ChannelClosedException)
        {
            // This is expected when the pipeline is shutting down.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing to audio channel: {ex.Message}");
        }
    }

    private void OnConnectionLost(object? sender, string reason)
    {
        var message = $"Connection Lost: {reason}";
        Debug.WriteLine($"Warning: {message}");
        Notify?.Invoke(this, (message, Severity.Error));
        // Orchestrator should handle the high-level StopAsync call.
    }

    private void OnTranscriptReceived(object? sender, TranscriptEntry entry)
    {
        CurrentConversation.RecognizingEntry = null;
        CurrentConversation.Transcript.Add(entry);
        StateHasChanged?.Invoke(this, EventArgs.Empty);
        ResetInactivityTimer();
    }

    private void OnRecognizingTranscriptReceived(object? sender, TranscriptEntry entry)
    {
        CurrentConversation.RecognizingEntry = entry;
        StateHasChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnInactivityTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        Debug.WriteLine("Inactivity timer elapsed. Finalizing current conversation segment.");

        if (CurrentConversation.Transcript.Any())
        {
            ConversationCompleted?.Invoke(this, EventArgs.Empty);
        }

        // Reset for a new conversation segment.
        CurrentConversation = new Conversation();
        StateHasChanged?.Invoke(this, EventArgs.Empty);

        // The timer is AutoReset=false, so it stops. We restart it to monitor the new empty segment.
        _inactivityTimer?.Start();
    }

    #endregion

    #region Timer Management

    private void StartInactivityTimer()
    {
        _inactivityTimer?.Dispose();
        _inactivityTimer = new Timer(INACTIVITY_TIMEOUT_SECONDS * 1000)
        {
            AutoReset = false
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

    #region Private Helpers

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
        _inactivityTimer?.Dispose();
        _pipelineCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}