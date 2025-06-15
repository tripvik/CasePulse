using Android.Content;
using Microsoft.Maui.ApplicationModel;
using MudBlazor;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Platforms.Android
{
    public class AndroidOrchestratorService : IOrchestrationService
    {
        public bool IsRecording { get; set; } = false;
        public bool IsDeviceConnected { get; set; } = false;
        public bool StateChanging { get; private set; } = false;

        public event EventHandler? StateHasChanged;
        public event EventHandler<(string message, Severity severity)>? Notify;

        public Conversation CurrentConversation { get; private set; } = new();

        public AndroidOrchestratorService(IConnectionService bluetoothService, ITranscriptionService transcriptionService)
        {
            AndroidServiceBridge.BluetoothService = bluetoothService;
            AndroidServiceBridge.TranscriptionService = transcriptionService;
            AndroidServiceBridge.OnDisconnected = HandleDisconnection;
            AndroidServiceBridge.OnFinalTranscript = entry =>
            {
                CurrentConversation.Transcript.Add(entry); // example data model
                StateHasChanged?.Invoke(this, EventArgs.Empty);
            };

            AndroidServiceBridge.OnRecognizingTranscript = entry =>
            {
                // maybe show interim UI
                //StateHasChanged?.Invoke(this, EventArgs.Empty);
            };

        }

        public Task StartAsync()
        {
            try
            {
                StateChanging = true;
                StateHasChanged?.Invoke(this, EventArgs.Empty);

                var intent = new Intent(Platform.CurrentActivity, typeof(BackGroundService));
                Platform.CurrentActivity.StartForegroundService(intent);

                IsDeviceConnected = true;
                IsRecording = true;
                StateChanging = false;
                StateHasChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Notify?.Invoke(this, ($"Start failed: {ex.Message}", Severity.Error));
            }

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            try
            {
                StateChanging = true;
                StateHasChanged?.Invoke(this, EventArgs.Empty);
                var intent = new Intent(Platform.CurrentActivity, typeof(BackGroundService));
                Platform.CurrentActivity.StopService(intent);

                IsDeviceConnected = false;
                IsRecording = false;
                StateChanging = false;
                StateHasChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Notify?.Invoke(this, ($"Stop failed: {ex.Message}", Severity.Error));
            }

            return Task.CompletedTask;
        }

        private void HandleDisconnection(string message)
        {
            Notify?.Invoke(this, ($"Connection Error: {message}", Severity.Error));
            IsDeviceConnected = false;
            IsRecording = false;
            StateHasChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}