namespace BluetoothDeskApp.Models;

public class ConnectionProfile
{
    public string Name { get; set; } = string.Empty;
    public string? ComPort { get; set; }
    public int BaudRate { get; set; } = 9600;
    public string CommandMode { get; set; } = "ASCII";
    public string LineEnding { get; set; } = "CRLF";
    public Guid? BleService { get; set; }
    public Guid? BleCharacteristic { get; set; }
    public BleWriteMode BleWriteMode { get; set; } = BleWriteMode.Auto;
}
