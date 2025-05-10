using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPendant.MAUIHybrid.Services
{
    public class BluetoothService : IConnectionService
    {
        public bool IsConnected => throw new NotImplementedException();

        public event EventHandler<byte[]> DataReceived;
        public event EventHandler<string>? ConnectionLost;
        public event EventHandler<string>? Disconnected;

        public Task<(bool success, Exception? error)> ConnectAsync()
        {
            throw new NotImplementedException();
        }

        public Task DisconnectAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> InitializeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
