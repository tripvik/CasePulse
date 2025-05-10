using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using InTheHand.Bluetooth.Permissions;
using System.Threading;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Services
{
    public class BluetoothClassicService : IConnectionService
    {
        private BluetoothClient? _client;
        private Stream? _stream;
        private CancellationTokenSource? _cts;
        private const string TargetName = "M5_Serial"; // Your device name

        public bool IsConnected => _client?.Connected ?? false;

        public event EventHandler<byte[]>? DataReceived;
        public event EventHandler<string>? ConnectionLost;
        public event EventHandler<string>? Disconnected;

        /// <summary>
        /// Ensures permissions and discovers/ connects to the paired device by name.
        /// </summary>
        public async Task<(bool success, Exception? error)> ConnectAsync()
        {
            try
            {
                // Request Bluetooth permission (works cross-platform) 
                var status = await Permissions.RequestAsync<Permissions.Bluetooth>();
                if (status != PermissionStatus.Granted)
                    return (false, new Exception("Bluetooth permission denied"));

                // Ensure the radio is on 
                var radio = BluetoothRadio.Default;
                if (radio == null || radio.Mode == RadioMode.PowerOff)
                    return (false, new Exception("Bluetooth radio unavailable or off"));
                _client = new BluetoothClient();
                var devices = _client.DiscoverDevices();
                // paired devices 

                // Find by name
                var deviceInfo = devices.FirstOrDefault(d => d.DeviceName.Equals(TargetName, StringComparison.OrdinalIgnoreCase));
                if (deviceInfo == null)
                    return (false, new Exception($"Paired device '{TargetName}' not found"));

                // Connect RFCOMM SPP channel 
                _client.Connect(deviceInfo.DeviceAddress, BluetoothService.SerialPort);
                _stream = _client.GetStream();

                StartReading();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex);
            }
        }

        /// <summary>
        /// No extra initialization needed for Classic SPP.
        /// </summary>
        public Task<bool> InitializeAsync() =>
            Task.FromResult(true);

        /// <summary>
        /// Stops reading and closes connection.
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                _cts?.Cancel();
                _stream?.Close();
                _client?.Close();
                Disconnected?.Invoke(this, TargetName);
                await Task.CompletedTask;
            }
            catch (Exception)
            {

                Debug.WriteLine($"Error disconnecting from {TargetName}.");
            }
            
        }

        /// <summary>
        /// Spins up a background loop to read incoming bytes.
        /// </summary>
        private void StartReading()
        {
            _cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                var buffer = new byte[1024];
                try
                {
                    while (!_cts.IsCancellationRequested && _stream != null)
                    {
                        int read = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                        if (read > 0)
                        {
                            var chunk = buffer.Take(read).ToArray();
                            DataReceived?.Invoke(this, chunk);
                        }
                        else
                        {
                            ConnectionLost?.Invoke(this, TargetName);
                            await DisconnectAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConnectionLost?.Invoke(this, $"{TargetName}: {ex.Message}");
                }
            }, _cts.Token);
        }
    }
}
