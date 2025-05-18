using NAudio.Wave;
using SmartPendant.MAUIHybrid.Models;
using SmartPendant.MAUIHybrid.Services;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Components.Pages
{
    public partial class Home(IConnectionService bluetoothService, ITranscriptionService transcriptionService)
    {
        private readonly IConnectionService _bluetoothService = bluetoothService;
        private readonly ITranscriptionService _transcriptionService = transcriptionService;
        private List<ChatMessage> _messages = [];
        private bool _connected = false;

        private bool _stopRecording => !_connected;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await StartRecordingAsync();
            }
        }

        public async void StopRecording()
        {
            await _transcriptionService.StopAsync();
            _transcriptionService.TranscriptReceived -= OnTranscriptReceived;
            _transcriptionService.RecognizingTranscriptReceived -= OnRecognizingTranscriptReceived;
            _bluetoothService.DataReceived -= OnDataReceived;
            await _bluetoothService.DisconnectAsync();
            _connected = false;
            await InvokeAsync(StateHasChanged);
        }

        public async Task StartRecordingAsync()
        {
            var (connected, ex) = await _bluetoothService.ConnectAsync();
            if (!connected)
            {
                Debug.WriteLine($"Failed to connect: {ex?.Message}");
                return;
            }

            var initialized = await _bluetoothService.InitializeAsync();
            if (!initialized)
            {
                Debug.WriteLine("Failed to initialize Bluetooth characteristic or service.");
                return;
            }

            _bluetoothService.DataReceived += OnDataReceived;
            _transcriptionService.TranscriptReceived += OnTranscriptReceived;
            _transcriptionService.RecognizingTranscriptReceived += OnRecognizingTranscriptReceived;

            await _transcriptionService.InitializeAsync(new WaveFormat(24000, 16, 1));
            _connected = true;
            await InvokeAsync(StateHasChanged);
        }

        private async void OnDataReceived(object? sender, byte[] data)
        {
            try
            {
                //purposefully fire and forget to allow the event to return
                BufferAndSendAsync(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing Bluetooth data: {ex.Message}");
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
    }
}
