using MudBlazor;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using SmartPendant.MAUIHybrid.Services;

namespace SmartPendant.MAUIHybrid.Platforms.Windows
{
    public class WindowsOrchestrationService : IOrchestrationService
    {
        #region Fields
        private readonly AudioPipelineManager _pipelineManager;
        #endregion

        #region Properties
        public bool IsRecording { get; private set; }
        public bool IsDeviceConnected { get; private set; }
        public bool IsStateChanging { get; private set; }
        public Conversation CurrentConversation => _pipelineManager.CurrentConversation;
        #endregion

        #region Events
        public event EventHandler? StateHasChanged;
        public event EventHandler<(string message, Severity severity)>? Notify;
        #endregion

        #region Constructor
        public WindowsOrchestrationService(AudioPipelineManager pipelineManager)
        {
            _pipelineManager = pipelineManager;
            _pipelineManager.StateHasChanged += (s, e) => StateHasChanged?.Invoke(s, e);
            _pipelineManager.Notify += (s, e) => Notify?.Invoke(s, e);
        }
        #endregion

        #region Public Methods
        public async Task StartAsync()
        {
            if (IsRecording) return;
            SetState(isStateChanging: true);

            var (success, errorMessage) = await _pipelineManager.StartPipelineAsync();
            if (success)
            {
                SetState(isRecording: true, isDeviceConnected: true, isStateChanging: false);
            }
            else
            {
                Notify?.Invoke(this, (errorMessage ?? "An unknown error occurred.", Severity.Error));
                SetState(isRecording: false, isDeviceConnected: false, isStateChanging: false);
            }
        }

        public async Task StopAsync()
        {
            if (!IsRecording && !IsStateChanging) return;
            SetState(isStateChanging: true);
            await _pipelineManager.StopPipelineAsync();
            SetState(isRecording: false, isDeviceConnected: false, isStateChanging: false);
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
            await _pipelineManager.DisposeAsync();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}