namespace SmartPendant.MAUIHybrid.Abstractions
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
