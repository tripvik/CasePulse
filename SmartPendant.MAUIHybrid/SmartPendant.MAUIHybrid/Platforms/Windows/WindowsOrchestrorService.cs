using InTheHand.Net.Bluetooth;
using MudBlazor;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPendant.MAUIHybrid.Platforms.Windows
{
    public class WindowsOrchestrorService(IConnectionService bluetoothService, ITranscriptionService transcriptionService) : IOrchestrationService
    {
        #region Implementation Details
        public bool IsRecording { get; set; } = false;

        public bool IsDeviceConnected { get; set; } = false;

        public bool StateChanging { get; private set; } = false;

        public Conversation CurrentConversation { get; private set; } = new();

        public event EventHandler? StateHasChanged;
        public event EventHandler<(string message, Severity severity)>? Notify;

        #endregion

        #region Privates
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

        #endregion

        public async Task StartAsync()
        {
            StateChanging = true;
            StateHasChanged?.Invoke(this,EventArgs.Empty);
            var (connected, ex) = await _bluetoothService.ConnectAsync();
            if (!connected)
            {
                var message = $"Failed to connect: {ex?.Message}";
                Debug.WriteLine(message);
                Notify?.Invoke(this,(message, Severity.Error));
                StateChanging = false;
                IsDeviceConnected = false;
                IsRecording = false;
                StateHasChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            var initialized = await _bluetoothService.InitializeAsync();
            if (!initialized)
            {
                Debug.WriteLine("Failed to initialize Bluetooth characteristic or service.");
                Notify?.Invoke(this, ("Failed to initialize Bluetooth service.", Severity.Error));
                StateChanging = false;
                IsDeviceConnected = false;
                IsRecording = false;
                StateHasChanged?.Invoke(this, EventArgs.Empty);
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
            IsDeviceConnected = true;
            IsRecording = true;
            StateChanging = false;
            StateHasChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task StopAsync()
        {
            StateChanging = true;
            StateHasChanged?.Invoke(this, EventArgs.Empty);
            _processingCts?.Cancel();
            _processingCts = null;
            await _transcriptionService.StopAsync();
            _transcriptionService.TranscriptReceived -= OnTranscriptReceived;
            _transcriptionService.RecognizingTranscriptReceived -= OnRecognizingTranscriptReceived;
            _bluetoothService.ConnectionLost -= OnDisconnected;
            _bluetoothService.Disconnected -= OnDisconnected;
            _bluetoothService.DataReceived -= OnDataReceived;
            await _bluetoothService.DisconnectAsync();
            IsDeviceConnected = false;
            IsRecording = false;
            StateChanging = false;
            StateHasChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnRecognizingTranscriptReceived(object? sender, TranscriptEntry message)
        {
             /*
              * Not needed for now
             */
        }

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
            //recognizingEntry = null;
            CurrentConversation.Transcript.Add(message);
            StateHasChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void OnDisconnected(object? sender, string e)
        {
            var message = $"Connection Error: {e}";
            Debug.WriteLine(message);
            Notify?.Invoke(this, (message, Severity.Error));
            await StopAsync(); // Stop the transcription service and disconnect
            IsDeviceConnected = false;
            IsRecording = false;
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
    }
}
