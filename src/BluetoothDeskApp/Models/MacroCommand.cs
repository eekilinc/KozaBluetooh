namespace BluetoothDeskApp.Models;

public class MacroCommand
{
    public string Name { get; set; } = string.Empty;
    public string CommandsText { get; set; } = string.Empty;
    public int DelayMs { get; set; } = 300;
}
