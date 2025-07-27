using NAudio.Wave;

namespace SmartPendant.MAUIHybrid.Abstractions
{
    public interface IAudioStorageService : IAsyncDisposable
    {
        Task InitializeAsync(WaveFormat micFormat);
        Task ProcessChunkAsync(byte[] audioData);
        Task<(string path, Exception? ex)> StopAsync(string? fileName = null);
    }
}
