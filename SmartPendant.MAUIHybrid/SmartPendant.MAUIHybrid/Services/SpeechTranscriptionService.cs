using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Services
{
    public class SpeechTranscriptionService : ITranscriptionService
    {
        #region Fields
        private readonly SpeechConfig _speechConfig;
        private ConversationTranscriber? _transcriber;
        private PushAudioInputStream? _pushStream;
        #endregion

        #region Events
        public event EventHandler<TranscriptEntry>? RecognizingTranscriptReceived;
        public event EventHandler<TranscriptEntry>? TranscriptReceived;
        #endregion

        #region Constructor
        public SpeechTranscriptionService(IConfiguration configuration)
        {
            var endpoint = configuration["Azure:Speech:Endpoint"] ?? throw new InvalidOperationException("Azure Speech Endpoint is not configured. Please check your appsettings.json or environment variables.");
            var subscriptionKey = configuration["Azure:Speech:Key"] ?? throw new InvalidOperationException("Azure Speech Subscription Key is not configured. Please check your appsettings.json or environment variables.");
            _speechConfig = SpeechConfig.FromEndpoint(new Uri(endpoint), subscriptionKey);
            _speechConfig.SpeechRecognitionLanguage = "en-IN";
            _speechConfig.SetProperty(PropertyId.Speech_SegmentationStrategy, "Semantic");
            _speechConfig.SetProperty(PropertyId.SpeechServiceResponse_PostProcessingOption, "TrueText");
            //Intermediate results are not needed for this service, so we disable them to reduce noise and processing overhead.
            _speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "false");
        }
        #endregion

        #region Public Methods
        public async Task InitializeAsync(WaveFormat micFormat)
        {
            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(
                (uint)micFormat.SampleRate,
                (byte)micFormat.BitsPerSample,
                (byte)micFormat.Channels);

            _pushStream = AudioInputStream.CreatePushStream(audioFormat);
            var audioConfig = AudioConfig.FromStreamInput(_pushStream);
            _transcriber = new ConversationTranscriber(_speechConfig, audioConfig);

            SubscribeToTranscriberEvents();

            await _transcriber.StartTranscribingAsync();
            Debug.WriteLine("Conversation transcriber session started.");
        }

        public Task ProcessChunkAsync(byte[] audioData)
        {
            if (_pushStream != null && audioData.Length > 0)
            {
                _pushStream.Write(audioData);
            }
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            Debug.WriteLine("Stopping conversation transcriber...");
            if (_transcriber != null)
            {
                await _transcriber.StopTranscribingAsync();
                UnsubscribeFromTranscriberEvents();
            }
            _pushStream?.Close();
        }
        #endregion

        #region Private Methods & Event Handlers
        private void SubscribeToTranscriberEvents()
        {
            if (_transcriber is null) return;
            _transcriber.Transcribing += OnTranscribing;
            _transcriber.Transcribed += OnTranscribed;
            _transcriber.Canceled += OnCanceled;
            _transcriber.SessionStopped += OnSessionStopped;
        }

        private void UnsubscribeFromTranscriberEvents()
        {
            if (_transcriber is null) return;
            _transcriber.Transcribing -= OnTranscribing;
            _transcriber.Transcribed -= OnTranscribed;
            _transcriber.Canceled -= OnCanceled;
            _transcriber.SessionStopped -= OnSessionStopped;
        }

        private void OnTranscribing(object? sender, ConversationTranscriptionEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Result.Text))
            {
                var entry = new TranscriptEntry
                {
                    Timestamp = DateTime.Now,
                    SpeakerLabel = e.Result.SpeakerId,
                    Text = e.Result.Text,
                };
                RecognizingTranscriptReceived?.Invoke(this, entry);
            }
        }

        private void OnTranscribed(object? sender, ConversationTranscriptionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(e.Result.Text))
            {
                var entry = new TranscriptEntry
                {
                    Timestamp = DateTime.Now,
                    SpeakerLabel = e.Result.SpeakerId,
                    Text = e.Result.Text,
                };
                TranscriptReceived?.Invoke(this, entry);
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Debug.WriteLine("NOMATCH: Speech could not be recognized.");
            }
        }

        private void OnSessionStopped(object? sender, SessionEventArgs e)
        {
            Debug.WriteLine($"Session stopped. SessionId: {e.SessionId}");
        }

        private void OnCanceled(object? sender, ConversationTranscriptionCanceledEventArgs e)
        {
            Debug.WriteLine($"CANCELED: Reason={e.Reason}");
            if (e.Reason == CancellationReason.Error)
            {
                Debug.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}, ErrorDetails={e.ErrorDetails}");
            }
        }
        #endregion

        #region IAsyncDisposable
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _transcriber?.Dispose();
            _pushStream?.Dispose();
            _transcriber = null;
            _pushStream = null;
            GC.SuppressFinalize(this);
            Debug.WriteLine("SpeechTranscriptionService disposed.");
        }
        #endregion
    }
}