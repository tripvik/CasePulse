using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using SmartPendant.MAUIHybrid.Abstractions;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Services
{
    public class BLEService : IConnectionService
    {
        private readonly IBluetoothLE _ble = CrossBluetoothLE.Current;
        private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;
        private IDevice? _connectedDevice;
        private ICharacteristic? _characteristic;
        private readonly Guid _serviceId = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
        private readonly Guid _characteristicId = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8");

        public event EventHandler<byte[]>? DataReceived;
        public event EventHandler<string>? ConnectionLost;
        public event EventHandler<string>? Disconnected;

        public bool IsConnected =>
            _connectedDevice != null && _connectedDevice.State == DeviceState.Connected;

        public BLEService()
        {
            _adapter.DeviceConnectionLost += (s, e) =>
            {
                if (_connectedDevice?.Id == e.Device.Id)
                    ConnectionLost?.Invoke(this, e.ErrorMessage ?? "Unknown");
            };

            _adapter.DeviceDisconnected += (s, e) =>
            {
                if (_connectedDevice?.Id == e.Device.Id)
                    Disconnected?.Invoke(this, "Disconnected");
            };
        }

        public async Task<(bool, Exception?)> ConnectAsync()
        {
            if (IsConnected)
                return (true, null);

            if (!await HasCorrectPermissions())
                return (false, new Exception("Bluetooth permission not granted"));

            try
            {
                var deviceId = new Guid("00000000-0000-0000-0000-f024f99b2a12");
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                _connectedDevice = await _adapter.ConnectToKnownDeviceAsync(deviceId, default, cts.Token);
                Debug.WriteLine($"MTU - {await _connectedDevice.RequestMtuAsync(250)}");
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex);
            }
        }

        //Implement the ValueTuple logic like ConnectAsync
        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (_connectedDevice == null)
                {
                    Debug.WriteLine("Error: No connected device.");
                    return (false);
                }

                var service = await _connectedDevice.GetServiceAsync(_serviceId);
                if (service == null) return false;

                _characteristic = await service.GetCharacteristicAsync(_characteristicId);
                if (_characteristic == null) return false;

                _characteristic.ValueUpdated += (s, e) =>
                {
                    var data = e.Characteristic.Value;
                    DataReceived?.Invoke(this, data);
                };

                await _characteristic.StartUpdatesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
            
        }

        public async Task DisconnectAsync()
        {
            if (_connectedDevice != null)
            {
                await _adapter.DisconnectDeviceAsync(_connectedDevice);
                _connectedDevice = null;
            }
        }

        private async Task<bool> HasCorrectPermissions()
        {
            // Check if Bluetooth is enabled
            if (!_ble.IsOn)
            {
                Debug.WriteLine("Bluetooth is not enabled.");
                return false;
            }
            Debug.WriteLine("Verifying Bluetooth permissions..");
            var permissionResult = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (permissionResult != PermissionStatus.Granted)
            {
                permissionResult = await Permissions.RequestAsync<Permissions.Bluetooth>();
            }
            Debug.WriteLine($"Result of requesting Bluetooth permissions: '{permissionResult}'");
            if (permissionResult != PermissionStatus.Granted)
            {
                Debug.WriteLine("Permissions not available, direct user to settings screen.");
                AppInfo.ShowSettingsUI();
                return false;
            }

            return true;
        }
    }

}