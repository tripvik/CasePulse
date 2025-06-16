using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Util;
using MudBlazor;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System;
using System.Threading.Channels;
using AM = Android.Manifest;
using SD = System.Diagnostics;


namespace SmartPendant.MAUIHybrid.Platforms.Android
{
    [Service(
        ForegroundServiceType = ForegroundService.TypeDataSync
    )]
    public class BackGroundService : Service
    {
        private IConnectionService _bluetoothService = AndroidServiceBridge.BluetoothService!;
        private ITranscriptionService _transcriptionService = AndroidServiceBridge.TranscriptionService!;
        private AndroidOrchestratorService _orchestrationService = AndroidServiceBridge.OrchestrationService!;
        private Channel<byte[]> _audioDataChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(500)
        {
            SingleReader = true,
            SingleWriter = false
        });
        private CancellationTokenSource? _processingCts = new CancellationTokenSource();

        private bool IsRecording
        {
            get => _orchestrationService.IsRecording;
            set => _orchestrationService.IsRecording = value;
        }
        private bool IsDeviceConnected
        {
            get => _orchestrationService.IsDeviceConnected;
            set => _orchestrationService.IsDeviceConnected = value;
        }
        private bool StateChanging
        {
            get => _orchestrationService.StateChanging;
            set => _orchestrationService.StateChanging = value;
        }
        private Conversation CurrentConversation
        {
            get => _orchestrationService.CurrentConversation;
            set => _orchestrationService.CurrentConversation = value;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel("foreground_channel", "Smart Pendant", NotificationImportance.Default);
                var manager = (NotificationManager)GetSystemService(NotificationService)!;
                manager.CreateNotificationChannel(channel);
            }
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            StartForegroundWithNotification();
            _processingCts = new CancellationTokenSource();
            RunAudioPipelineAsync();
            return StartCommandResult.Sticky;
        }

        public override async void OnDestroy()
        {
            StateChanging = true;
            _orchestrationService.RaiseStateHasChanged();

            _processingCts?.Cancel();
            _processingCts = null;

            await _transcriptionService.StopAsync();
            _transcriptionService.TranscriptReceived -= OnTranscriptReceived;
            _transcriptionService.RecognizingTranscriptReceived -= OnRecognizingTranscriptReceived;

            _bluetoothService.ConnectionLost -= OnDisconnected;
            _bluetoothService.Disconnected -= OnDisconnected;
            _bluetoothService.DataReceived -= OnDataReceived;

            await _bluetoothService.DisconnectAsync();

            IsDeviceConnected = false;
            IsRecording = false;
            StateChanging = false;
            _orchestrationService.RaiseStateHasChanged();

            base.OnDestroy();
        }

        public override IBinder? OnBind(Intent? intent) => null;

        private async void RunAudioPipelineAsync()
        {
            StateChanging = true;
            _orchestrationService.RaiseStateHasChanged();
            var (connected, ex) = await _bluetoothService.ConnectAsync();
            if (!connected)
            {
                var message = $"Failed to connect: {ex?.Message}";
                SD.Debug.WriteLine(message);
                _orchestrationService.RaiseNotify(message, Severity.Error);
                StateChanging = false;
                IsDeviceConnected = false;
                IsRecording = false;
                _orchestrationService.RaiseStateHasChanged();
                StopSelf(); // Stop service if connection fails
                return;
            }

            var initialized = await _bluetoothService.InitializeAsync();
            if (!initialized)
            {
                SD.Debug.WriteLine("Failed to initialize Bluetooth characteristic or service.");
                _orchestrationService.RaiseNotify("Failed to initialize Bluetooth service.", Severity.Error);
                StateChanging = false;
                IsDeviceConnected = false;
                IsRecording = false;
                _orchestrationService.RaiseStateHasChanged();
                StopSelf(); // Stop service if initialization fails
                return;
            }

            _processingCts = new CancellationTokenSource();
            _ = ProcessAudioDataAsync(_processingCts.Token);

            _bluetoothService.DataReceived += OnDataReceived;
            _transcriptionService.TranscriptReceived += OnTranscriptReceived;
            _bluetoothService.ConnectionLost += OnDisconnected;
            _bluetoothService.Disconnected += OnDisconnected;
            _transcriptionService.RecognizingTranscriptReceived += OnRecognizingTranscriptReceived;
            await _transcriptionService.InitializeAsync(new WaveFormat(16000, 16, 1));
            IsDeviceConnected = true;
            IsRecording = true;
            StateChanging = false;
            _orchestrationService.RaiseStateHasChanged();
        }

        private void OnDisconnected(object? sender, string disconnectMessage)
        {
            var logMessage = $"Connection Error: {disconnectMessage}";
            SD.Debug.WriteLine(logMessage);
            _orchestrationService.RaiseNotify(logMessage, Severity.Error);

            // Stop the service when connection is lost
            StopSelf();
        }

        private async void StartForegroundWithNotification()
        {
            var intent = new Intent(this, typeof(MainActivity));
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                var permissionResult = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (permissionResult != PermissionStatus.Granted)
                {
                    permissionResult = await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
            }
            // To Do : Should we make the notification non-dismissible?
            var notification = new Notification.Builder(this, "foreground_channel")
                .SetContentTitle("Smart Pendant")
                .SetContentText("Recording in progress…")
                .SetSmallIcon(Resource.Drawable.abc_btn_radio_material)
                .SetContentIntent(pendingIntent)
                .SetCategory(Notification.CategoryService) // Recommended
                .SetOngoing(true)
                .Build();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                AndroidX.Core.App.ServiceCompat.StartForeground(
                    this,
                    7777,
                    notification,
                    (int)ForegroundService.TypeDataSync
                );
            }
            else
            {
                StartForeground(7777, notification);
            }
        }

        private void OnRecognizingTranscriptReceived(object? sender, TranscriptEntry message)
        {
            /*
             * Not needed for now
            */
        }

        private async Task ProcessAudioDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var audioData in _audioDataChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        await BufferAndSendAsync(audioData);
                    }
                    catch (Exception ex)
                    {
                        SD.Debug.WriteLine($"Error processing audio data: {ex.Message}");
                    }
                }
            }
            catch (System.OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                SD.Debug.WriteLine($"Error in audio processing loop: {ex.Message}");
            }
        }

        private async void OnDataReceived(object? sender, byte[] data)
        {
            try
            {
                // Copy the data to avoid potential issues with reused buffers
                await _audioDataChannel.Writer.WriteAsync(data);
            }
            catch (Exception ex)
            {
                SD.Debug.WriteLine($"Error processing data: {ex.Message}");
            }
        }

        private void OnTranscriptReceived(object? sender, TranscriptEntry message)
        {
            CurrentConversation.Transcript.Add(message);
            _orchestrationService.RaiseStateHasChanged();
        }

        private async Task BufferAndSendAsync(byte[] newData)
        {
            await _transcriptionService.ProcessChunkAsync(newData);
        }
    }
}