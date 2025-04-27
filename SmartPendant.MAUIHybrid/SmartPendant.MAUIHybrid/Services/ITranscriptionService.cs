using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace SmartPendant.MAUIHybrid.Services
{
    public interface ITranscriptionService : IAsyncDisposable
    {
        // *** NEW: Events for INTERIM recognition results ***
        event EventHandler<string>? RecognizingTranscriptReceived;

        // *** MODIFIED: Events for FINAL recognized segments ***
        event EventHandler<string>? TranscriptReceived; // Changed from string

        Task InitializeAsync(WaveFormat micFormat);
        Task ProcessChunkAsync(byte[] audioData);
        Task StopAsync();
    }
}
