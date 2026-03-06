namespace BluetoothDeskApp.Models;

public class LogEntry
{
    public string Timestamp { get; set; } = DateTime.Now.ToString("HH:mm:ss.fff");
    public string Type { get; set; } = "INFO";
    public string Message { get; set; } = string.Empty;
}
