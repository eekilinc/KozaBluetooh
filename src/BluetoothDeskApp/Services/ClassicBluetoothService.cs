using System.IO.Ports;
using System.Management;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Text;

namespace BluetoothDeskApp.Services;

public class ClassicBluetoothService : IClassicBluetoothService
{
    private SerialPort? _serialPort;
    private BluetoothDevice? _rfcommDevice;
    private RfcommDeviceService? _rfcommService;
    private StreamSocket? _rfcommSocket;
    private DataWriter? _rfcommWriter;
    private DataReader? _rfcommReader;
    private CancellationTokenSource? _rfcommReadCts;
    private Task? _rfcommReadTask;

    public event Action<string>? DataReceived;
    public event Action<string>? ErrorOccurred;

    public bool IsConnected => _serialPort?.IsOpen == true || _rfcommSocket != null;

    public Task<IReadOnlyList<(string Port, string FriendlyName)>> GetAvailablePortsAsync()
    {
        var ports = SerialPort.GetPortNames().OrderBy(p => p).ToList();
        var result = new List<(string, string)>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
            var names = searcher.Get().Cast<ManagementObject>()
                .Select(mo => mo["Name"]?.ToString() ?? string.Empty)
                .ToList();

            foreach (var port in ports)
            {
                var friendly = names.FirstOrDefault(n => n.Contains($"({port})", StringComparison.OrdinalIgnoreCase)) ?? port;
                result.Add((port, friendly));
            }
        }
        catch
        {
            result.AddRange(ports.Select(p => (p, p)));
        }

        return Task.FromResult<IReadOnlyList<(string, string)>>(result);
    }

    public async Task<IReadOnlyList<(string Name, string DeviceId)>> GetPairedDevicesAsync()
    {
        var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var infoList = await DeviceInformation.FindAllAsync(selector);

        var paired = infoList
            .Select(d => (Name: string.IsNullOrWhiteSpace(d.Name) ? "Unnamed Classic Device" : d.Name, DeviceId: d.Id))
            .Distinct()
            .ToList();

        return paired;
    }

    public Task ConnectAsync(string portName, int baudRate)
    {
        try
        {
            DisconnectRfcomm();

            _serialPort = new SerialPort(portName, baudRate)
            {
                NewLine = "\r\n",
                ReadTimeout = 500,
                WriteTimeout = 500
            };
            _serialPort.DataReceived += SerialPortOnDataReceived;
            _serialPort.Open();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Classic connect error: {ex.Message}");
            throw;
        }
    }

    public async Task ConnectToPairedDeviceAsync(string deviceId)
    {
        try
        {
            await DisconnectAsync();

            _rfcommDevice = await BluetoothDevice.FromIdAsync(deviceId);
            if (_rfcommDevice == null)
            {
                throw new InvalidOperationException("Paired device could not be opened.");
            }

            var servicesResult = await _rfcommDevice.GetRfcommServicesForIdAsync(RfcommServiceId.SerialPort);
            if (servicesResult.Services.Count == 0)
            {
                throw new InvalidOperationException("No SPP (RFCOMM) service found on paired device.");
            }

            _rfcommService = servicesResult.Services[0];

            _rfcommSocket = new StreamSocket();
            await _rfcommSocket.ConnectAsync(
                _rfcommService.ConnectionHostName,
                _rfcommService.ConnectionServiceName,
                SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            _rfcommWriter = new DataWriter(_rfcommSocket.OutputStream);
            _rfcommReader = new DataReader(_rfcommSocket.InputStream)
            {
                InputStreamOptions = InputStreamOptions.Partial
            };

            _rfcommReadCts = new CancellationTokenSource();
            _rfcommReadTask = Task.Run(() => RfcommReadLoopAsync(_rfcommReadCts.Token));
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Classic RFCOMM connect error: {ex.Message}");
            DisconnectRfcomm();
            throw;
        }
    }

    public async Task SendAsync(string text)
    {
        await SendBytesAsync(Encoding.ASCII.GetBytes(text));
    }

    public async Task SendBytesAsync(byte[] bytes)
    {
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.Write(bytes, 0, bytes.Length);
            return;
        }

        if (_rfcommWriter != null)
        {
            _rfcommWriter.WriteBytes(bytes);
            await _rfcommWriter.StoreAsync();
            await _rfcommWriter.FlushAsync();
            return;
        }

        throw new InvalidOperationException("Classic connection is not active.");
    }

    public Task DisconnectAsync()
    {
        if (_serialPort != null)
        {
            _serialPort.DataReceived -= SerialPortOnDataReceived;
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            _serialPort.Dispose();
            _serialPort = null;
        }

        DisconnectRfcomm();
        return Task.CompletedTask;
    }

    private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var data = _serialPort?.ReadExisting();
            if (!string.IsNullOrWhiteSpace(data))
            {
                DataReceived?.Invoke(data.Trim());
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Classic read error: {ex.Message}");
        }
    }

    private async Task RfcommReadLoopAsync(CancellationToken token)
    {
        if (_rfcommReader == null)
        {
            return;
        }

        try
        {
            while (!token.IsCancellationRequested)
            {
                var loaded = await _rfcommReader.LoadAsync(256);
                if (loaded == 0)
                {
                    continue;
                }

                var bytes = new byte[loaded];
                _rfcommReader.ReadBytes(bytes);
                var text = Encoding.ASCII.GetString(bytes).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    DataReceived?.Invoke(text);
                }
            }
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                ErrorOccurred?.Invoke($"Classic RFCOMM read error: {ex.Message}");
            }
        }
    }

    private void DisconnectRfcomm()
    {
        _rfcommReadCts?.Cancel();
        _rfcommReadCts?.Dispose();
        _rfcommReadCts = null;

        _rfcommReader?.DetachStream();
        _rfcommReader?.Dispose();
        _rfcommReader = null;

        _rfcommWriter?.DetachStream();
        _rfcommWriter?.Dispose();
        _rfcommWriter = null;

        _rfcommSocket?.Dispose();
        _rfcommSocket = null;

        _rfcommService?.Dispose();
        _rfcommService = null;

        _rfcommDevice?.Dispose();
        _rfcommDevice = null;

        _rfcommReadTask = null;
    }
}
