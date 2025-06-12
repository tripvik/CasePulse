using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using NAudio.Wave;
using Microsoft.CognitiveServices.Speech.Transcription;
using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Services
{
    public interface ITranscriptionService : IAsyncDisposable
    {
        // *** NEW: Events for INTERIM recognition results ***
        event EventHandler<TranscriptEntry>? RecognizingTranscriptReceived;

        // *** MODIFIED: Events for FINAL recognized segments ***
        event EventHandler<TranscriptEntry>? TranscriptReceived; // Changed from string

        Task InitializeAsync(WaveFormat micFormat);
        Task ProcessChunkAsync(byte[] audioData);
        Task StopAsync();
    }
}
