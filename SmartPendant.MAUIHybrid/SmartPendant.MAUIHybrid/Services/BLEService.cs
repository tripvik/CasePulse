using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Services
{
    public class BLEService
    {
        private readonly IBluetoothLE _ble = CrossBluetoothLE.Current;
        private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;
        public EventHandler<DeviceEventArgs>? PendantDisconnected;
        public EventHandler<DeviceErrorEventArgs>? PendantConnectionLost;
        private IDevice? _connectedDevice = null;
        private readonly Guid _serviceId = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
        private readonly Guid _characteristicId = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8"); 


        public bool IsConnected {
            get
            {
                return _connectedDevice != null && _connectedDevice.State == DeviceState.Connected;
            }
        }

        public BLEService()
        {
            _adapter.DeviceDisconnected += Adapter_DeviceDisconnected;
            _adapter.DeviceConnectionLost += Adapter_DeviceConnectionLost;
        }

        private void Adapter_DeviceConnectionLost(object? sender, DeviceErrorEventArgs e)
        {
            if(e.Device.Id == _connectedDevice?.Id)
            {
                Debug.WriteLine($"Connection lost to device: {e.Device.Name} - {e.Device.Id}");
                PendantConnectionLost?.Invoke(this, e);
            }
        }

        private void Adapter_DeviceDisconnected(object? sender, DeviceEventArgs e)
        {
            if (e.Device.Id == _connectedDevice?.Id)
            {
                Debug.WriteLine($"Device Disconnected : {e.Device.Name} - {e.Device.Id}");
                PendantDisconnected?.Invoke(this, e);
            }
        }

        public async Task<(bool,Exception?)> ConnectToPendant()
        {
            if(_connectedDevice != null && _connectedDevice.State == DeviceState.Connected)
            {
                Debug.WriteLine("Already connected to a device.");
                return (true,null);
            }
            else
            {
                if(await HasCorrectPermissions())
                {
                    //create a cancellation token for 30s
                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(30000);
                    try
                    {
                        _connectedDevice = await _adapter.ConnectToKnownDeviceAsync(GetDeviceId(), default, cts.Token);
                        if (_connectedDevice == null)
                        {
                            // Handle the case when the device is not found or connection fails
                            return (false,new Exception("Unknown error while connecting"));
                        }
                        Debug.WriteLine($"MTU - {await _connectedDevice.RequestMtuAsync(250)}");
                    }
                    catch (DeviceConnectionException ex)
                    {
                        // ... could not connect to device
                        Debug.WriteLine($"Could not connect to device: {ex}");
                        return (false, ex);
                    }
                    catch (Exception ex)
                    {
                        // ... other exceptions
                        Debug.WriteLine($"An error occurred: {ex}");
                        return (false, ex);
                    }
                }      
            }
            return (true, null);

        }

           public async Task<(bool, ICharacteristic?)> GetCharacteristic(Guid? serviceId = null, Guid? characteristicId = null)
        {
            try
            {
                // Use the default instance fields if the parameters are null
                var resolvedServiceId = serviceId ?? _serviceId;
                var resolvedCharacteristicId = characteristicId ?? _characteristicId;

                if (_connectedDevice == null)
                {
                    Debug.WriteLine("Error: No connected device.");
                    return (false, null);
                }

                var service = await _connectedDevice.GetServiceAsync(resolvedServiceId);
                if (service == null)
                {
                    Debug.WriteLine($"Error: Service {resolvedServiceId} not found.");
                    return (false, null);
                }

                var characteristic = await service.GetCharacteristicAsync(resolvedCharacteristicId);
                if (characteristic == null)
                {
                    Debug.WriteLine($"Error: Characteristic {resolvedCharacteristicId} not found.");
                    return (false, null);
                }

                return (true, characteristic);
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Debug.WriteLine($"Error in GetCharacteristic: {ex.Message}");
                return (false, null);
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

        public async Task DisconnectFromPendant()
        {
            if (_connectedDevice != null)
            {
                await _adapter.DisconnectDeviceAsync(_connectedDevice);
                _connectedDevice = null;
            }
        }

        private System.Guid GetDeviceId()
        {
            // Eventually get from configuration or save device  
            return new System.Guid("00000000-0000-0000-0000-f024f99b2a12");
        }
    }
}
