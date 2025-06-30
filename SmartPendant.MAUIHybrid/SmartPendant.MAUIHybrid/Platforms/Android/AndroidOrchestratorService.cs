using Android.Content;
using MudBlazor;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using SmartPendant.MAUIHybrid.Services;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Platforms.Android
{
    public class AndroidOrchestrationService : IOrchestrationService
    {
        #region Fields
        private readonly AudioPipelineManager _pipelineManager;
        private readonly Intent _serviceIntent;
        #endregion

        #region Properties
        public bool IsRecording { get; private set; }
        public bool IsDeviceConnected { get; private set; }
        public bool IsStateChanging { get; private set; }
        public ConversationRecord CurrentConversation => _pipelineManager.CurrentConversation;
        public DayRecord CurrentDay => _pipelineManager.CurrentDay;
        #endregion

        #region Events
        public event EventHandler? StateHasChanged;
        public event EventHandler<(string message, Severity severity)>? Notify;
        public event EventHandler? ConversationCompleted;
        public event EventHandler<(bool isRecording, bool isDeviceConnected, bool isStateChanging)>? SetStateEvent;
        #endregion

        #region Constructor
        public AndroidOrchestrationService(AudioPipelineManager pipelineManager)
        {
            _pipelineManager = pipelineManager;
            _serviceIntent = new Intent(Platform.CurrentActivity ?? throw new InvalidOperationException("CurrentActivity is null"), typeof(AudioProcessingService));
            // Forward events from the pipeline manager to the UI
            _pipelineManager.StateHasChanged += (s, e) => StateHasChanged?.Invoke(s, e);
            _pipelineManager.ConversationCompleted += (s, e) => ConversationCompleted?.Invoke(s, e);
            _pipelineManager.Notify += (s, e) => Notify?.Invoke(s, e);
            _pipelineManager.SetStateEvent += (object? s, (bool isRecording, bool isDeviceConnected, bool isStateChanging) state) =>
            {
                SetState(state.isRecording, state.isDeviceConnected, state.isStateChanging);
            };
        }
        #endregion

        #region Public Methods
        public Task StartAsync()
        {
            if (IsRecording) return Task.CompletedTask;

            SetState(isStateChanging: true);
            try
            {
                // The actual logic is now inside the Android Service,
                // which will be started here. The service will then start the pipeline.
                Platform.CurrentActivity?.StartForegroundService(_serviceIntent);
                //Event set in _pipelineManager.StartPipelineAsync
                //SetState(isRecording: true, isDeviceConnected: true, isStateChanging: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start foreground service: {ex.Message}");
                Notify?.Invoke(this, ($"Start failed: {ex.Message}", Severity.Error));
                SetState(isStateChanging: false);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            if (!IsRecording && !IsStateChanging) return Task.CompletedTask;

            SetState(isStateChanging: true);
            try
            {
                // This will trigger the OnDestroy method in the service,
                // which in turn calls StopPipelineAsync.
                Platform.CurrentActivity?.StopService(_serviceIntent);
                //Event set in _pipelineManager.StopPipelineAsync
                //SetState(isRecording: false, isDeviceConnected: false, isStateChanging: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to stop foreground service: {ex.Message}");
                Notify?.Invoke(this, ($"Stop failed: {ex.Message}", Severity.Error));
                SetState(isStateChanging: false); // Reset state even on failure
            }
            return Task.CompletedTask;
        }
        #endregion

        #region Private Methods
        private void SetState(bool? isRecording = null, bool? isDeviceConnected = null, bool? isStateChanging = null)
        {
            IsRecording = isRecording ?? IsRecording;
            IsDeviceConnected = isDeviceConnected ?? IsDeviceConnected;
            IsStateChanging = isStateChanging ?? IsStateChanging;
            StateHasChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region IAsyncDisposable
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}