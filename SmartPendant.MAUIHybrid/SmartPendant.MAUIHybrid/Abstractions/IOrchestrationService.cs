using MudBlazor;
using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Abstractions
{
    /// <summary>
    /// Orchestrates the high-level operations of the application, coordinating
    /// the connection, transcription, and UI state.
    /// </summary>
    public interface IOrchestrationService : IAsyncDisposable
    {
        bool IsRecording { get; }
        bool IsDeviceConnected { get; }
        bool IsStateChanging { get; }
        ConversationModel CurrentConversation { get; }

        event EventHandler? StateHasChanged;
        event EventHandler? ConversationCompleted;
        event EventHandler<(string message, Severity severity)>? Notify;
        event EventHandler<(bool isRecording, bool isDeviceConnected, bool isStateChanging)>? SetStateEvent;

        Task StartAsync();
        Task StopAsync();
    }
}