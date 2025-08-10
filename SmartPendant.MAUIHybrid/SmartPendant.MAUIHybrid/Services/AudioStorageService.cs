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
        private string? _currentFilePath;
        private string? _fileName;
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
            
            // Clean up file if it exists and hasn't been moved
            if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
            {
                try
                {
                    File.Delete(_currentFilePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DisposeAsync] Error deleting file: {ex.Message}");
                }
            }
            
            GC.SuppressFinalize(this);
        }

        public Task InitializeAsync(WaveFormat? micFormat, string conversationId)
        {
            if (micFormat is not null)
            {
                _waveFormat = micFormat;
            }

            // Dispose previous writer if exists
            _waveFileWriter?.Dispose();
            _waveFileWriter = null;

            // Clean up previous file if it exists
            if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
            {
                try
                {
                    File.Delete(_currentFilePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[InitializeAsync] Error deleting previous file: {ex.Message}");
                }
            }

            // Generate filename from conversationId
            _fileName = conversationId;
            
            // Create the directory structure and file path
            var date = DateTime.UtcNow;
            var directoryPath = Path.Combine(FileSystem.AppDataDirectory, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd"));
            Directory.CreateDirectory(directoryPath);
            
            _currentFilePath = Path.Combine(directoryPath, _fileName + ".wav");

            // Create WaveFileWriter with the file path
            _waveFileWriter = new WaveFileWriter(_currentFilePath, _waveFormat);

            Debug.WriteLine($"[InitializeAsync] Initialized recording to: {_currentFilePath}");

            return Task.CompletedTask;
        }

        public async Task ProcessChunkAsync(byte[] audioData)
        {
            ArgumentNullException.ThrowIfNull(audioData);
            ArgumentNullException.ThrowIfNull(_waveFileWriter);
            try
            {
                // Write raw audio data bytes to the WAV file
                await _waveFileWriter.WriteAsync(audioData);
                //await _waveFileWriter.FlushAsync(); // Was causing issues, so removed
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcessChunkAsync] Error writing mic chunk: {ex.Message}");
                throw; // Re-throw to handle upstream
            }
        }

        public async Task<(string path, Exception? ex)> StopAsync()
        {
            try
            {
                if (_waveFileWriter == null)
                {
                    Debug.WriteLine("[StopAsync] WaveFileWriter is null.");
                    return (string.Empty, new InvalidOperationException("WaveFileWriter is not initialized."));
                }

                if (string.IsNullOrEmpty(_fileName) || string.IsNullOrEmpty(_currentFilePath))
                {
                    Debug.WriteLine("[StopAsync] FileName or current file path is null or empty.");
                    return (string.Empty, new InvalidOperationException("FileName is not set."));
                }

                // Flush and dispose the writer to finalize the WAV file
                await _waveFileWriter.FlushAsync();
                await _waveFileWriter.DisposeAsync();
                _waveFileWriter = null;

                // Check if file exists and has content
                if (!File.Exists(_currentFilePath))
                {
                    Debug.WriteLine($"[StopAsync] Audio file does not exist: {_currentFilePath}");
                    return (string.Empty, new InvalidOperationException("Audio file does not exist."));
                }

                var fileInfo = new FileInfo(_currentFilePath);
                if (fileInfo.Length == 0)
                {
                    Debug.WriteLine($"[StopAsync] No audio data to save. File size: {fileInfo.Length}");
                    return (string.Empty, new InvalidOperationException("No audio data to save."));
                }

                var path = useBlobStorage
                    ? await UploadToBlobStorageAsync(_currentFilePath, _fileName)
                    : _currentFilePath; // For local storage, the file is already in the right place

                Debug.WriteLine($"[StopAsync] Audio saved successfully to: {path}");

                // Clear the current file path since it's been processed
                _currentFilePath = null;
                _fileName = null;

                return (path, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StopAsync] Error saving audio: {ex.Message}");
                return (string.Empty, ex);
            }
        }

        private async Task<string> UploadToBlobStorageAsync(string currentFilePath, string fileName)
        {
            try
            {
                var date = DateTime.UtcNow;
                var blobPath = Path.Combine(date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd"), fileName + ".wav");

                Debug.WriteLine($"[UploadToBlobStorageAsync] Uploading from: {currentFilePath} to blob: {blobPath}");

                // Read the file and upload it to blob storage
                using var fileStream = File.OpenRead(currentFilePath);
                var uri = await _storageService.UploadAudioAsync(fileStream, blobPath);

                Debug.WriteLine($"[UploadToBlobStorageAsync] Upload successful. URI: {uri}");

                // Delete the local file after successful upload
                try
                {
                    File.Delete(currentFilePath);
                    Debug.WriteLine($"[UploadToBlobStorageAsync] Local file deleted: {currentFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[UploadToBlobStorageAsync] Error deleting local file: {ex.Message}");
                }

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