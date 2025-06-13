using NAudio.Wave;
using Microsoft.Maui.Storage;
using SmartPendant.MAUIHybrid.Models;
using SmartPendant.MAUIHybrid.Helpers;

namespace SmartPendant.MAUIHybrid.Services
{
    internal class FileTranscriptionService : ITranscriptionService
    {
        public event EventHandler<TranscriptEntry>? RecognizingTranscriptReceived;
        public event EventHandler<TranscriptEntry>? TranscriptReceived;

        private readonly IStorageService _storageService;
        private readonly MemoryStream _memoryStream = new();

        private static bool IsDesktop =>
            DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.macOS;
            //false;

        public FileTranscriptionService(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public ValueTask DisposeAsync() => _memoryStream.DisposeAsync();

        public Task InitializeAsync(WaveFormat micFormat) => Task.CompletedTask;

        public async Task ProcessChunkAsync(byte[] audioData)
        {
            if (audioData == null) throw new ArgumentNullException(nameof(audioData));

            try
            {
                //convert audio data to Signed 16-bit PCM
                //var signedAudioData = AudioHelper.ConvertUnsigned8BitToSigned16Bit(audioData);

                await _memoryStream.WriteAsync(audioData.AsMemory());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessChunkAsync] Error writing mic chunk: {ex.Message}");
            }
        }

        public async Task StopAsync()
        {
            try
            {
                var audioBytes = _memoryStream.ToArray();
                if (audioBytes.Length == 0)
                {
                    Console.WriteLine("[StopAsync] No audio data to save.");
                    return;
                }

                var fileName = $"{Guid.NewGuid()}.pcm";

                if (IsDesktop)
                {
                    var directoryPath = Path.Combine(FileSystem.AppDataDirectory, "pcmfiles");
                    Directory.CreateDirectory(directoryPath);

                    var filePath = Path.Combine(directoryPath, fileName);
                    await File.WriteAllBytesAsync(filePath, audioBytes);

                    TranscriptReceived?.Invoke(this, new TranscriptEntry
                    {
                        Text = $"File saved locally: {filePath}",
                        Timestamp = DateTime.Now,
                        SpeakerLabel = nameof(FileTranscriptionService),
                        Initials = "FTS"
                    });
                }
                else
                {
                    using var stream = new MemoryStream(audioBytes);
                    var uri = await _storageService.UploadAudioAsync(stream, fileName);

                    TranscriptReceived?.Invoke(this, new TranscriptEntry
                    {
                        Text = $"File uploaded to Azure Blob Storage: {uri}",
                        Timestamp = DateTime.Now,
                        SpeakerLabel = nameof(FileTranscriptionService),
                        Initials = "FTS"
                    });
                }
                // Reset the memory stream for the next recording
                _memoryStream.SetLength(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StopAsync] Error saving audio: {ex.Message}");
            }
        }
    }
}
