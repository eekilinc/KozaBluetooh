namespace BluetoothDeskApp.Models;

public class DiscoveredDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DeviceTransport Transport { get; set; }
    public ulong? BleAddress { get; set; }
    public string? ClassicComPort { get; set; }
    public bool IsPairedOnlyClassic { get; set; }

    public string DisplayId
    {
        get
        {
            if (Transport == DeviceTransport.ClassicSerial)
            {
                if (!string.IsNullOrWhiteSpace(ClassicComPort))
                {
                    return $"Classic - {ClassicComPort}";
                }

                if (IsPairedOnlyClassic)
                {
                    return "Classic - Paired (no COM)";
                }
            }

            return $"{Transport} - {Id}";
        }
    }
}
