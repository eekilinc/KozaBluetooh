using System.Text;

namespace BluetoothDeskApp.Services;

public class SimulatorService : ISimulatorService
{
    private CancellationTokenSource? _cts;

    public event Action<string>? DataReceived;
    public bool IsConnected { get; private set; }

    public Task ConnectAsync()
    {
        IsConnected = true;
        _cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            var rnd = new Random();
            while (!_cts.Token.IsCancellationRequested)
            {
                var msg = $"SIM_DATA temp={20 + rnd.NextDouble() * 5:F2}C hum={40 + rnd.NextDouble() * 10:F2}%";
                DataReceived?.Invoke(msg);
                await Task.Delay(1000, _cts.Token);
            }
        }, _cts.Token);

        return Task.CompletedTask;
    }

    public Task SendAsync(string text)
    {
        DataReceived?.Invoke($"SIM_ECHO {text}");
        return Task.CompletedTask;
    }

    public Task SendBytesAsync(byte[] bytes)
    {
        var printable = bytes.All(b => b >= 32 && b <= 126);
        if (printable)
        {
            var text = Encoding.ASCII.GetString(bytes);
            DataReceived?.Invoke($"SIM_ECHO {text}");
        }
        else
        {
            var hex = BitConverter.ToString(bytes).Replace("-", " ");
            DataReceived?.Invoke($"SIM_ECHO_HEX {hex}");
        }

        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        IsConnected = false;
        return Task.CompletedTask;
    }
}
