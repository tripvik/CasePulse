using Microsoft.AspNetCore.Components;
using MudBlazor;
using NAudio.Wave;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Components.Pages
{
    public partial class Home
    {
        [Inject]
        private ISnackbar Snackbar { get; set; } = default!;
        [Inject]
        private IOrchestrationService OrchestrationService { get; set; } = default!;

        #region State
        public bool isRecording
        {
            get => OrchestrationService.IsRecording;
            set => OrchestrationService.IsRecording = value;
        }
        public bool stateChanging
        {
            get => OrchestrationService.StateChanging;
        }
        public bool isDeviceConnected
        {
            get => OrchestrationService.IsDeviceConnected;
            set => OrchestrationService.IsRecording = value;
        }
        private List<TranscriptEntry> transcriptEntries
        {
            get => OrchestrationService?.CurrentConversation?.Transcript;
            set
            {
                if (OrchestrationService?.CurrentConversation != null)
                    OrchestrationService.CurrentConversation.Transcript = value;
            }
        }
        private TranscriptEntry? recognizingEntry
        {
            get => OrchestrationService?.CurrentConversation?.RecognizingEntry;
            set
            {
                if (OrchestrationService?.CurrentConversation != null)
                    OrchestrationService.CurrentConversation.RecognizingEntry = value;
            }
        }
        #endregion


        #region Lifecycle
        protected override void OnInitialized()
        {
            // By this point, Snackbar has been injected and is available
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomCenter;
            OrchestrationService.StateHasChanged += StateHasChangedWrapper;
            OrchestrationService.Notify += NotifyWrapper;
            base.OnInitialized();
        }

        private void StateHasChangedWrapper(object? sender, EventArgs e)
        {
            InvokeAsync(StateHasChanged);
        }

        private void NotifyWrapper(object? sender, (string message, Severity severity) e)
        {
            Snackbar.Add(e.message, e.severity);    
        }

        #endregion

        #region UI Actions
        private async Task ToggleRecording()
        {
            isRecording = !isRecording;
            if (isRecording)
            {
                await OrchestrationService.StartAsync();
            }
            else
            {
                await OrchestrationService.StopAsync();
            }
            StateHasChanged();
        }
        #endregion

        #region Real Service Logic
      
        #endregion

        #region Disposal
        public async void Dispose()
        {
            OrchestrationService.StateHasChanged -= StateHasChangedWrapper;
            OrchestrationService.Notify -= NotifyWrapper;
        }
        #endregion

    }
}