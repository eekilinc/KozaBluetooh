namespace BluetoothDeskApp.Services;

public interface IClassicBluetoothService
{
    event Action<string>? DataReceived;
    event Action<string>? ErrorOccurred;
    event Action<string>? ConnectionLost;
    bool IsConnected { get; }

    Task<IReadOnlyList<(string Port, string FriendlyName)>> GetAvailablePortsAsync();
    Task<IReadOnlyList<(string Name, string DeviceId)>> GetPairedDevicesAsync();
    Task ConnectAsync(string portName, int baudRate);
    Task ConnectToPairedDeviceAsync(string deviceId);
    Task SendAsync(string text);
    Task SendBytesAsync(byte[] bytes);
    Task DisconnectAsync();
}
