namespace BluetoothDeskApp.Services;

public interface ISimulatorService
{
    event Action<string>? DataReceived;
    bool IsConnected { get; }

    Task ConnectAsync();
    Task SendAsync(string text);
    Task SendBytesAsync(byte[] bytes);
    Task DisconnectAsync();
}
