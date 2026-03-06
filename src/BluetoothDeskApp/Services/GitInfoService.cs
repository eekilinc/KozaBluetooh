using System.Diagnostics;

namespace BluetoothDeskApp.Services;

public class GitInfoService : IGitInfoService
{
    public async Task<string> GetGitInfoAsync()
    {
        try
        {
            var branch = await RunGitAsync("rev-parse --abbrev-ref HEAD");
            var hash = await RunGitAsync("rev-parse --short HEAD");
            if (string.IsNullOrWhiteSpace(branch) || string.IsNullOrWhiteSpace(hash))
            {
                return "Git: repository not found";
            }

            return $"Git: {branch} ({hash})";
        }
        catch
        {
            return "Git: info unavailable";
        }
    }

    private static Task<string> RunGitAsync(string args)
    {
        var tcs = new TaskCompletionSource<string>();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.Exited += (_, _) =>
        {
            var output = process.StandardOutput.ReadToEnd().Trim();
            tcs.TrySetResult(output);
            process.Dispose();
        };

        process.Start();
        return tcs.Task;
    }
}
