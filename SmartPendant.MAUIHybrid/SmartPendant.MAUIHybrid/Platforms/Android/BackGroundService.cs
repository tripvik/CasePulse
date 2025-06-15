using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Util;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using System;
using System.Threading.Channels;
using AM = Android.Manifest;

namespace SmartPendant.MAUIHybrid.Platforms.Android
{
    [Service(
  ForegroundServiceType = ForegroundService.TypeDataSync
)]
    public class BackGroundService : Service
    {
        private IConnectionService _bluetoothService;
        private ITranscriptionService _transcriptionService;
        private Channel<byte[]> _audioDataChannel;
        private CancellationTokenSource _cts;
        private Handler _handler;
        private Action _runnable;

        public override void OnCreate()
        {
            base.OnCreate();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel("foreground_channel", "Smart Pendant", NotificationImportance.Default);
                var manager = (NotificationManager)GetSystemService(NotificationService)!;
                manager.CreateNotificationChannel(channel);
            }
            _bluetoothService = AndroidServiceBridge.BluetoothService!;
            _transcriptionService = AndroidServiceBridge.TranscriptionService!;
            _audioDataChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(500)
            {
                SingleReader = true,
                SingleWriter = false
            });
            _handler = new Handler(Looper.MainLooper);
            _runnable = RunAudioPipelineAsync;
            _bluetoothService.ConnectionLost += OnDisconnected;
            _bluetoothService.Disconnected += OnDisconnected;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            StartForegroundWithNotification();
            _cts = new CancellationTokenSource();
            _handler.Post(_runnable);
            return StartCommandResult.Sticky;
        }

        public override async void OnDestroy()
        {
            _bluetoothService.ConnectionLost -= OnDisconnected;
            _bluetoothService.Disconnected -= OnDisconnected;
            await _bluetoothService.DisconnectAsync();
            await _transcriptionService.StopAsync();
            _cts?.Cancel();
            base.OnDestroy();
        }

        public override IBinder? OnBind(Intent? intent) => null;

        private async void RunAudioPipelineAsync()
        {
            var (connected, _) = await _bluetoothService.ConnectAsync();
            if (!connected) return;

            var initialized = await _bluetoothService.InitializeAsync();
            if (!initialized) return;

            _bluetoothService.DataReceived += async (_, data) =>
            {
                await _audioDataChannel.Writer.WriteAsync(data);
            };
            _transcriptionService.TranscriptReceived += (_, entry) =>
            {
                AndroidServiceBridge.OnFinalTranscript?.Invoke(entry);
            };
            _transcriptionService.RecognizingTranscriptReceived += (_, entry) =>
            {
                AndroidServiceBridge.OnRecognizingTranscript?.Invoke(entry);
            };
            await _transcriptionService.InitializeAsync(new WaveFormat(16000, 16, 1));

            try
            {
                await foreach (var data in _audioDataChannel.Reader.ReadAllAsync(_cts.Token))
                {
                    await _transcriptionService.ProcessChunkAsync(data);
                }
            }
            catch (System.OperationCanceledException ex)
            {
            }
            catch (System.Exception ex)
            {
                Log.Error("BackGroundService", $"Error processing audio chunk: {ex.Message}");
            }
        }

        private void OnDisconnected(object? sender, string message)
        {
            Log.Warn("BackGroundService", $"Disconnected: {message}");
            AndroidServiceBridge.OnDisconnected?.Invoke(message);
            _ = StopSelfResult(0);
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

            var notification = new Notification.Builder(this, "foreground_channel")
                .SetContentTitle("Smart Pendant")
                .SetContentText("Recording in progress…")
                .SetSmallIcon(Resource.Drawable.abc_btn_radio_material)
                .SetContentIntent(pendingIntent)
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
    }
}