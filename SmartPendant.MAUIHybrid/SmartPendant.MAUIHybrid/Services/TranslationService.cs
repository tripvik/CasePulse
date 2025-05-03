using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using NAudio.Wave;
using System.Diagnostics;
using Microsoft.CognitiveServices.Speech.Transcription;

namespace SmartPendant.MAUIHybrid.Services
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly SpeechConfig _speechConfig;
        private ConversationTranscriber? _transcriber;
        private PushAudioInputStream? _pushStream;

        // *** NEW: Events for INTERIM recognition results ***
        public event EventHandler<string>? RecognizingTranscriptReceived;

        // *** MODIFIED: Events for FINAL recognized segments ***
        public event EventHandler<string>? TranscriptReceived; // Changed from string

        public TranscriptionService(Uri endpoint, string subscriptionKey)
        {
            _speechConfig = SpeechConfig.FromEndpoint(endpoint, subscriptionKey);
            //_speechConfig.SpeechRecognitionLanguage = "en-IN";
            _speechConfig.SetProperty(PropertyId.Speech_SegmentationStrategy, "Semantic");
            _speechConfig.SetProperty(PropertyId.SpeechServiceResponse_PostProcessingOption, "TrueText");
            _speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");
        }

        public async Task InitializeAsync(WaveFormat micFormat)
        {
            var micAudioFormat = AudioStreamFormat.GetWaveFormatPCM(
                (uint)micFormat.SampleRate,
                (byte)micFormat.BitsPerSample,
                (byte)micFormat.Channels);

            _pushStream = AudioInputStream.CreatePushStream(micAudioFormat);
            _transcriber = new ConversationTranscriber(_speechConfig, AudioConfig.FromStreamInput(_pushStream));

            // Handle INTERIM Mic Results
            _transcriber.Transcribing += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Result.Text))
                {
                    RecognizingTranscriptReceived?.Invoke(this, $"{e.Result.SpeakerId} - {e.Result.Text}..."); // Raise new interim event
                }
            };

            // Handle FINAL Mic Results
            _transcriber.Transcribed += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Result.Text))
                {
                    TranscriptReceived?.Invoke(this, $"{e.Result.SpeakerId} - {e.Result.Text}");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Debug.WriteLine("MIC NOMATCH: Speech could not be recognized.");
                }
            };
            _transcriber.Canceled += (s, e) => Debug.WriteLine($"CANCELED: Reason={e.Reason}, Details={e.ErrorDetails}");
            _transcriber.SessionStopped += (s, e) => Debug.WriteLine("Session stopped.");

            await _transcriber.StartTranscribingAsync();
            Debug.WriteLine("Recognizer session started.");
        }

        public Task ProcessChunkAsync(byte[] audioData)
        {
            if (_pushStream != null)
            {
                try 
                {

                    var signedData = TranscriptionService.ConvertUnsignedToSigned(audioData);
                    _pushStream.Write(signedData); 
                }
                catch (Exception ex) { Console.WriteLine($"Error writing Mic chunk: {ex.Message}"); }
            }
            return Task.CompletedTask;
        }


        public async Task StopAsync()
        {
            Debug.WriteLine("Azure Recognizers stopping...");
            if (_transcriber != null) await _transcriber.StopTranscribingAsync();
            _pushStream?.Close();
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _transcriber?.Dispose();
            _pushStream?.Dispose();
            // Unsubscribe for safety (though instance disposal usually handles this)
            RecognizingTranscriptReceived = null;
            TranscriptReceived = null;
            Debug.WriteLine("Azure Transcription Service disposed.");
        }

        private static byte[] ConvertUnsignedToSigned(byte[] unsignedData)
        {
            byte[] signedData = new byte[unsignedData.Length];
            for (int i = 0; i < unsignedData.Length; i++)
            {
                signedData[i] = (byte)(unsignedData[i] - 128);
            }
            return signedData;
        }

    }
}
