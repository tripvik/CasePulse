using Microsoft.AspNetCore.Components;
using MudBlazor;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Components.Pages
{
    public partial class Home(IConnectionService bluetoothService, ITranscriptionService transcriptionService)
    {
        [Inject]
        private ISnackbar Snackbar { get; set; } = default!;
        // Suppress unused parameter warnings
        private readonly IConnectionService _bluetoothService = bluetoothService;
        private readonly ITranscriptionService _transcriptionService = transcriptionService;


        private static readonly int _boundedCapacity = 500; // Adjust 
        private readonly System.Threading.Channels.Channel<byte[]> _audioDataChannel = System.Threading.Channels.Channel.CreateBounded<byte[]>(
        new System.Threading.Channels.BoundedChannelOptions(_boundedCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait
        });
        private CancellationTokenSource? _processingCts;

        #region State
        private bool isRecording = false;
        private bool stateChanging = false;
        private bool isDeviceConnected = false;
        private List<TranscriptEntry> transcriptEntries = new();
        private TranscriptEntry? recognizingEntry;
        #endregion

        #region Lifecycle
        protected override void OnInitialized()
        {
            // By this point, Snackbar has been injected and is available
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomCenter;

            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await StartRecordingAsync();
            }
        }

        #endregion

        #region UI Actions
        private async Task ToggleRecording()
        {
            isRecording = !isRecording;
            if (isRecording)
            {
                await StartRecordingAsync();
            }
            else
            {
                await StopServicesAndDisconnect();
            }
            StateHasChanged();
        }
        #endregion

        #region Real Service Logic

        // Currently, the device will start sending data as soon as the connection is established.
        public async Task StartRecordingAsync()
        {
            stateChanging = true;
            await InvokeAsync(StateHasChanged);
            var (connected, ex) = await _bluetoothService.ConnectAsync();
            if (!connected)
            {
                var message = $"Failed to connect: {ex?.Message}";
                Debug.WriteLine(message);
                Notify(message, Severity.Error);
                stateChanging = false;
                isDeviceConnected = false;
                isRecording = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            var initialized = await _bluetoothService.InitializeAsync();
            if (!initialized)
            {
                Debug.WriteLine("Failed to initialize Bluetooth characteristic or service.");
                Notify("Failed to initialize Bluetooth service.", Severity.Error);
                stateChanging = false;
                stateChanging = false;
                isDeviceConnected = false;
                isRecording = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            _processingCts = new CancellationTokenSource();
            _ = ProcessAudioDataAsync(_processingCts.Token);

            _bluetoothService.DataReceived += OnDataReceived;
            _transcriptionService.TranscriptReceived += OnTranscriptReceived;
            _bluetoothService.ConnectionLost += OnDisconnected;
            _bluetoothService.Disconnected += OnDisconnected;
            _transcriptionService.RecognizingTranscriptReceived += OnRecognizingTranscriptReceived;
            await _transcriptionService.InitializeAsync(new WaveFormat(16000, 16, 1));
            isDeviceConnected = true;
            isRecording = true;
            stateChanging = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task StopServicesAndDisconnect()
        {
            stateChanging = true;
            await InvokeAsync(StateHasChanged);
            _processingCts?.Cancel();
            _processingCts = null;
            await _transcriptionService.StopAsync();
            _transcriptionService.TranscriptReceived -= OnTranscriptReceived;
            _transcriptionService.RecognizingTranscriptReceived -= OnRecognizingTranscriptReceived;
            _bluetoothService.ConnectionLost -= OnDisconnected;
            _bluetoothService.Disconnected -= OnDisconnected;
            _bluetoothService.DataReceived -= OnDataReceived;
            await _bluetoothService.DisconnectAsync();
            isDeviceConnected = false;
            isRecording = false;
            stateChanging = false;
            await InvokeAsync(StateHasChanged);
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
                Debug.WriteLine($"Error processing data: {ex.Message}");
            }
        }

        private void OnTranscriptReceived(object? sender, TranscriptEntry message)
        {
            InvokeAsync(() =>
            {
                recognizingEntry = null;
                transcriptEntries.Add(message);
                StateHasChanged();
            });
        }

        private async void OnDisconnected(object? sender, string e)
        {
            var message = $"Connection Error: {e}";
            Debug.WriteLine(message);
            Notify(message, Severity.Error);
            await InvokeAsync(StopServicesAndDisconnect);
            isDeviceConnected = false;
            isRecording = false;
        }

        private void OnRecognizingTranscriptReceived(object? sender, TranscriptEntry message)
        {
            InvokeAsync(() =>
            {
                recognizingEntry = message;
                StateHasChanged();
            });
        }
        #endregion

        #region Disposal
        public async void Dispose()
        {
            if (isDeviceConnected)
            {
                await StopServicesAndDisconnect();
            }
        }
        #endregion

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