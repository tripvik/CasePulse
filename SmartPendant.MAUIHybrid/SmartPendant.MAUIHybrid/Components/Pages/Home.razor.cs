using Microsoft.AspNetCore.Components;
using MudBlazor;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Models;
using SmartPendant.MAUIHybrid.Services;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Components.Pages
{
    public partial class Home(IConnectionService bluetoothService, ITranscriptionService transcriptionService)
    {
        [Inject]
        private ISnackbar Snackbar { get; set; } = default!;
        private readonly IConnectionService _bluetoothService = bluetoothService;
        private readonly ITranscriptionService _transcriptionService = transcriptionService;
        private List<ChatMessage> _messages = [];
        private bool _connected = false;
        private static readonly int _boundedCapacity = 500; // Adjust 
        private readonly System.Threading.Channels.Channel<byte[]> _audioDataChannel = System.Threading.Channels.Channel.CreateBounded<byte[]>(
        new System.Threading.Channels.BoundedChannelOptions(_boundedCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait
        });
        private CancellationTokenSource? _processingCts;

        protected override void OnInitialized()
        {
            // By this point, Snackbar has been injected and is available
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomCenter;

            base.OnInitialized();
        }

        private bool _stopRecording => !_connected;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await StartRecordingAsync();
            }
        }

        public async Task StopRecordingAsync()
        {
            // Cancel the processing loop
            _processingCts?.Cancel();
            _processingCts = null;
            await _transcriptionService.StopAsync();
            _transcriptionService.TranscriptReceived -= OnTranscriptReceived;
            _transcriptionService.RecognizingTranscriptReceived -= OnRecognizingTranscriptReceived;
            _bluetoothService.DataReceived -= OnDataReceived;
            _bluetoothService.ConnectionLost -= OnDisconnected;
            _bluetoothService.Disconnected -= OnDisconnected;
            await _bluetoothService.DisconnectAsync();
            _connected = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task StartRecordingAsync()
        {
            var (connected, ex) = await _bluetoothService.ConnectAsync();
            if (!connected)
            {
                var message = $"Failed to connect: {ex?.Message}";
                Debug.WriteLine(message);
                Notify(message, Severity.Error);
                return;
            }

            var initialized = await _bluetoothService.InitializeAsync();
            if (!initialized)
            {
                Debug.WriteLine("Failed to initialize Bluetooth characteristic or service.");
                Notify("Failed to initialize Bluetooth service.", Severity.Error);
                return;
            }

            _processingCts = new CancellationTokenSource();
            _ = ProcessAudioDataAsync(_processingCts.Token);

            _bluetoothService.DataReceived += OnDataReceived;
            _bluetoothService.ConnectionLost += OnDisconnected;
            _bluetoothService.Disconnected += OnDisconnected;
            _transcriptionService.TranscriptReceived += OnTranscriptReceived;
            _transcriptionService.RecognizingTranscriptReceived += OnRecognizingTranscriptReceived;

            await _transcriptionService.InitializeAsync(new WaveFormat(16000, 16, 1));
            _connected = true;
            await InvokeAsync(StateHasChanged);
        }

        private async void OnDisconnected(object? sender, string e)
        {
            var message = $"Connection Error: {e}";
            Debug.WriteLine(message);
            Notify(message, Severity.Error);
            await InvokeAsync(StopRecordingAsync);
        }

        private async void OnDataReceived(object? sender, byte[] data)
        {
            try
            {
                // Copy the data to avoid potential issues with reused buffers?
                await _audioDataChannel.Writer.WriteAsync(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing Bluetooth data: {ex.Message}");
            }
        }

        // Background processing loop
        private async Task ProcessAudioDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var audioData in _audioDataChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        await BufferAndSendAsync(audioData);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing audio data: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in audio processing loop: {ex.Message}");
            }
        }

        private async void OnTranscriptReceived(object? sender, ChatMessage message)
        {
            Debug.WriteLine($"Transcript: {message.User} - {message.Message}");
            _messages.Add(message);
            await InvokeAsync(StateHasChanged);
        }

        private void OnRecognizingTranscriptReceived(object? sender, ChatMessage message)
        {
            Debug.WriteLine($"Recognizing: {message.Message}...");
        }
        private async Task BufferAndSendAsync(byte[] newData)
        {
            await _transcriptionService.ProcessChunkAsync(newData);
            //_buffer.Write(newData, 0, newData.Length);

            //if (_buffer.Length >= 10000) // e.g., 100ms of 32kHz mono 8-bit audio
            //{
            //    var chunk = _buffer.ToArray();

            //    _buffer.SetLength(0); // clear buffer
            //}
        }

        private void Notify(string message, Severity severity)
        {
            Snackbar.Add(message, severity);
        }
    }
}
