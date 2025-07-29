using Plugin.Maui.Audio;

namespace SmartPendant.MAUIHybrid.Services
{
    public interface IAudioService
    {
        Task<IAudioPlayer?> LoadAudioFromFileAsync(string filePath);
        Task<IAudioPlayer?> LoadAudioFromStreamAsync(Stream audioStream);
        Task<IAudioPlayer?> LoadAudioFromUrlAsync(string url);
        bool IsAudioFile(string filePath);
        Task<TimeSpan> GetAudioDurationAsync(string filePath);
    }

    public class AudioService : IAudioService
    {
        private readonly IAudioManager _audioManager;

        public AudioService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public Task<IAudioPlayer?> LoadAudioFromFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return Task.FromResult<IAudioPlayer?>(null);

                if (!IsAudioFile(filePath))
                    return Task.FromResult<IAudioPlayer?>(null);

                var fileStream = File.OpenRead(filePath);
                var player = _audioManager.CreatePlayer(fileStream);
                return Task.FromResult<IAudioPlayer?>(player);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading audio from file: {ex.Message}");
                return Task.FromResult<IAudioPlayer?>(null);
            }
        }

        public Task<IAudioPlayer?> LoadAudioFromStreamAsync(Stream audioStream)
        {
            try
            {
                if (audioStream == null || !audioStream.CanRead)
                    return Task.FromResult<IAudioPlayer?>(null);

                var player = _audioManager.CreatePlayer(audioStream);
                return Task.FromResult<IAudioPlayer?>(player);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading audio from stream: {ex.Message}");
                return Task.FromResult<IAudioPlayer?>(null);
            }
        }

        public async Task<IAudioPlayer?> LoadAudioFromUrlAsync(string url)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var stream = await response.Content.ReadAsStreamAsync();
                return _audioManager.CreatePlayer(stream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading audio from URL: {ex.Message}");
                return null;
            }
        }

        public bool IsAudioFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var supportedFormats = new[] { ".wav", ".mp3", ".m4a", ".aac", ".ogg", ".flac" };
            
            return supportedFormats.Contains(extension);
        }

        public async Task<TimeSpan> GetAudioDurationAsync(string filePath)
        {
            try
            {
                var player = await LoadAudioFromFileAsync(filePath);
                if (player != null)
                {
                    var durationSeconds = player.Duration;
                    var duration = TimeSpan.FromSeconds(durationSeconds);
                    player.Dispose();
                    return duration;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting audio duration: {ex.Message}");
            }

            return TimeSpan.Zero;
        }
    }
}