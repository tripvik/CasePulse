using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPendant.MAUIHybrid.Services
{
    public interface IConnectionService
    {
            bool IsConnected { get; }

            Task<(bool success, Exception? error)> ConnectAsync();

            Task DisconnectAsync();

            Task<bool> InitializeAsync();

            event EventHandler<byte[]> DataReceived;

            event EventHandler<string>? ConnectionLost;

            event EventHandler<string>? Disconnected;
    }

}
