using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using NAudio.Wave;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Services
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly SpeechConfig _speechConfig;
        private SpeechRecognizer? _recognizer;
        private PushAudioInputStream? _pushStream;

        // *** NEW: Events for INTERIM recognition results ***
        public event EventHandler<string>? RecognizingTranscriptReceived;

        // *** MODIFIED: Events for FINAL recognized segments ***
        public event EventHandler<string>? TranscriptReceived; // Changed from string

        public TranscriptionService(Uri endpoint, string subscriptionKey)
        {
            _speechConfig = SpeechConfig.FromEndpoint(endpoint, subscriptionKey);
            _speechConfig.SpeechRecognitionLanguage = "en-US";
            _speechConfig.SetProperty(PropertyId.Speech_SegmentationStrategy, "Semantic");
            _speechConfig.SetProperty(PropertyId.SpeechServiceResponse_PostProcessingOption, "TrueText");
        }

        public async Task InitializeAsync(WaveFormat micFormat)
        {
            var micAudioFormat = AudioStreamFormat.GetWaveFormatPCM(
                (uint)micFormat.SampleRate,
                (byte)micFormat.BitsPerSample,
                (byte)micFormat.Channels);

            _pushStream = AudioInputStream.CreatePushStream(micAudioFormat);
            _recognizer = new SpeechRecognizer(_speechConfig, AudioConfig.FromStreamInput(_pushStream));

            // Handle INTERIM Mic Results
            _recognizer.Recognizing += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Result.Text))
                {
                    RecognizingTranscriptReceived?.Invoke(this, e.Result.Text); // Raise new interim event
                }
            };

            // Handle FINAL Mic Results
            _recognizer.Recognized += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Result.Text))
                {
                    TranscriptReceived?.Invoke(this, e.Result.Text);
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Debug.WriteLine("MIC NOMATCH: Speech could not be recognized.");
                }
            };
            _recognizer.Canceled += (s, e) => Debug.WriteLine($"CANCELED: Reason={e.Reason}, Details={e.ErrorDetails}");
            _recognizer.SessionStopped += (s, e) => Debug.WriteLine("Session stopped.");

            await _recognizer.StartContinuousRecognitionAsync();
            Debug.WriteLine("Recognizer session started.");
        }

        public Task ProcessChunkAsync(byte[] audioData)
        {
            if (_pushStream != null)
            {
                try { _pushStream.Write(audioData); }
                catch (Exception ex) { Console.WriteLine($"Error writing Mic chunk: {ex.Message}"); }
            }
            return Task.CompletedTask;
        }


        public async Task StopAsync()
        {
            Debug.WriteLine("Azure Recognizers stopping...");
            if (_recognizer != null) await _recognizer.StopContinuousRecognitionAsync();
            _pushStream?.Close();
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _recognizer?.Dispose();
            _pushStream?.Dispose();
            // Unsubscribe for safety (though instance disposal usually handles this)
            RecognizingTranscriptReceived = null;
            TranscriptReceived = null;
            Debug.WriteLine("Azure Transcription Service disposed.");
        }
    }
}
