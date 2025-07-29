using MudBlazor;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;
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
    private readonly IAudioStorageService _audioStorageService;
    private static readonly WaveFormat DefaultWaveFormat = new WaveFormat(16000, 16, 1);
    private readonly Channel<byte[]> _audioDataChannel;
    private CancellationTokenSource? _pipelineCts;
    private Timer? _inactivityTimer;
    private Task? _processingTask;

    #endregion

    #region Properties

    public ConversationRecord CurrentConversation { get; private set; } = new();
    public DayRecord CurrentDay { get; private set; } = new();
    #endregion

    #region Events

    public event EventHandler? StateHasChanged;
    public event EventHandler<(string message, Severity severity)>? Notify;
    public event EventHandler? ConversationCompleted;
    public event EventHandler<(bool isRecording, bool isDeviceConnected, bool isStateChanging)>? SetStateEvent;

    #endregion

    #region Constructor

    public AudioPipelineManager(IConnectionService connectionService, ITranscriptionService transcriptionService, IAudioStorageService audioStorageService)
    {
        _connectionService = connectionService;
        _transcriptionService = transcriptionService;
        _audioStorageService = audioStorageService;
        CurrentConversation = new ConversationRecord();

        _audioDataChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(5000)
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
        //moved to clear conversation regardless of successful subsequent device connection.
        CurrentConversation = new ConversationRecord();
        // Connect and initialize device
        var (connected, connEx) = await _connectionService.ConnectAsync();
        if (!connected)
        {
            var message = $"Failed to connect: {connEx?.Message ?? "Unknown error."}";
            Debug.WriteLine($"Error: {message}");
            Notify?.Invoke(this, (message, Severity.Error));
            return (false, message);
        }

        if (!await _connectionService.InitializeAsync())
        {
            const string message = "Failed to initialize Bluetooth service.";
            Debug.WriteLine($"Error: {message}");
            await _connectionService.DisconnectAsync();
            Notify?.Invoke(this, (message, Severity.Error));
            return (false, message);
        }
        SetStateEvent?.Invoke(this, (isRecording: false, isDeviceConnected: true, isStateChanging: true));
        await _transcriptionService.InitializeAsync(DefaultWaveFormat);
        await _audioStorageService.InitializeAsync(DefaultWaveFormat);
        SubscribeToEvents();
        _pipelineCts = new CancellationTokenSource();
        _processingTask = ProcessAudioDataFromChannelAsync(_pipelineCts.Token);
        SetStateEvent?.Invoke(this, (isRecording: true, isDeviceConnected: true, isStateChanging: false));
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
            //Once the channel is completed, it cannot be reused. 
            //_audioDataChannel.Writer.TryComplete();
            //if (_processingTask is not null) await _processingTask;

            _pipelineCts.Cancel();
            _pipelineCts.Dispose();
            _pipelineCts = null;
        }

        UnsubscribeFromEvents();

        // Finalize conversation if there's content.
        if (CurrentConversation.Transcript.Any())
        {
            var (filepath,exception) = await _audioStorageService.StopAsync(CurrentConversation.Id.ToString());
            if (exception != null)
            {
                Debug.WriteLine($"Error stopping audio storage: {exception.Message}");
                Notify?.Invoke(this, ($"Failed to save audio file: {exception.Message}", Severity.Error));
            }
            else
            {
                CurrentConversation.AudioFilePath = filepath;
                Debug.WriteLine($"Audio file saved at: {filepath}");
            }
            Debug.WriteLine("Finalizing conversation upon stopping.");
            if (CurrentConversation.CreatedAt.Date == DateTime.Now.Date)
            {
                CurrentDay.Conversations.Add(CurrentConversation);
            }
            else
            {
                CurrentDay = new DayRecord { Date = DateTime.Now.Date, Conversations = { CurrentConversation } };
            }

            ConversationCompleted?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            await _audioStorageService.StopAsync();
        }
            
        await _transcriptionService.StopAsync();
        await _connectionService.DisconnectAsync();
        SetStateEvent?.Invoke(this, (isRecording: false, isDeviceConnected: false, isStateChanging: false));
        Debug.WriteLine("Audio pipeline stopped.");
    }

    #endregion

    #region Event Subscription

    private void SubscribeToEvents()
    {
        _connectionService.DataReceived += OnDataReceived;
        _connectionService.ConnectionLost += async (sender, reason) => await OnConnectionLost(sender, reason);
        //Disconnected event is fired in case on intentional disconnection, like when the user clicks on stop recording button. Hence commented out.
        //_connectionService.Disconnected += async (sender, reason) => await OnConnectionLost(sender, reason);
        _transcriptionService.TranscriptReceived += OnTranscriptReceived;
        _transcriptionService.RecognizingTranscriptReceived += OnRecognizingTranscriptReceived;
    }

    private void UnsubscribeFromEvents()
    {
        _connectionService.DataReceived -= OnDataReceived;
        _connectionService.ConnectionLost -= async (sender, reason) => await OnConnectionLost(sender, reason);
        // _connectionService.Disconnected +=  async (sender, reason) => await OnConnectionLost(sender, reason);
        _transcriptionService.TranscriptReceived -= OnTranscriptReceived;
        _transcriptionService.RecognizingTranscriptReceived -= OnRecognizingTranscriptReceived;
    }

    private async void OnDataReceived(object? sender, byte[] data)
    {
        try
        {
            if (_audioDataChannel?.Writer != null && _pipelineCts?.Token != null)
            {
                await _audioDataChannel.Writer.WriteAsync(data, _pipelineCts.Token);
            }
            else
            {
                Debug.WriteLine("Audio data channel writer or cancellation token is null.");
            }
            await _audioStorageService.ProcessChunkAsync(data);
        }
        catch (OperationCanceledException)
        {
            // This is expected when the pipeline is shutting down.
            return;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing to audio channel: {ex.Message}");
        }
    }

    private async Task OnConnectionLost(object? sender, string reason)
    {
        var message = $"Connection Lost: {reason}";
        Debug.WriteLine($"Warning: {message}");
        Notify?.Invoke(this, (message, Severity.Error));
        await StopPipelineAsync();
        SetStateEvent?.Invoke(this, (isRecording: false, isDeviceConnected: false, isStateChanging: false));
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

        if (CurrentConversation.Transcript.Count != 0)
        {
            if (CurrentConversation.CreatedAt.Date == DateTime.Now.Date)
            {
                CurrentDay.Conversations.Add(CurrentConversation);
            }
            else
            {
                CurrentDay = new DayRecord { Date = DateTime.Now.Date, Conversations = { CurrentConversation } };
            }

            ConversationCompleted?.Invoke(this, EventArgs.Empty);
        }

        // Reset for a new conversation segment.
        CurrentConversation = new ConversationRecord();
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