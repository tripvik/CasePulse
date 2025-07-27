using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;
using System.IO;

namespace SmartPendant.MAUIHybrid.Services
{
    internal class AudioStorageService : IAudioStorageService
    {
        private readonly IStorageService _storageService;
        private readonly MemoryStream _memoryStream = new();
        private WaveFileWriter? _waveFileWriter;
        private WaveFormat _waveFormat = new(16000, 16, 1);

        // Cache the platform check result instead of computing it repeatedly
        private static readonly bool IsDesktop =
                DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.macOS;
        private readonly bool useBlobStorage;

        public AudioStorageService(IStorageService storageService, IConfiguration configuration)
        {
            _storageService = storageService;
            useBlobStorage = configuration.GetValue<bool>("UseBlobStorage");
        }

        public async ValueTask DisposeAsync()
        {
            if (_waveFileWriter is not null)
            {
                await _waveFileWriter.DisposeAsync();
                _waveFileWriter = null;
            }
            await _memoryStream.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        public Task InitializeAsync(WaveFormat? micFormat)
        {
            if (micFormat is not null)
            {
                _waveFormat = micFormat;
            }

            // Dispose previous writer if exists
            //_waveFileWriter?.Dispose();

            // Reset memory stream position to beginning
            _memoryStream.SetLength(0);
            _memoryStream.Position = 0;

            //Todo : Find the best way to dispose the WaveFileWriter. It appears like Disposing WaveFileWriter will also dispose the MemoryStream.

            _waveFileWriter = new WaveFileWriter(_memoryStream, _waveFormat);

            return Task.CompletedTask;
        }

        public async Task ProcessChunkAsync(byte[] audioData)
        {
            ArgumentNullException.ThrowIfNull(audioData);
            ArgumentNullException.ThrowIfNull(_waveFileWriter);
            try
            {
                await _waveFileWriter.WriteAsync(audioData);
                await _waveFileWriter.FlushAsync(); // Ensure data is written
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcessChunkAsync] Error writing mic chunk: {ex.Message}");
                throw; // Re-throw to handle upstream
            }
        }

        public async Task<(string path, Exception? ex)> StopAsync(string? fileName = null)
        {
            try
            {
                if (_waveFileWriter == null)
                {
                    Debug.WriteLine("[StopAsync] WaveFileWriter is null.");
                    return (string.Empty, new InvalidOperationException("WaveFileWriter is not initialized."));
                }

                // Flush and dispose the writer to finalize the WAV file
                await _waveFileWriter.FlushAsync();
                //_waveFileWriter.Dispose();
                //_waveFileWriter = null;

                if (_memoryStream.Length == 0 || string.IsNullOrEmpty(fileName))
                {
                    Debug.WriteLine($"[StopAsync] No audio data to save. Stream length: {_memoryStream.Length}, FileName: {fileName}");
                    return (string.Empty, new InvalidOperationException("No audio data to save."));
                }

                var date = DateTime.UtcNow;
                var directoryPath = Path.Combine(FileSystem.AppDataDirectory, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd"));

                var path = useBlobStorage
                    ? await UploadToBlobStorageAsync(directoryPath, fileName)
                    : await SaveToLocalFileAsync(directoryPath, fileName);

                Debug.WriteLine($"[StopAsync] Audio saved successfully to: {path}");

                // Reset memory stream for next recording
                _memoryStream.SetLength(0);
                _memoryStream.Position = 0;

                return (path, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StopAsync] Error saving audio: {ex.Message}");
                return (string.Empty, ex);
            }
        }

        private async Task<string> SaveToLocalFileAsync(string directoryPath, string fileName)
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
                var filePath = Path.Combine(directoryPath, fileName + ".wav");

                Debug.WriteLine($"[SaveToLocalFileAsync] Saving to: {filePath}, Stream length: {_memoryStream.Length}");

                // Reset stream position to beginning for reading
                _memoryStream.Position = 0;

                using var fileStream = File.Create(filePath);
                await _memoryStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();

                Debug.WriteLine($"[SaveToLocalFileAsync] File saved successfully. Size: {new FileInfo(filePath).Length} bytes");

                return filePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SaveToLocalFileAsync] Error: {ex.Message}");
                throw;
            }
        }

        private async Task<string> UploadToBlobStorageAsync(string directoryPath, string fileName)
        {
            try
            {
                var filePath = Path.Combine(directoryPath, fileName + ".wav");

                Debug.WriteLine($"[UploadToBlobStorageAsync] Uploading: {filePath}, Stream length: {_memoryStream.Length}");

                // Reset stream position to beginning for reading
                _memoryStream.Position = 0;

                var uri = await _storageService.UploadAudioAsync(_memoryStream, filePath);

                Debug.WriteLine($"[UploadToBlobStorageAsync] Upload successful. URI: {uri}");

                return uri;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UploadToBlobStorageAsync] Error: {ex.Message}");
                throw;
            }
        }
    }
}