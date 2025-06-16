using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Services;
using SD = System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Platforms.Android
{
    [Service(Name = "com.smartpendant.audioprocessingservice", ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class AudioProcessingService : Service
    {
        #region Constants & Fields
        private const int NOTIFICATION_ID = 8888;
        private const string NOTIFICATION_CHANNEL_ID = "audio_processing_channel";
        private const string NOTIFICATION_CHANNEL_NAME = "Audio Processing";

        private AudioPipelineManager? _pipelineManager;
        private IOrchestrationService? _orchestrationService;
        #endregion

        #region Service Lifecycle Methods
        public override IBinder? OnBind(Intent? intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();
            try
            {
                // Resolve dependencies from the MAUI DI container.
                _orchestrationService = MauiProgram.Services.GetService<IOrchestrationService>();
                _pipelineManager = MauiProgram.Services.GetService<AudioPipelineManager>();

                if (_pipelineManager is null || _orchestrationService is null)
                {
                    SD.Debug.WriteLine("CRITICAL: Failed to resolve services in AudioProcessingService. Stopping service.");
                    StopSelf();
                }
            }
            catch (Exception ex)
            {
                SD.Debug.WriteLine($"CRITICAL: Exception during service creation: {ex.Message}");
                StopSelf();
            }
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();
            var notification = BuildNotification();
            StartForeground(NOTIFICATION_ID, notification);

            _ = StartAudioPipelineAsync();

            return StartCommandResult.Sticky;
        }

        public override async void OnDestroy()
        {
            // NOTE: async void is acceptable in lifecycle overrides, but exceptions must be handled.
            try
            {
                if (_pipelineManager != null)
                {
                    // The pipeline manager handles all the cleanup.
                    await _pipelineManager.StopPipelineAsync();
                }
            }
            catch (Exception ex)
            {
                SD.Debug.WriteLine($"Error during service destruction: {ex.Message}");
            }
            finally
            {
                base.OnDestroy();
            }
        }
        #endregion

        #region Private Methods
        private async Task StartAudioPipelineAsync()
        {
            if (_pipelineManager is null) return;

            var (success, errorMessage) = await _pipelineManager.StartPipelineAsync();

            if (!success && _orchestrationService is not null)
            {
                // Let the orchestrator handle the full shutdown and UI update.
                // This prevents state inconsistencies.
                await _orchestrationService.StopAsync();
            }
        }
        #endregion

        #region Notification Management
        private Notification BuildNotification()
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable);

            return new Notification.Builder(this, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle("Smart Pendant Active")
                .SetContentText("Transcription service is running.")
                .SetSmallIcon(Resource.Mipmap.appicon) // Use a valid icon
                .SetContentIntent(pendingIntent)
                .SetOngoing(true)
                .Build();
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

            var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, NOTIFICATION_CHANNEL_NAME, NotificationImportance.Default)
            {
                Description = "Notification channel for the audio processing service."
            };

            var manager = (NotificationManager)GetSystemService(NotificationService)!;
            manager.CreateNotificationChannel(channel);
        }
        #endregion
    }
}