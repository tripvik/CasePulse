using MudBlazor;
using SmartPendant.MAUIHybrid.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPendant.MAUIHybrid.Abstractions
{
    public interface IOrchestrationService
    {
        bool IsRecording { get; set; }
        bool IsDeviceConnected { get; set; }
        bool StateChanging { get; }
        event EventHandler StateHasChanged;
        event EventHandler<(string message, Severity severity)>? Notify;
        Conversation CurrentConversation { get;  }
        Task StartAsync();
        Task StopAsync();
    }
}
