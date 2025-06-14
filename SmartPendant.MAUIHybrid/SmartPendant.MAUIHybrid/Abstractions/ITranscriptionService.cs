using NAudio.Wave;
using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Abstractions
{
    public interface ITranscriptionService : IAsyncDisposable
    {
        event EventHandler<TranscriptEntry>? RecognizingTranscriptReceived;
        event EventHandler<TranscriptEntry>? TranscriptReceived;

        Task InitializeAsync(WaveFormat micFormat);
        Task ProcessChunkAsync(byte[] audioData);
        Task StopAsync();
    }
}
