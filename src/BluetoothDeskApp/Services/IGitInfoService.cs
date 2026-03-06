namespace BluetoothDeskApp.Services;

public interface IGitInfoService
{
    Task<string> GetGitInfoAsync();
}
