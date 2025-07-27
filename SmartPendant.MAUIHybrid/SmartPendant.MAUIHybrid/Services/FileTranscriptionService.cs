using Microsoft.Maui.Storage;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.IO;

namespace SmartPendant.MAUIHybrid.Services
{
    internal class FileTranscriptionService : ITranscriptionService
    {
        public event EventHandler<TranscriptEntry>? RecognizingTranscriptReceived;
        public event EventHandler<TranscriptEntry>? TranscriptReceived;

        private readonly IStorageService _storageService;
        private readonly MemoryStream _memoryStream = new();
        private static readonly WaveFormat? _waveFormat = new(16000,16,1);

        // Cache the platform check result instead of computing it repeatedly
        private static readonly bool IsDesktop =
                DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.macOS;

        // Pre-compute constants to avoid repeated allocations
        private static readonly string ServiceName = nameof(FileTranscriptionService);
        private const string ServiceInitials = "FTS";
        private const string PcmDirectoryName = "wavfiles";

        public FileTranscriptionService(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public ValueTask DisposeAsync() => _memoryStream.DisposeAsync();

        public Task InitializeAsync(WaveFormat micFormat) => Task.CompletedTask;

        public async Task ProcessChunkAsync(byte[] audioData)
        {
            ArgumentNullException.ThrowIfNull(audioData);

            try
            {
                await _memoryStream.WriteAsync(audioData);
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
                if (_memoryStream.Length == 0)
                {
                    Console.WriteLine("[StopAsync] No audio data to save.");
                    return;
                }

                var fileName = $"{Guid.NewGuid()}.wav";
                var timestamp = DateTime.Now;

                var message = IsDesktop
                    ? await SaveToLocalFileAsync(_memoryStream, fileName)
                    : await UploadToBlobStorageAsync(_memoryStream, fileName);

                RaiseTranscriptReceived(message, timestamp);

                _memoryStream.SetLength(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StopAsync] Error saving audio: {ex.Message}");
            }
        } 

        private static async Task<string> SaveToLocalFileAsync(MemoryStream stream, string fileName)
        {
            var directoryPath = Path.Combine(FileSystem.AppDataDirectory, PcmDirectoryName);
            Directory.CreateDirectory(directoryPath);

            var filePath = Path.Combine(directoryPath, fileName);

            using var rawStream = new RawSourceWaveStream(stream, _waveFormat);
            //write to file.
            using var fileStream = File.Create(filePath);
            await rawStream.CopyToAsync(fileStream);

            return $"File saved locally: {filePath}";
        }

        private async Task<string> UploadToBlobStorageAsync(Stream audioStream, string fileName)
        {
            audioStream.Position = 0;
            using var rawStream = new RawSourceWaveStream(audioStream, _waveFormat);
            var uri = await _storageService.UploadAudioAsync(rawStream, fileName);
            return $"File uploaded to Azure Blob Storage: {uri}";
        }

        private void RaiseTranscriptReceived(string message, DateTime timestamp)
        {
            TranscriptReceived?.Invoke(this, new TranscriptEntry
            {
                Text = message,
                Timestamp = timestamp,
                SpeakerLabel = ServiceName,
                Initials = ServiceInitials
            });
        }
    }
}
