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
        public bool StateChanging { get; set; } = false;

        public event EventHandler? StateHasChanged;
        public event EventHandler<(string message, Severity severity)>? Notify;

        public Conversation CurrentConversation { get; set; } = new();

        public AndroidOrchestratorService(IConnectionService bluetoothService, ITranscriptionService transcriptionService)
        {
            AndroidServiceBridge.BluetoothService = bluetoothService;
            AndroidServiceBridge.TranscriptionService = transcriptionService;
            AndroidServiceBridge.OrchestrationService = this;

        }

        public Task StartAsync()
        {
            try
            {
                var intent = new Intent(Platform.CurrentActivity, typeof(BackGroundService));
                Platform.CurrentActivity.StartForegroundService(intent);
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
                var intent = new Intent(Platform.CurrentActivity, typeof(BackGroundService));
                Platform.CurrentActivity.StopService(intent);
            }
            catch (Exception ex)
            {
                Notify?.Invoke(this, ($"Stop failed: {ex.Message}", Severity.Error));
            }

            return Task.CompletedTask;
        }

        public void RaiseStateHasChanged()
        {
            StateHasChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseNotify(string message, Severity severity)
        {
            Notify?.Invoke(this,(message, severity));
        }

    }
}