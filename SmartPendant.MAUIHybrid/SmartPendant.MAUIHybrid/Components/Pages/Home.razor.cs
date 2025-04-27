using NAudio.Wave;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using SmartPendant.MAUIHybrid.Services;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Components.Pages
{
    public partial class Home
    {
        private readonly IBluetoothLE _ble = CrossBluetoothLE.Current;
        private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;
        private readonly ITranscriptionService _trancriptionService;
        private readonly IDevice _connectedDevice;
        private bool _connected = false;

        public Home(ITranscriptionService transcriptionService)
        {
            _trancriptionService = transcriptionService;
        }

        protected override async Task OnAfterRenderAsync(bool first)
        {
            await base.OnAfterRenderAsync(first);
            if (first)
            {
                _adapter.DeviceDiscovered += Adapter_DeviceDiscovered;
                await _adapter.StartScanningForDevicesAsync();
            }
        }

        private async void Adapter_DeviceDiscovered(object? sender, DeviceEventArgs e)
        {
            // This is where you can handle the discovered devices
            // For example, you can add them to a list or display them in the UI
            if (!_connected)
            {
                if (e.Device.Name == "ESP32")
                {
                    Debug.WriteLine($"Found ESP32 device: {e.Device.Name} - {e.Device.Id}");
                    // Stop scanning if you found the device you are looking for
                    await _adapter.StopScanningForDevicesAsync();
                    try
                    {
                        await _adapter.ConnectToDeviceAsync(e.Device);
                        _connected = true; 
                        await InvokeAsync(StateHasChanged);
                        var _connectedDevice = e.Device;
                        var size = await _connectedDevice.RequestMtuAsync(185); // Request a larger MTU size if needed
                        var service = await _connectedDevice.GetServiceAsync(Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b"));
                        var characteristic = await service.GetCharacteristicAsync(Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8"));
                        characteristic.ValueUpdated += async (o, args) =>
                        {
                            try
                            {
                                var bytes = args.Characteristic.Value;
                                await _trancriptionService.ProcessChunkAsync(bytes);
                            }
                            catch (Exception ex)
                            {
                                // Log or handle the exception properly
                                Console.WriteLine($"Error processing chunk: {ex}");
                            }
                        };
                        _trancriptionService.TranscriptReceived += (o, args) =>
                        {
                            Debug.WriteLine($"Transcript: {args}");
                        };
                        await _trancriptionService.InitializeAsync(new WaveFormat(8000, 16, 1));
                        await characteristic.StartUpdatesAsync();
                    }
                    catch (DeviceConnectionException ex)
                    {
                        // ... could not connect to device
                        Debug.WriteLine($"Could not connect to device: {ex}");
                    }
                    catch (Exception ex)
                    {
                        // ... other exceptions
                        Debug.WriteLine($"An error occurred: {ex}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Discovered device: {e.Device.Name} - {e.Device.Id}");
                }
            }
        }

        public async void StopRecording()
        {
            
            await _trancriptionService.StopAsync();
        }
    }
}
