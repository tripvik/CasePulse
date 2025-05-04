using NAudio.Wave;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using SmartPendant.MAUIHybrid.Services;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Components.Pages
{
    public partial class Home(ITranscriptionService transcriptionService, BLEService bleService)
    {
        private readonly ITranscriptionService _trancriptionService = transcriptionService;
        private readonly BLEService _bleService = bleService;
        private MemoryStream _buffer = new MemoryStream();
        private bool _connected = false;
        // stop recording will inverse of connected
        private bool _stopRecording
        {
            get => !_connected;
        }

        protected override async Task OnAfterRenderAsync(bool first)
        {
            await base.OnAfterRenderAsync(first);
            if (first)
            {
                var (connectionResult,exception) = await _bleService.ConnectToPendant();
                if(connectionResult)
                {
                    var (characteristicResult, characteristic) = await _bleService.GetCharacteristic();
                    if (characteristicResult && characteristic != null)
                    {
                        characteristic.ValueUpdated += async (o, args) =>
                        {
                            try
                            {
                                var bytes = args.Characteristic.Value;
                                //implement buffer before sending to Transcription Service
                                await BufferAndSendToAzureAsync(bytes);
                            }
                            catch (Exception ex)
                            {
                                // Log or handle the exception properly
                                Console.WriteLine($"Error processing chunk: {ex}");
                            }
                        };
                        _trancriptionService.TranscriptReceived += (o, args) =>
                        {
                            Debug.WriteLine($"Transcript: {args}");
                        };
                        _trancriptionService.RecognizingTranscriptReceived += (o, args) =>
                        {
                            Debug.WriteLine($"Recognizing: {args}...");
                        };
                        await _trancriptionService.InitializeAsync(new WaveFormat(16000, 8, 1));
                        await characteristic.StartUpdatesAsync();
                        _connected = true;
                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
        }

        public async void StopRecording()
        {

            await _trancriptionService.StopAsync();
            await _bleService.DisconnectFromPendant();
            _connected = false;
            //disconnect from the device
        }

        private async Task BufferAndSendToAzureAsync(byte[] newData)
        {
            _buffer.Write(newData, 0, newData.Length);

            if (_buffer.Length >= 10000) // e.g., 100ms of 32kHz mono 8-bit audio
            {
                var chunk = _buffer.ToArray();
                await _trancriptionService.ProcessChunkAsync(chunk);
                _buffer.SetLength(0); // clear buffer
            }
        }
    }
}
