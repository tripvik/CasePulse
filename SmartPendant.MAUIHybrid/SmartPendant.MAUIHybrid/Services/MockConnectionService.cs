using SmartPendant.MAUIHybrid.Abstractions;
using System;
using System.Threading.Tasks;

namespace SmartPendant.MAUIHybrid.Services
{
    /// <summary>
    /// Mock implementation of IConnectionService for simulating device connection and data events.
    /// </summary>
    public class MockConnectionService : IConnectionService
    {
        public bool IsConnected { get; private set; } = false;

        public event EventHandler<byte[]>? DataReceived;
        public event EventHandler<string>? ConnectionLost;
        public event EventHandler<string>? Disconnected;

        public Task<(bool success, Exception? error)> ConnectAsync()
        {
            IsConnected = true;
            return Task.FromResult((true, (Exception?)null));
        }

        public Task<bool> InitializeAsync()
        {
            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            //Disconnected?.Invoke(this, "Mock disconnect");
            return Task.CompletedTask;
        }

        // Optionally, you can add a method to simulate data reception
        public void SimulateDataReceived(byte[] data)
        {
            DataReceived?.Invoke(this, data);
        }
    }
}
