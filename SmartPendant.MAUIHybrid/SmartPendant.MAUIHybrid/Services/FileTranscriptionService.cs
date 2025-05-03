using NAudio.Wave;
using Microsoft.Maui.Storage;

namespace SmartPendant.MAUIHybrid.Services
{
    internal class FileTranscriptionService : ITranscriptionService
    {
        public event EventHandler<string>? RecognizingTranscriptReceived;
        public event EventHandler<string>? TranscriptReceived;

        private readonly MemoryStream _memoryStream = new();
        private RawSourceWaveStream? _rawWaveStream;
        private WaveFormat? _waveFormat;

        public async ValueTask DisposeAsync()
        {
            // Ensure proper disposal of resources
            _rawWaveStream?.Dispose();
            await _memoryStream.DisposeAsync();
        }

        public Task InitializeAsync(WaveFormat micFormat)
        {
            _waveFormat = micFormat ?? throw new ArgumentNullException(nameof(micFormat));
            return Task.CompletedTask;
        }

        public async Task ProcessChunkAsync(byte[] audioData)
        {
            if (audioData == null) throw new ArgumentNullException(nameof(audioData));

            try
            {
                await _memoryStream.WriteAsync(audioData.AsMemory());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing mic chunk: {ex.Message}");
            }
        }

        public async Task StopAsync()
        {
            //its going null. 
            if (_waveFormat == null)
            {
                throw new InvalidOperationException("Service is not initialized.");
            }

            var directoryPath = Path.Combine(FileSystem.AppDataDirectory, "wavfiles");
            Directory.CreateDirectory(directoryPath);

            var filePath = Path.Combine(directoryPath, $"{Guid.NewGuid()}.wav");

            try
            {
                _rawWaveStream = new RawSourceWaveStream(new MemoryStream(_memoryStream.ToArray()), _waveFormat);
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                using var waveFileWriter = new WaveFileWriter(fileStream, _waveFormat);

                // Instead of CopyToAsync, manually write the raw PCM bytes
                var buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = await _rawWaveStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    waveFileWriter.Write(buffer, 0, bytesRead);
                }

                await waveFileWriter.FlushAsync();
                TranscriptReceived?.Invoke(this, $"file saved {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving WAV file: {ex.Message}");
            }
        }

    }
}