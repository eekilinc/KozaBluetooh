using System.Text;
using BluetoothDeskApp.Models;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;

namespace BluetoothDeskApp.Services;

public class BleBluetoothService : IBleBluetoothService
{
    private BluetoothLEDevice? _device;
    private GattCharacteristic? _ioCharacteristic;
    private BleWriteMode _writeMode = BleWriteMode.Auto;
    private bool _isIntentionalDisconnect;

    public event Action<string>? DataReceived;
    public event Action<string>? ErrorOccurred;
    public event Action<string>? ConnectionLost;

    public bool IsConnected => _device != null;

    public async Task<IReadOnlyList<(string Name, ulong Address)>> ScanAsync(TimeSpan duration)
    {
        var found = new Dictionary<ulong, string>();
        var watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        watcher.Received += (_, args) =>
        {
            var name = string.IsNullOrWhiteSpace(args.Advertisement.LocalName)
                ? $"BLE_{args.BluetoothAddress:X}"
                : args.Advertisement.LocalName;

            if (!found.ContainsKey(args.BluetoothAddress))
            {
                found[args.BluetoothAddress] = name;
            }
        };

        watcher.Start();
        await Task.Delay(duration);
        watcher.Stop();

        return found.Select(x => (x.Value, x.Key)).ToList();
    }

    public async Task ConnectAsync(ulong address)
    {
        _isIntentionalDisconnect = false;
        _device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
        if (_device == null)
        {
            throw new InvalidOperationException("BLE device could not be connected.");
        }

        _device.ConnectionStatusChanged -= DeviceOnConnectionStatusChanged;
        _device.ConnectionStatusChanged += DeviceOnConnectionStatusChanged;
    }

    public async Task<IReadOnlyList<Guid>> GetServicesAsync()
    {
        if (_device == null)
        {
            throw new InvalidOperationException("BLE is not connected.");
        }

        var result = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
        if (result.Status != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException($"Service fetch failed: {result.Status}");
        }

        return result.Services.Select(s => s.Uuid).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetCharacteristicsAsync(Guid serviceId)
    {
        if (_device == null)
        {
            throw new InvalidOperationException("BLE is not connected.");
        }

        var serviceResult = await _device.GetGattServicesForUuidAsync(serviceId, BluetoothCacheMode.Uncached);
        var service = serviceResult.Services.FirstOrDefault();
        if (service == null)
        {
            throw new InvalidOperationException("Service not found.");
        }

        var charsResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
        if (charsResult.Status != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException($"Characteristic fetch failed: {charsResult.Status}");
        }

        return charsResult.Characteristics.Select(c => c.Uuid).ToList();
    }

    public async Task ConfigureIoAsync(Guid serviceId, Guid characteristicId, BleWriteMode writeMode = BleWriteMode.Auto)
    {
        if (_device == null)
        {
            throw new InvalidOperationException("BLE is not connected.");
        }

        var serviceResult = await _device.GetGattServicesForUuidAsync(serviceId, BluetoothCacheMode.Uncached);
        var service = serviceResult.Services.FirstOrDefault();
        if (service == null)
        {
            throw new InvalidOperationException("Service not found.");
        }

        var charResult = await service.GetCharacteristicsForUuidAsync(characteristicId, BluetoothCacheMode.Uncached);
        _ioCharacteristic = charResult.Characteristics.FirstOrDefault();
        if (_ioCharacteristic == null)
        {
            throw new InvalidOperationException("Characteristic not found.");
        }

        _writeMode = writeMode;

        _ioCharacteristic.ValueChanged -= IoCharacteristicOnValueChanged;
        _ioCharacteristic.ValueChanged += IoCharacteristicOnValueChanged;

        var cccdResult = await _ioCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue.Notify);

        if (cccdResult != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException($"Notify enable failed: {cccdResult}");
        }
    }

    public async Task SendAsync(string text)
    {
        await SendBytesAsync(Encoding.UTF8.GetBytes(text));
    }

    public async Task SendBytesAsync(byte[] bytes)
    {
        if (_ioCharacteristic == null)
        {
            throw new InvalidOperationException("BLE IO characteristic is not selected.");
        }

        var buffer = CryptographicBuffer.CreateFromByteArray(bytes);
        var option = ResolveWriteOption(_ioCharacteristic);

        var result = await _ioCharacteristic.WriteValueAsync(buffer, option);
        if (result != GattCommunicationStatus.Success && option == GattWriteOption.WriteWithoutResponse)
        {
            result = await _ioCharacteristic.WriteValueAsync(buffer, GattWriteOption.WriteWithResponse);
        }

        if (result != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException($"BLE write failed: {result}");
        }
    }

    private GattWriteOption ResolveWriteOption(GattCharacteristic characteristic)
    {
        var props = characteristic.CharacteristicProperties;
        var canWithout = props.HasFlag(GattCharacteristicProperties.WriteWithoutResponse);
        var canWith = props.HasFlag(GattCharacteristicProperties.Write);

        return _writeMode switch
        {
            BleWriteMode.WriteWithResponse when canWith => GattWriteOption.WriteWithResponse,
            BleWriteMode.WriteWithoutResponse when canWithout => GattWriteOption.WriteWithoutResponse,
            BleWriteMode.WriteWithResponse => throw new InvalidOperationException("Characteristic WriteWithResponse desteklemiyor."),
            BleWriteMode.WriteWithoutResponse => throw new InvalidOperationException("Characteristic WriteWithoutResponse desteklemiyor."),
            _ when canWithout => GattWriteOption.WriteWithoutResponse,
            _ when canWith => GattWriteOption.WriteWithResponse,
            _ => throw new InvalidOperationException("Characteristic yazma desteklemiyor.")
        };
    }

    public async Task DisconnectAsync()
    {
        _isIntentionalDisconnect = true;
        if (_ioCharacteristic != null)
        {
            try
            {
                _ioCharacteristic.ValueChanged -= IoCharacteristicOnValueChanged;
            }
            catch
            {
                // ignore
            }

            try
            {
                await _ioCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.None);
            }
            catch
            {
                // device may already be gone, ignore during disconnect
            }

            _ioCharacteristic = null;
        }

        try
        {
            if (_device != null)
            {
                _device.ConnectionStatusChanged -= DeviceOnConnectionStatusChanged;
            }
            _device?.Dispose();
        }
        catch
        {
            // ignore cleanup errors
        }

        _device = null;
        _isIntentionalDisconnect = false;
    }

    private void DeviceOnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (_isIntentionalDisconnect)
        {
            return;
        }

        if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
        {
            ConnectionLost?.Invoke("BLE bağlantısı koptu.");
        }
    }

    private void IoCharacteristicOnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        try
        {
            CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out var bytes);
            var text = Encoding.UTF8.GetString(bytes);
            DataReceived?.Invoke(text);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"BLE read error: {ex.Message}");
        }
    }
}
