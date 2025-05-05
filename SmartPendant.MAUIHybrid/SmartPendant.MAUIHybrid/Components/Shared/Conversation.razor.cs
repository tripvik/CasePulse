using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SmartPendant.MAUIHybrid.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartPendant.MAUIHybrid.Components.Shared
{
    public partial class Conversation
    {
        [Parameter]
        [DefaultValue(null)]
        public List<ChatMessage>? Messages { get; set; }

        [Inject]
        IJSRuntime? JS { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("scrollToBottom", "chatContainer");
        }
    }
}
