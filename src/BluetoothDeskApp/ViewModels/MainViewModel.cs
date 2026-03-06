using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using BluetoothDeskApp.Infrastructure;
using BluetoothDeskApp.Models;
using BluetoothDeskApp.Services;
using Microsoft.Win32;

namespace BluetoothDeskApp.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly IClassicBluetoothService _classic;
    private readonly IBleBluetoothService _ble;
    private readonly ISimulatorService _sim;
    private readonly IGitInfoService _git;

    private DiscoveredDevice? _selectedDevice;
    private string _commandText = string.Empty;
    private string _statusText = "Bağlı değil";
    private Brush _statusBrush = Brushes.Gray;
    private string _gitInfoText = "Git: ...";
    private string _selectedLanguage = "Türkçe";
    private string _appWindowTitle = "KozaBluetooth";
    private string _aboutTitle = "Hakkımda";
    private string _aboutText = "KozaBluetooth\nBluetooth Classic + BLE terminal uygulaması.";

    private string _scanTitle = "Cihaz Tarama";
    private string _settingsTitle = "Ayarlar";
    private string _commandTitle = "Komut Gönderme";
    private string _liveFlowTitle = "Canlı Veri Akışı (Gelen/Giden/Hata)";
    private string _logTitle = "Log ve Hata Kayıtları";

    private string _scanClassicText = "Classic Tara";
    private string _scanBleText = "BLE Tara";
    private string _addSimulatorText = "Simülatör Ekle";
    private string _openBtSettingsText = "Bluetooth Ayarları";
    private string _connectText = "Bağlan";
    private string _disconnectText = "Bağlantıyı Kes";
    private string _reconnectText = "Yeniden Bağlan";
    private string _sendText = "Gönder";
    private string _sendModeText = "Gönderim Modu";
    private string _lineEndingText = "Satır Sonu";
    private string _comPortText = "COM Port";
    private string _baudRateText = "Baud Rate";
    private string _bleServiceText = "BLE Servis";
    private string _bleCharacteristicText = "BLE Karakteristik (Write/Notify)";
    private string _loadServicesText = "Servisleri Yükle";
    private string _loadCharsText = "Karakteristik Yükle";
    private string _applyBleText = "BLE Seçimi Uygula";
    private string _languageLabelText = "Dil";
    private string _exportFlowText = "Akış CSV";
    private string _exportLogText = "Log TXT";
    private string _hexHintText = "HEX örnek: AA 01 0D 0A";

    private string? _selectedComPort;
    private int _selectedBaudRate = 9600;
    private Guid? _selectedBleService;
    private Guid? _selectedBleCharacteristic;
    private string _selectedCommandMode = "ASCII";
    private string _selectedLineEnding = "CRLF";

    private DeviceTransport? _activeTransport;
    private DeviceTransport? _lastTransport;
    private ConnectionState _currentState = ConnectionState.Disconnected;
    private string? _lastComPort;
    private int _lastBaudRate = 9600;
    private string? _lastClassicDeviceId;
    private ulong? _lastBleAddress;

    public ObservableCollection<DiscoveredDevice> Devices { get; } = new();
    public ObservableCollection<LogEntry> Traffic { get; } = new();
    public ObservableCollection<LogEntry> Logs { get; } = new();
    public ObservableCollection<string> LanguageOptions { get; } = new() { "Türkçe", "English" };
    public ObservableCollection<string> CommandModeOptions { get; } = new() { "ASCII", "HEX" };
    public ObservableCollection<string> LineEndingOptions { get; } = new() { "NONE", "LF", "CR", "CRLF" };

    public ObservableCollection<string> ClassicPorts { get; } = new();
    public ObservableCollection<int> BaudRates { get; } = new() { 9600, 19200, 38400, 57600, 115200 };
    public ObservableCollection<Guid> BleServices { get; } = new();
    public ObservableCollection<Guid> BleCharacteristics { get; } = new();

    public DiscoveredDevice? SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
    }

    public string? SelectedComPort
    {
        get => _selectedComPort;
        set => SetProperty(ref _selectedComPort, value);
    }

    public int SelectedBaudRate
    {
        get => _selectedBaudRate;
        set => SetProperty(ref _selectedBaudRate, value);
    }

    public Guid? SelectedBleService
    {
        get => _selectedBleService;
        set => SetProperty(ref _selectedBleService, value);
    }

    public Guid? SelectedBleCharacteristic
    {
        get => _selectedBleCharacteristic;
        set => SetProperty(ref _selectedBleCharacteristic, value);
    }

    public string CommandText
    {
        get => _commandText;
        set => SetProperty(ref _commandText, value);
    }

    public string SelectedCommandMode
    {
        get => _selectedCommandMode;
        set => SetProperty(ref _selectedCommandMode, value);
    }

    public string SelectedLineEnding
    {
        get => _selectedLineEnding;
        set => SetProperty(ref _selectedLineEnding, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public Brush StatusBrush
    {
        get => _statusBrush;
        set => SetProperty(ref _statusBrush, value);
    }

    public string GitInfoText
    {
        get => _gitInfoText;
        set => SetProperty(ref _gitInfoText, value);
    }

    public string AppWindowTitle
    {
        get => _appWindowTitle;
        set => SetProperty(ref _appWindowTitle, value);
    }

    public string AboutTitle
    {
        get => _aboutTitle;
        set => SetProperty(ref _aboutTitle, value);
    }

    public string AboutText
    {
        get => _aboutText;
        set => SetProperty(ref _aboutText, value);
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (!SetProperty(ref _selectedLanguage, value))
            {
                return;
            }

            UpdateUiTexts();
            SetStatus(_currentState);
        }
    }

    public string ScanTitle { get => _scanTitle; set => SetProperty(ref _scanTitle, value); }
    public string SettingsTitle { get => _settingsTitle; set => SetProperty(ref _settingsTitle, value); }
    public string CommandTitle { get => _commandTitle; set => SetProperty(ref _commandTitle, value); }
    public string LiveFlowTitle { get => _liveFlowTitle; set => SetProperty(ref _liveFlowTitle, value); }
    public string LogTitle { get => _logTitle; set => SetProperty(ref _logTitle, value); }
    public string ScanClassicText { get => _scanClassicText; set => SetProperty(ref _scanClassicText, value); }
    public string ScanBleText { get => _scanBleText; set => SetProperty(ref _scanBleText, value); }
    public string AddSimulatorText { get => _addSimulatorText; set => SetProperty(ref _addSimulatorText, value); }
    public string OpenBtSettingsText { get => _openBtSettingsText; set => SetProperty(ref _openBtSettingsText, value); }
    public string ConnectText { get => _connectText; set => SetProperty(ref _connectText, value); }
    public string DisconnectText { get => _disconnectText; set => SetProperty(ref _disconnectText, value); }
    public string ReconnectText { get => _reconnectText; set => SetProperty(ref _reconnectText, value); }
    public string SendText { get => _sendText; set => SetProperty(ref _sendText, value); }
    public string SendModeText { get => _sendModeText; set => SetProperty(ref _sendModeText, value); }
    public string LineEndingText { get => _lineEndingText; set => SetProperty(ref _lineEndingText, value); }
    public string ComPortText { get => _comPortText; set => SetProperty(ref _comPortText, value); }
    public string BaudRateText { get => _baudRateText; set => SetProperty(ref _baudRateText, value); }
    public string BleServiceText { get => _bleServiceText; set => SetProperty(ref _bleServiceText, value); }
    public string BleCharacteristicText { get => _bleCharacteristicText; set => SetProperty(ref _bleCharacteristicText, value); }
    public string LoadServicesText { get => _loadServicesText; set => SetProperty(ref _loadServicesText, value); }
    public string LoadCharsText { get => _loadCharsText; set => SetProperty(ref _loadCharsText, value); }
    public string ApplyBleText { get => _applyBleText; set => SetProperty(ref _applyBleText, value); }
    public string LanguageLabelText { get => _languageLabelText; set => SetProperty(ref _languageLabelText, value); }
    public string ExportFlowText { get => _exportFlowText; set => SetProperty(ref _exportFlowText, value); }
    public string ExportLogText { get => _exportLogText; set => SetProperty(ref _exportLogText, value); }
    public string HexHintText { get => _hexHintText; set => SetProperty(ref _hexHintText, value); }

    public AsyncRelayCommand ScanClassicCommand { get; }
    public AsyncRelayCommand ScanBleCommand { get; }
    public RelayCommand AddSimulatorCommand { get; }
    public AsyncRelayCommand ConnectCommand { get; }
    public AsyncRelayCommand DisconnectCommand { get; }
    public AsyncRelayCommand ReconnectCommand { get; }
    public AsyncRelayCommand SendCommand { get; }
    public AsyncRelayCommand SendPresetStartCommand { get; }
    public AsyncRelayCommand SendPresetStopCommand { get; }
    public AsyncRelayCommand SendPresetStatusCommand { get; }
    public AsyncRelayCommand SendPresetResetCommand { get; }
    public AsyncRelayCommand LoadBleServicesCommand { get; }
    public AsyncRelayCommand LoadBleCharacteristicsCommand { get; }
    public AsyncRelayCommand ApplyBleSelectionCommand { get; }
    public RelayCommand ExportTrafficCsvCommand { get; }
    public RelayCommand ExportLogsTxtCommand { get; }
    public RelayCommand OpenBluetoothSettingsCommand { get; }

    public MainViewModel(
        IClassicBluetoothService classic,
        IBleBluetoothService ble,
        ISimulatorService simulator,
        IGitInfoService git)
    {
        _classic = classic;
        _ble = ble;
        _sim = simulator;
        _git = git;

        _classic.DataReceived += OnData;
        _classic.ErrorOccurred += OnError;
        _ble.DataReceived += OnData;
        _ble.ErrorOccurred += OnError;
        _sim.DataReceived += OnData;

        ScanClassicCommand = new AsyncRelayCommand(ScanClassicAsync);
        ScanBleCommand = new AsyncRelayCommand(ScanBleAsync);
        AddSimulatorCommand = new RelayCommand(AddSimulator);
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
        ReconnectCommand = new AsyncRelayCommand(ReconnectAsync);
        SendCommand = new AsyncRelayCommand(SendAsync);

        SendPresetStartCommand = new AsyncRelayCommand(() => SendPresetAsync("START"));
        SendPresetStopCommand = new AsyncRelayCommand(() => SendPresetAsync("STOP"));
        SendPresetStatusCommand = new AsyncRelayCommand(() => SendPresetAsync("STATUS"));
        SendPresetResetCommand = new AsyncRelayCommand(() => SendPresetAsync("RESET"));

        LoadBleServicesCommand = new AsyncRelayCommand(LoadBleServicesAsync);
        LoadBleCharacteristicsCommand = new AsyncRelayCommand(LoadBleCharacteristicsAsync);
        ApplyBleSelectionCommand = new AsyncRelayCommand(ApplyBleSelectionAsync);
        ExportTrafficCsvCommand = new RelayCommand(ExportTrafficCsv);
        ExportLogsTxtCommand = new RelayCommand(ExportLogsTxt);
        OpenBluetoothSettingsCommand = new RelayCommand(OpenBluetoothSettings);

        UpdateUiTexts();

        _ = LoadGitInfoAsync();
    }

    private async Task LoadGitInfoAsync()
    {
        GitInfoText = await _git.GetGitInfoAsync();
        RefreshAboutText();
    }

    private async Task ScanClassicAsync()
    {
        try
        {
            RemoveDevicesByTransport(DeviceTransport.ClassicSerial);

            var ports = await _classic.GetAvailablePortsAsync();
            var pairedClassic = await _classic.GetPairedDevicesAsync();
            ClassicPorts.Clear();

            var pairedByName = pairedClassic
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToDictionary(p => p.Name, p => p.DeviceId, StringComparer.OrdinalIgnoreCase);

            foreach (var (port, friendlyName) in ports)
            {
                ClassicPorts.Add(port);

                var matchedPairedName = pairedByName.Keys.FirstOrDefault(name =>
                    friendlyName.Contains(name, StringComparison.OrdinalIgnoreCase));
                var pairedDeviceId = matchedPairedName != null
                    ? pairedByName[matchedPairedName]
                    : null;

                if (matchedPairedName != null)
                {
                    pairedByName.Remove(matchedPairedName);
                }

                Devices.Add(new DiscoveredDevice
                {
                    Id = pairedDeviceId ?? port,
                    Name = friendlyName,
                    Transport = DeviceTransport.ClassicSerial,
                    ClassicComPort = port
                });
            }

            foreach (var (name, deviceId) in pairedByName.Select(k => (k.Key, k.Value)))
            {
                Devices.Add(new DiscoveredDevice
                {
                    Id = deviceId,
                    Name = name,
                    Transport = DeviceTransport.ClassicSerial,
                    IsPairedOnlyClassic = true
                });
            }

            AddLog("BILGI", $"Classic tarama tamamlandı. COM: {ports.Count}, eşleşmiş cihaz: {pairedClassic.Count}.");
            if (ports.Count == 0)
            {
                AddLog("UYARI", "Bluetooth seri COM bulunamadı. Windows'ta eşleyip giden COM port oluşturun.");
            }
        }
        catch (Exception ex)
        {
            OnError($"Classic tarama hatası: {ex.Message}");
        }
    }

    private async Task ScanBleAsync()
    {
        try
        {
            RemoveDevicesByTransport(DeviceTransport.Ble);
            var list = await _ble.ScanAsync(TimeSpan.FromSeconds(5));
            foreach (var (name, address) in list)
            {
                Devices.Add(new DiscoveredDevice
                {
                    Id = address.ToString(),
                    Name = name,
                    Transport = DeviceTransport.Ble,
                    BleAddress = address
                });
            }

            AddLog("BILGI", $"BLE tarama tamamlandı. {list.Count} cihaz bulundu.");
        }
        catch (Exception ex)
        {
            OnError($"BLE tarama hatası: {ex.Message}");
        }
    }

    private void AddSimulator()
    {
        Devices.Add(new DiscoveredDevice
        {
            Id = "SIM-001",
            Name = "Simulator Device",
            Transport = DeviceTransport.Simulator
        });
        AddLog("BILGI", "Simülatör cihaz eklendi.");
    }

    private async Task ConnectAsync()
    {
        if (SelectedDevice == null)
        {
            AddLog("UYARI", "Önce bir cihaz seçin.");
            return;
        }

        SetStatus(ConnectionState.Connecting);

        try
        {
            switch (SelectedDevice.Transport)
            {
                case DeviceTransport.ClassicSerial:
                    var port = SelectedComPort ?? SelectedDevice.ClassicComPort;
                    if (!string.IsNullOrWhiteSpace(port))
                    {
                        await _classic.ConnectAsync(port, SelectedBaudRate);
                        _lastComPort = port;
                        _lastBaudRate = SelectedBaudRate;
                        _lastClassicDeviceId = null;
                        AddLog("BILGI", $"Classic COM ile bağlandı: {port}");
                    }
                    else
                    {
                        await _classic.ConnectToPairedDeviceAsync(SelectedDevice.Id);
                        _lastClassicDeviceId = SelectedDevice.Id;
                        _lastComPort = null;
                        AddLog("BILGI", "Classic doğrudan RFCOMM ile bağlandı (COM gerekmiyor).");
                    }
                    _activeTransport = DeviceTransport.ClassicSerial;
                    _lastTransport = DeviceTransport.ClassicSerial;
                    break;

                case DeviceTransport.Ble:
                    if (!SelectedDevice.BleAddress.HasValue)
                    {
                        throw new InvalidOperationException("BLE adresi bulunamadı.");
                    }

                    await _ble.ConnectAsync(SelectedDevice.BleAddress.Value);
                    _activeTransport = DeviceTransport.Ble;
                    _lastTransport = DeviceTransport.Ble;
                    _lastBleAddress = SelectedDevice.BleAddress;
                    break;

                case DeviceTransport.Simulator:
                    await _sim.ConnectAsync();
                    _activeTransport = DeviceTransport.Simulator;
                    _lastTransport = DeviceTransport.Simulator;
                    break;
            }

            SetStatus(ConnectionState.Connected);
            AddLog("BILGI", $"Bağlandı: {SelectedDevice.Name}");
        }
        catch (Exception ex)
        {
            SetStatus(ConnectionState.Error);
            OnError($"Bağlantı hatası: {ex.Message}");
        }
    }

    private async Task DisconnectAsync()
    {
        try
        {
            switch (_activeTransport)
            {
                case DeviceTransport.ClassicSerial:
                    await _classic.DisconnectAsync();
                    break;
                case DeviceTransport.Ble:
                    await _ble.DisconnectAsync();
                    break;
                case DeviceTransport.Simulator:
                    await _sim.DisconnectAsync();
                    break;
            }

            _activeTransport = null;
            SetStatus(ConnectionState.Disconnected);
            AddLog("BILGI", "Bağlantı kapatıldı.");
        }
        catch (Exception ex)
        {
            OnError($"Bağlantı kesme hatası: {ex.Message}");
        }
    }

    private async Task ReconnectAsync()
    {
        try
        {
            await DisconnectAsync();
            SetStatus(ConnectionState.Connecting);

            switch (_lastTransport)
            {
                case DeviceTransport.ClassicSerial:
                    if (!string.IsNullOrWhiteSpace(_lastComPort))
                    {
                        await _classic.ConnectAsync(_lastComPort, _lastBaudRate);
                        _activeTransport = DeviceTransport.ClassicSerial;
                    }
                    else if (!string.IsNullOrWhiteSpace(_lastClassicDeviceId))
                    {
                        await _classic.ConnectToPairedDeviceAsync(_lastClassicDeviceId);
                        _activeTransport = DeviceTransport.ClassicSerial;
                    }
                    break;
                case DeviceTransport.Ble:
                    if (_lastBleAddress.HasValue)
                    {
                        await _ble.ConnectAsync(_lastBleAddress.Value);
                        _activeTransport = DeviceTransport.Ble;
                    }
                    break;
                case DeviceTransport.Simulator:
                    await _sim.ConnectAsync();
                    _activeTransport = DeviceTransport.Simulator;
                    break;
                default:
                    AddLog("UYARI", "Yeniden bağlanmak için önceki bağlantı bulunamadı.");
                    SetStatus(ConnectionState.Disconnected);
                    return;
            }

            SetStatus(ConnectionState.Connected);
            AddLog("BILGI", "Yeniden bağlandı.");
        }
        catch (Exception ex)
        {
            SetStatus(ConnectionState.Error);
            OnError($"Yeniden bağlanma hatası: {ex.Message}");
        }
    }

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(CommandText))
        {
            return;
        }

        await SendRawAsync(CommandText.Trim());
    }

    private Task SendPresetAsync(string cmd)
    {
        return SendRawAsync(cmd);
    }

    private async Task SendRawAsync(string text)
    {
        try
        {
            var payload = BuildPayload(text);
            var sendPreview = SelectedCommandMode == "HEX"
                ? BitConverter.ToString(payload).Replace("-", " ")
                : Encoding.ASCII.GetString(payload).Replace("\r", "\\r").Replace("\n", "\\n");

            switch (_activeTransport)
            {
                case DeviceTransport.ClassicSerial:
                    await _classic.SendBytesAsync(payload);
                    break;
                case DeviceTransport.Ble:
                    await _ble.SendBytesAsync(payload);
                    break;
                case DeviceTransport.Simulator:
                    await _sim.SendBytesAsync(payload);
                    break;
                default:
                    AddLog("UYARI", "Aktif bağlantı yok.");
                    return;
            }

            AddTraffic("GIDEN", sendPreview);
            AddLog("GIDEN", $"[{SelectedCommandMode}/{SelectedLineEnding}] {sendPreview}");
        }
        catch (Exception ex)
        {
            OnError($"Gönderim hatası: {ex.Message}");
        }
    }

    private byte[] BuildPayload(string input)
    {
        byte[] body;
        if (SelectedCommandMode == "HEX")
        {
            body = ParseHex(input);
        }
        else
        {
            body = Encoding.ASCII.GetBytes(input);
        }

        var ending = SelectedLineEnding switch
        {
            "LF" => new byte[] { 0x0A },
            "CR" => new byte[] { 0x0D },
            "CRLF" => new byte[] { 0x0D, 0x0A },
            _ => Array.Empty<byte>()
        };

        if (ending.Length == 0)
        {
            return body;
        }

        var combined = new byte[body.Length + ending.Length];
        Buffer.BlockCopy(body, 0, combined, 0, body.Length);
        Buffer.BlockCopy(ending, 0, combined, body.Length, ending.Length);
        return combined;
    }

    private static byte[] ParseHex(string input)
    {
        var cleaned = input
            .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace(",", string.Empty)
            .Trim();

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            throw new InvalidOperationException("HEX veri boş olamaz.");
        }

        if (cleaned.Length % 2 != 0)
        {
            throw new InvalidOperationException("HEX uzunluğu çift olmalı. Örnek: AA01FF");
        }

        var bytes = new byte[cleaned.Length / 2];
        for (var i = 0; i < cleaned.Length; i += 2)
        {
            if (!byte.TryParse(cleaned.Substring(i, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
            {
                throw new InvalidOperationException($"Geçersiz HEX byte: {cleaned.Substring(i, 2)}");
            }

            bytes[i / 2] = b;
        }

        return bytes;
    }

    private async Task LoadBleServicesAsync()
    {
        try
        {
            BleServices.Clear();
            var services = await _ble.GetServicesAsync();
            foreach (var service in services)
            {
                BleServices.Add(service);
            }

            AddLog("BILGI", $"BLE servisleri yüklendi: {services.Count}");
        }
        catch (Exception ex)
        {
            OnError($"BLE servis yükleme hatası: {ex.Message}");
        }
    }

    private async Task LoadBleCharacteristicsAsync()
    {
        if (!SelectedBleService.HasValue)
        {
            AddLog("UYARI", "Önce bir BLE servisi seçin.");
            return;
        }

        try
        {
            BleCharacteristics.Clear();
            var chars = await _ble.GetCharacteristicsAsync(SelectedBleService.Value);
            foreach (var characteristic in chars)
            {
                BleCharacteristics.Add(characteristic);
            }

            AddLog("BILGI", $"BLE karakteristikleri yüklendi: {chars.Count}");
        }
        catch (Exception ex)
        {
            OnError($"BLE karakteristik yükleme hatası: {ex.Message}");
        }
    }

    private async Task ApplyBleSelectionAsync()
    {
        if (!SelectedBleService.HasValue || !SelectedBleCharacteristic.HasValue)
        {
            AddLog("UYARI", "BLE servis ve karakteristik seçin.");
            return;
        }

        try
        {
            await _ble.ConfigureIoAsync(SelectedBleService.Value, SelectedBleCharacteristic.Value);
            AddLog("BILGI", "BLE I/O karakteristiği yapılandırıldı.");
        }
        catch (Exception ex)
        {
            OnError($"BLE seçim uygulama hatası: {ex.Message}");
        }
    }

    private void OnData(string text)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            AddTraffic("GELEN", text);
            AddLog("GELEN", text);
        });
    }

    private void OnError(string message)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            SetStatus(ConnectionState.Error);
            AddLog("HATA", message);
        });
    }

    private void AddTraffic(string type, string message)
    {
        if (type != "GELEN" && type != "GIDEN")
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var cleaned = new string(message.Where(ch => !char.IsControl(ch) || ch == '\t' || ch == '\n' || ch == '\r').ToArray());
        var parts = cleaned
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (parts.Count == 0)
        {
            return;
        }

        foreach (var part in parts)
        {
            Traffic.Insert(0, new LogEntry
            {
                Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                Type = type,
                Message = part
            });
        }

        while (Traffic.Count > 1500)
        {
            Traffic.RemoveAt(Traffic.Count - 1);
        }
    }

    private void AddLog(string type, string message)
    {
        Logs.Insert(0, new LogEntry
        {
            Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
            Type = type,
            Message = message
        });
    }

    private void SetStatus(ConnectionState state)
    {
        _currentState = state;
        switch (state)
        {
            case ConnectionState.Disconnected:
                StatusText = L("Bağlı değil", "Disconnected");
                StatusBrush = Brushes.Gray;
                break;
            case ConnectionState.Connecting:
                StatusText = L("Bağlanıyor...", "Connecting...");
                StatusBrush = Brushes.DarkGoldenrod;
                break;
            case ConnectionState.Connected:
                StatusText = L("Bağlı", "Connected");
                StatusBrush = Brushes.SeaGreen;
                break;
            case ConnectionState.Error:
                StatusText = L("Hata", "Error");
                StatusBrush = Brushes.Firebrick;
                break;
        }
    }

    private void OpenBluetoothSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:bluetooth",
                UseShellExecute = true
            });
            AddLog("BILGI", L("Windows Bluetooth ayarları açıldı.", "Windows Bluetooth settings opened."));
        }
        catch (Exception ex)
        {
            OnError($"{L("Windows Bluetooth ayarları açılamadı", "Could not open Windows Bluetooth settings")}: {ex.Message}");
        }
    }

    private void ExportTrafficCsv()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"traffic_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var lines = new List<string> { "Timestamp,Type,Message" };
            foreach (var item in Traffic.Reverse())
            {
                var safeMessage = item.Message.Replace("\"", "\"\"");
                lines.Add($"\"{item.Timestamp}\",\"{item.Type}\",\"{safeMessage}\"");
            }

            File.WriteAllLines(dialog.FileName, lines);
            AddLog("BILGI", L("Canlı akış CSV dışa aktarıldı.", "Live traffic CSV exported."));
        }
        catch (Exception ex)
        {
            OnError($"{L("CSV dışa aktarma hatası", "CSV export error")}: {ex.Message}");
        }
    }

    private void ExportLogsTxt()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text (*.txt)|*.txt",
                FileName = $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var lines = Logs.Reverse().Select(x => $"[{x.Timestamp}] {x.Type}: {x.Message}").ToList();
            File.WriteAllLines(dialog.FileName, lines);
            AddLog("BILGI", L("Log TXT dışa aktarıldı.", "Log TXT exported."));
        }
        catch (Exception ex)
        {
            OnError($"{L("TXT dışa aktarma hatası", "TXT export error")}: {ex.Message}");
        }
    }

    private string L(string tr, string en)
    {
        return SelectedLanguage == "English" ? en : tr;
    }

    private void UpdateUiTexts()
    {
        AppWindowTitle = L("KozaBluetooth", "KozaBluetooth");
        ScanTitle = L("Cihaz Tarama", "Device Scan");
        SettingsTitle = L("Ayarlar", "Settings");
        CommandTitle = L("Komut Gönderme", "Send Command");
        LiveFlowTitle = L("Canlı Veri Akışı (Gelen/Giden/Hata)", "Live Data Flow (In/Out/Error)");
        LogTitle = L("Log ve Hata Kayıtları", "Logs and Errors");
        AboutTitle = L("Hakkımda", "About");

        ScanClassicText = L("Classic Tara", "Scan Classic");
        ScanBleText = L("BLE Tara", "Scan BLE");
        AddSimulatorText = L("Simülatör Ekle", "Add Simulator");
        OpenBtSettingsText = L("Bluetooth Ayarları", "Bluetooth Settings");

        ConnectText = L("Bağlan", "Connect");
        DisconnectText = L("Bağlantıyı Kes", "Disconnect");
        ReconnectText = L("Yeniden Bağlan", "Reconnect");
        SendText = L("Gönder", "Send");
        SendModeText = L("Gönderim Modu", "Send Mode");
        LineEndingText = L("Satır Sonu", "Line Ending");
        HexHintText = L("HEX örnek: AA 01 0D 0A", "HEX example: AA 01 0D 0A");

        ComPortText = "COM Port";
        BaudRateText = "Baud Rate";
        BleServiceText = L("BLE Servis", "BLE Service");
        BleCharacteristicText = L("BLE Karakteristik (Write/Notify)", "BLE Characteristic (Write/Notify)");
        LoadServicesText = L("Servisleri Yükle", "Load Services");
        LoadCharsText = L("Karakteristik Yükle", "Load Chars");
        ApplyBleText = L("BLE Seçimi Uygula", "Apply BLE Selection");
        LanguageLabelText = L("Dil", "Language");
        ExportFlowText = L("Akış CSV", "Flow CSV");
        ExportLogText = L("Log TXT", "Log TXT");

        RefreshAboutText();
    }

    private void RefreshAboutText()
    {
        AboutText = L(
            "KozaBluetooth\nWindows 10/11 için Classic Bluetooth (HC-05/HC-06) ve BLE terminal uygulaması.\nGeliştirici: Koza Akademi\n" + GitInfoText + "\nRepo: https://github.com/eekilinc/KozaBluetooh",
            "KozaBluetooth\nClassic Bluetooth (HC-05/HC-06) and BLE terminal app for Windows 10/11.\nDeveloper: Koza Akademi\n" + GitInfoText + "\nRepo: https://github.com/eekilinc/KozaBluetooh");
    }

    private void RemoveDevicesByTransport(DeviceTransport transport)
    {
        var toRemove = Devices.Where(d => d.Transport == transport).ToList();
        foreach (var device in toRemove)
        {
            Devices.Remove(device);
        }
    }
}
