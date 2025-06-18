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
        Conversation CurrentConversation { get; }

        event EventHandler? StateHasChanged;
        event EventHandler<Conversation>? ConversationCompleted;
        event EventHandler<(string message, Severity severity)>? Notify;

        Task StartAsync();
        Task StopAsync();
    }
}