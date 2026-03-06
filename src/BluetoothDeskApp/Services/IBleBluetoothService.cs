namespace BluetoothDeskApp.Services;

public interface IBleBluetoothService
{
    event Action<string>? DataReceived;
    event Action<string>? ErrorOccurred;
    bool IsConnected { get; }

    Task<IReadOnlyList<(string Name, ulong Address)>> ScanAsync(TimeSpan duration);
    Task ConnectAsync(ulong address);
    Task<IReadOnlyList<Guid>> GetServicesAsync();
    Task<IReadOnlyList<Guid>> GetCharacteristicsAsync(Guid serviceId);
    Task ConfigureIoAsync(Guid serviceId, Guid characteristicId);
    Task SendAsync(string text);
    Task SendBytesAsync(byte[] bytes);
    Task DisconnectAsync();
}
