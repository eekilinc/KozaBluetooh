using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Data;
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
    private string _bleWriteModeText = "BLE Yazma Modu";
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
    private string _clearFlowText = "Akışı Temizle";
    private string _clearLogsText = "Logu Temizle";
    private string _hexHintText = "HEX örnek: AA 01 0D 0A";
    private string _autoReconnectText = "Otomatik Yeniden Bağlan";
    private string _healthTelemetryText = "Health Telemetry";
    private string _profilesTitleText = "Profil";
    private string _saveProfileText = "Profili Kaydet";
    private string _applyProfileText = "Profili Uygula";
    private string _deleteProfileText = "Profili Sil";
    private string _macroTitleText = "Makro";
    private string _runMacroText = "Makroyu Çalıştır";
    private string _saveMacroText = "Makroyu Kaydet";
    private string _deleteMacroText = "Makroyu Sil";
    private string _schedulerTitleText = "Zamanlı Gönderim";
    private string _startSchedulerText = "Başlat";
    private string _stopSchedulerText = "Durdur";
    private string _trafficFilterText = "Akış Filtresi";

    private string? _selectedComPort;
    private int _selectedBaudRate = 9600;
    private Guid? _selectedBleService;
    private Guid? _selectedBleCharacteristic;
    private string _selectedCommandMode = "ASCII";
    private string _selectedLineEnding = "CRLF";
    private BleWriteMode _selectedBleWriteMode = BleWriteMode.Auto;
    private bool _autoReconnectEnabled = true;
    private bool _enableHealthTelemetry = true;
    private bool _isReconnectInProgress;
    private bool _isManualDisconnect;
    private CancellationTokenSource? _schedulerCts;

    private int _txCount;
    private int _rxCount;
    private int _errorCount;
    private int _reconnectCount;

    private string _profileNameInput = "Varsayilan";
    private ConnectionProfile? _selectedProfile;
    private string _macroNameInput = "Yeni Makro";
    private string _macroCommandsInput = "STATUS";
    private int _macroDelayMs = 300;
    private MacroCommand? _selectedMacro;
    private string _schedulerCommand = "STATUS";
    private int _schedulerIntervalMs = 1000;
    private bool _isSchedulerRunning;
    private string _trafficFilterKeyword = string.Empty;
    private string _trafficFilterType = "ALL";

    private DeviceTransport? _activeTransport;
    private DeviceTransport? _lastTransport;
    private ConnectionState _currentState = ConnectionState.Disconnected;
    private string? _lastComPort;
    private int _lastBaudRate = 9600;
    private string? _lastClassicDeviceId;
    private ulong? _lastBleAddress;

    public ObservableCollection<DiscoveredDevice> Devices { get; } = new();
    public ObservableCollection<LogEntry> Traffic { get; } = new();
    public ICollectionView TrafficView { get; }
    public ObservableCollection<LogEntry> Logs { get; } = new();
    public ObservableCollection<string> LanguageOptions { get; } = new() { "Türkçe", "English" };
    public ObservableCollection<string> CommandModeOptions { get; } = new() { "ASCII", "HEX" };
    public ObservableCollection<string> LineEndingOptions { get; } = new() { "NONE", "LF", "CR", "CRLF" };
    public ObservableCollection<BleWriteMode> BleWriteModeOptions { get; } = new() { BleWriteMode.Auto, BleWriteMode.WriteWithoutResponse, BleWriteMode.WriteWithResponse };
    public ObservableCollection<ConnectionProfile> Profiles { get; } = new();
    public ObservableCollection<MacroCommand> Macros { get; } = new();
    public ObservableCollection<string> TrafficFilterTypes { get; } = new() { "ALL", "GELEN", "GIDEN" };

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

    public BleWriteMode SelectedBleWriteMode
    {
        get => _selectedBleWriteMode;
        set => SetProperty(ref _selectedBleWriteMode, value);
    }

    public bool AutoReconnectEnabled
    {
        get => _autoReconnectEnabled;
        set => SetProperty(ref _autoReconnectEnabled, value);
    }

    public bool EnableHealthTelemetry
    {
        get => _enableHealthTelemetry;
        set
        {
            if (SetProperty(ref _enableHealthTelemetry, value))
            {
                RefreshAboutText();
            }
        }
    }

    public string ProfileNameInput
    {
        get => _profileNameInput;
        set => SetProperty(ref _profileNameInput, value);
    }

    public ConnectionProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => SetProperty(ref _selectedProfile, value);
    }

    public string MacroNameInput
    {
        get => _macroNameInput;
        set => SetProperty(ref _macroNameInput, value);
    }

    public string MacroCommandsInput
    {
        get => _macroCommandsInput;
        set => SetProperty(ref _macroCommandsInput, value);
    }

    public int MacroDelayMs
    {
        get => _macroDelayMs;
        set => SetProperty(ref _macroDelayMs, value);
    }

    public MacroCommand? SelectedMacro
    {
        get => _selectedMacro;
        set => SetProperty(ref _selectedMacro, value);
    }

    public string SchedulerCommand
    {
        get => _schedulerCommand;
        set => SetProperty(ref _schedulerCommand, value);
    }

    public int SchedulerIntervalMs
    {
        get => _schedulerIntervalMs;
        set => SetProperty(ref _schedulerIntervalMs, value);
    }

    public bool IsSchedulerRunning
    {
        get => _isSchedulerRunning;
        set => SetProperty(ref _isSchedulerRunning, value);
    }

    public string TrafficFilterKeyword
    {
        get => _trafficFilterKeyword;
        set
        {
            if (SetProperty(ref _trafficFilterKeyword, value))
            {
                TrafficView.Refresh();
            }
        }
    }

    public string TrafficFilterType
    {
        get => _trafficFilterType;
        set
        {
            if (SetProperty(ref _trafficFilterType, value))
            {
                TrafficView.Refresh();
            }
        }
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
    public string BleWriteModeText { get => _bleWriteModeText; set => SetProperty(ref _bleWriteModeText, value); }
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
    public string ClearFlowText { get => _clearFlowText; set => SetProperty(ref _clearFlowText, value); }
    public string ClearLogsText { get => _clearLogsText; set => SetProperty(ref _clearLogsText, value); }
    public string HexHintText { get => _hexHintText; set => SetProperty(ref _hexHintText, value); }
    public string AutoReconnectText { get => _autoReconnectText; set => SetProperty(ref _autoReconnectText, value); }
    public string HealthTelemetryText { get => _healthTelemetryText; set => SetProperty(ref _healthTelemetryText, value); }
    public string ProfilesTitleText { get => _profilesTitleText; set => SetProperty(ref _profilesTitleText, value); }
    public string SaveProfileText { get => _saveProfileText; set => SetProperty(ref _saveProfileText, value); }
    public string ApplyProfileText { get => _applyProfileText; set => SetProperty(ref _applyProfileText, value); }
    public string DeleteProfileText { get => _deleteProfileText; set => SetProperty(ref _deleteProfileText, value); }
    public string MacroTitleText { get => _macroTitleText; set => SetProperty(ref _macroTitleText, value); }
    public string RunMacroText { get => _runMacroText; set => SetProperty(ref _runMacroText, value); }
    public string SaveMacroText { get => _saveMacroText; set => SetProperty(ref _saveMacroText, value); }
    public string DeleteMacroText { get => _deleteMacroText; set => SetProperty(ref _deleteMacroText, value); }
    public string SchedulerTitleText { get => _schedulerTitleText; set => SetProperty(ref _schedulerTitleText, value); }
    public string StartSchedulerText { get => _startSchedulerText; set => SetProperty(ref _startSchedulerText, value); }
    public string StopSchedulerText { get => _stopSchedulerText; set => SetProperty(ref _stopSchedulerText, value); }
    public string TrafficFilterText { get => _trafficFilterText; set => SetProperty(ref _trafficFilterText, value); }

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
    public RelayCommand ClearTrafficCommand { get; }
    public RelayCommand ClearLogsCommand { get; }
    public RelayCommand SaveProfileCommand { get; }
    public RelayCommand ApplyProfileCommand { get; }
    public RelayCommand DeleteProfileCommand { get; }
    public AsyncRelayCommand RunMacroCommand { get; }
    public RelayCommand SaveMacroCommand { get; }
    public RelayCommand DeleteMacroCommand { get; }
    public RelayCommand StartSchedulerCommand { get; }
    public RelayCommand StopSchedulerCommand { get; }
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

        TrafficView = CollectionViewSource.GetDefaultView(Traffic);
        TrafficView.Filter = TrafficFilter;

        _classic.DataReceived += OnData;
        _classic.ErrorOccurred += OnError;
        _classic.ConnectionLost += OnConnectionLost;
        _ble.DataReceived += OnData;
        _ble.ErrorOccurred += OnError;
        _ble.ConnectionLost += OnConnectionLost;
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
        ClearTrafficCommand = new RelayCommand(ClearTraffic);
        ClearLogsCommand = new RelayCommand(ClearLogs);
        SaveProfileCommand = new RelayCommand(SaveProfile);
        ApplyProfileCommand = new RelayCommand(ApplyProfile);
        DeleteProfileCommand = new RelayCommand(DeleteProfile);
        RunMacroCommand = new AsyncRelayCommand(RunMacroAsync);
        SaveMacroCommand = new RelayCommand(SaveMacro);
        DeleteMacroCommand = new RelayCommand(DeleteMacro);
        StartSchedulerCommand = new RelayCommand(StartScheduler);
        StopSchedulerCommand = new RelayCommand(StopScheduler);
        OpenBluetoothSettingsCommand = new RelayCommand(OpenBluetoothSettings);

        LoadProfiles();
        LoadMacros();
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
        _isManualDisconnect = true;
        try
        {
            StopScheduler();
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
        finally
        {
            _isManualDisconnect = false;
        }
    }

    private async Task ReconnectAsync()
    {
        if (_isReconnectInProgress)
        {
            return;
        }

        _isReconnectInProgress = true;
        try
        {
            await DisconnectAsync();
            SetStatus(ConnectionState.Connecting);

            var delays = new[] { 0, 1000, 2500, 5000 };
            Exception? lastError = null;

            for (var attempt = 0; attempt < delays.Length; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        AddLog("BILGI", $"Yeniden bağlanma denemesi {attempt + 1}/{delays.Length}...");
                    }

                    if (delays[attempt] > 0)
                    {
                        await Task.Delay(delays[attempt]);
                    }

                    await ConnectLastKnownAsync();
                    _reconnectCount++;
                    SetStatus(ConnectionState.Connected);
                    AddLog("BILGI", "Yeniden bağlandı.");
                    RefreshAboutText();
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    AddLog("UYARI", $"Yeniden bağlanma denemesi başarısız: {ex.Message}");
                }
            }

            SetStatus(ConnectionState.Error);
            if (lastError != null)
            {
                OnError($"Yeniden bağlanma hatası: {lastError.Message}");
            }
        }
        catch (Exception ex)
        {
            SetStatus(ConnectionState.Error);
            OnError($"Yeniden bağlanma hatası: {ex.Message}");
        }
        finally
        {
            _isReconnectInProgress = false;
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
            _txCount++;
            RefreshAboutText();
        }
        catch (Exception ex)
        {
            OnError($"Gönderim hatası: {ex.Message}");
        }
    }

    private byte[] BuildPayload(string input) => CommandPayloadBuilder.Build(input, SelectedCommandMode, SelectedLineEnding);

    private async Task ConnectLastKnownAsync()
    {
        switch (_lastTransport)
        {
            case DeviceTransport.ClassicSerial:
                if (!string.IsNullOrWhiteSpace(_lastComPort))
                {
                    await _classic.ConnectAsync(_lastComPort, _lastBaudRate);
                    _activeTransport = DeviceTransport.ClassicSerial;
                    return;
                }

                if (!string.IsNullOrWhiteSpace(_lastClassicDeviceId))
                {
                    await _classic.ConnectToPairedDeviceAsync(_lastClassicDeviceId);
                    _activeTransport = DeviceTransport.ClassicSerial;
                    return;
                }

                break;

            case DeviceTransport.Ble:
                if (_lastBleAddress.HasValue)
                {
                    await _ble.ConnectAsync(_lastBleAddress.Value);
                    _activeTransport = DeviceTransport.Ble;
                    return;
                }

                break;

            case DeviceTransport.Simulator:
                await _sim.ConnectAsync();
                _activeTransport = DeviceTransport.Simulator;
                return;
        }

        throw new InvalidOperationException("Yeniden bağlanmak için önceki bağlantı bilgisi eksik.");
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
            await _ble.ConfigureIoAsync(SelectedBleService.Value, SelectedBleCharacteristic.Value, SelectedBleWriteMode);
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
            _rxCount++;
            RefreshAboutText();
        });
    }

    private void OnError(string message)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            _errorCount++;
            SetStatus(ConnectionState.Error);
            AddLog("HATA", message);
            RefreshAboutText();

            if (AutoReconnectEnabled && !_isManualDisconnect && !_isReconnectInProgress && _activeTransport != null && _lastTransport != null)
            {
                _ = ReconnectAsync();
            }
        });
    }

    private void OnConnectionLost(string message)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            _activeTransport = null;
            StopScheduler();
            StatusText = L("Bağlantı koptu", "Connection lost");
            StatusBrush = Brushes.Gray;
            AddLog("HATA", message);
            _errorCount++;
            RefreshAboutText();

            if (AutoReconnectEnabled && !_isManualDisconnect && !_isReconnectInProgress && _lastTransport != null)
            {
                _ = ReconnectAsync();
            }
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

    private void ClearTraffic()
    {
        Traffic.Clear();
        AddLog("BILGI", L("Canlı akış temizlendi.", "Live flow cleared."));
    }

    private void ClearLogs()
    {
        Logs.Clear();
        AddLog("BILGI", L("Log kayıtları temizlendi.", "Logs cleared."));
    }

    private bool TrafficFilter(object obj)
    {
        if (obj is not LogEntry entry)
        {
            return false;
        }

        var typeOk = TrafficFilterType == "ALL" || string.Equals(entry.Type, TrafficFilterType, StringComparison.OrdinalIgnoreCase);
        var keywordOk = string.IsNullOrWhiteSpace(TrafficFilterKeyword)
            || entry.Message.Contains(TrafficFilterKeyword, StringComparison.OrdinalIgnoreCase)
            || entry.Type.Contains(TrafficFilterKeyword, StringComparison.OrdinalIgnoreCase);

        return typeOk && keywordOk;
    }

    private static string ProfilesPath => Path.Combine(AppStateDirectory, "profiles.json");
    private static string MacrosPath => Path.Combine(AppStateDirectory, "macros.json");
    private static string AppStateDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KozaBluetooth");

    private void LoadProfiles()
    {
        try
        {
            Directory.CreateDirectory(AppStateDirectory);
            if (!File.Exists(ProfilesPath))
            {
                return;
            }

            var json = File.ReadAllText(ProfilesPath);
            var loaded = JsonSerializer.Deserialize<List<ConnectionProfile>>(json) ?? new List<ConnectionProfile>();
            Profiles.Clear();
            foreach (var item in loaded)
            {
                Profiles.Add(item);
            }
        }
        catch (Exception ex)
        {
            AddLog("UYARI", $"Profil yükleme hatası: {ex.Message}");
        }
    }

    private void SaveProfilesToDisk()
    {
        Directory.CreateDirectory(AppStateDirectory);
        var json = JsonSerializer.Serialize(Profiles.ToList(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ProfilesPath, json);
    }

    private void SaveProfile()
    {
        try
        {
            var name = string.IsNullOrWhiteSpace(ProfileNameInput) ? "Varsayilan" : ProfileNameInput.Trim();
            var existing = Profiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            var profile = existing ?? new ConnectionProfile { Name = name };

            profile.ComPort = SelectedComPort;
            profile.BaudRate = SelectedBaudRate;
            profile.CommandMode = SelectedCommandMode;
            profile.LineEnding = SelectedLineEnding;
            profile.BleService = SelectedBleService;
            profile.BleCharacteristic = SelectedBleCharacteristic;
            profile.BleWriteMode = SelectedBleWriteMode;

            if (existing == null)
            {
                Profiles.Add(profile);
            }

            SaveProfilesToDisk();
            AddLog("BILGI", $"Profil kaydedildi: {name}");
        }
        catch (Exception ex)
        {
            OnError($"Profil kaydetme hatası: {ex.Message}");
        }
    }

    private void ApplyProfile()
    {
        if (SelectedProfile == null)
        {
            AddLog("UYARI", "Uygulamak için profil seçin.");
            return;
        }

        SelectedComPort = SelectedProfile.ComPort;
        SelectedBaudRate = SelectedProfile.BaudRate;
        SelectedCommandMode = SelectedProfile.CommandMode;
        SelectedLineEnding = SelectedProfile.LineEnding;
        SelectedBleService = SelectedProfile.BleService;
        SelectedBleCharacteristic = SelectedProfile.BleCharacteristic;
        SelectedBleWriteMode = SelectedProfile.BleWriteMode;

        AddLog("BILGI", $"Profil uygulandı: {SelectedProfile.Name}");
    }

    private void DeleteProfile()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        var name = SelectedProfile.Name;
        Profiles.Remove(SelectedProfile);
        SelectedProfile = null;
        SaveProfilesToDisk();
        AddLog("BILGI", $"Profil silindi: {name}");
    }

    private void LoadMacros()
    {
        try
        {
            Directory.CreateDirectory(AppStateDirectory);
            if (!File.Exists(MacrosPath))
            {
                return;
            }

            var json = File.ReadAllText(MacrosPath);
            var loaded = JsonSerializer.Deserialize<List<MacroCommand>>(json) ?? new List<MacroCommand>();
            Macros.Clear();
            foreach (var item in loaded)
            {
                Macros.Add(item);
            }
        }
        catch (Exception ex)
        {
            AddLog("UYARI", $"Makro yükleme hatası: {ex.Message}");
        }
    }

    private void SaveMacrosToDisk()
    {
        Directory.CreateDirectory(AppStateDirectory);
        var json = JsonSerializer.Serialize(Macros.ToList(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(MacrosPath, json);
    }

    private void SaveMacro()
    {
        try
        {
            var name = string.IsNullOrWhiteSpace(MacroNameInput) ? "Yeni Makro" : MacroNameInput.Trim();
            var existing = Macros.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
            var macro = existing ?? new MacroCommand { Name = name };
            macro.CommandsText = MacroCommandsInput;
            macro.DelayMs = Math.Max(0, MacroDelayMs);

            if (existing == null)
            {
                Macros.Add(macro);
            }

            SaveMacrosToDisk();
            AddLog("BILGI", $"Makro kaydedildi: {name}");
        }
        catch (Exception ex)
        {
            OnError($"Makro kaydetme hatası: {ex.Message}");
        }
    }

    private void DeleteMacro()
    {
        if (SelectedMacro == null)
        {
            return;
        }

        var name = SelectedMacro.Name;
        Macros.Remove(SelectedMacro);
        SelectedMacro = null;
        SaveMacrosToDisk();
        AddLog("BILGI", $"Makro silindi: {name}");
    }

    private async Task RunMacroAsync()
    {
        var macro = SelectedMacro ?? new MacroCommand
        {
            Name = string.IsNullOrWhiteSpace(MacroNameInput) ? "Ad-hoc" : MacroNameInput,
            CommandsText = MacroCommandsInput,
            DelayMs = Math.Max(0, MacroDelayMs)
        };

        if (string.IsNullOrWhiteSpace(macro.CommandsText))
        {
            AddLog("UYARI", "Makro komutları boş.");
            return;
        }

        var commands = macro.CommandsText
            .Split(new[] { "\r\n", "\n", ";" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        AddLog("BILGI", $"Makro başladı: {macro.Name} ({commands.Count} komut)");
        foreach (var cmd in commands)
        {
            await SendRawAsync(cmd);
            if (macro.DelayMs > 0)
            {
                await Task.Delay(macro.DelayMs);
            }
        }

        AddLog("BILGI", $"Makro tamamlandı: {macro.Name}");
    }

    private void StartScheduler()
    {
        if (IsSchedulerRunning)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SchedulerCommand))
        {
            AddLog("UYARI", "Zamanlı komut boş olamaz.");
            return;
        }

        var interval = Math.Max(200, SchedulerIntervalMs);
        _schedulerCts = new CancellationTokenSource();
        IsSchedulerRunning = true;
        AddLog("BILGI", $"Zamanlı gönderim başladı ({interval} ms).");

        _ = Task.Run(async () =>
        {
            while (!_schedulerCts.IsCancellationRequested)
            {
                await App.Current.Dispatcher.InvokeAsync(async () => await SendRawAsync(SchedulerCommand));
                try
                {
                    await Task.Delay(interval, _schedulerCts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        });
    }

    private void StopScheduler()
    {
        if (!IsSchedulerRunning)
        {
            return;
        }

        _schedulerCts?.Cancel();
        _schedulerCts?.Dispose();
        _schedulerCts = null;
        IsSchedulerRunning = false;
        AddLog("BILGI", "Zamanlı gönderim durduruldu.");
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
        BleWriteModeText = L("BLE Yazma Modu", "BLE Write Mode");
        HexHintText = L("HEX örnek: AA 01 0D 0A", "HEX example: AA 01 0D 0A");
        AutoReconnectText = L("Otomatik Yeniden Bağlan", "Auto Reconnect");
        HealthTelemetryText = L("Sağlık Telemetrisi", "Health Telemetry");
        ProfilesTitleText = L("Profiller", "Profiles");
        SaveProfileText = L("Profili Kaydet", "Save Profile");
        ApplyProfileText = L("Profili Uygula", "Apply Profile");
        DeleteProfileText = L("Profili Sil", "Delete Profile");
        MacroTitleText = L("Makrolar", "Macros");
        RunMacroText = L("Makroyu Çalıştır", "Run Macro");
        SaveMacroText = L("Makroyu Kaydet", "Save Macro");
        DeleteMacroText = L("Makroyu Sil", "Delete Macro");
        SchedulerTitleText = L("Zamanlı Gönderim", "Scheduled Send");
        StartSchedulerText = L("Başlat", "Start");
        StopSchedulerText = L("Durdur", "Stop");
        TrafficFilterText = L("Akış Filtresi", "Flow Filter");

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
        ClearFlowText = L("Akışı Temizle", "Clear Flow");
        ClearLogsText = L("Logu Temizle", "Clear Logs");

        RefreshAboutText();
    }

    private void RefreshAboutText()
    {
        var telemetryLine = EnableHealthTelemetry
            ? $"\nTelemetri: TX={_txCount}, RX={_rxCount}, HATA={_errorCount}, RECONNECT={_reconnectCount}"
            : string.Empty;

        AboutText = L(
            "KozaBluetooth\nWindows 10/11 için Classic Bluetooth (HC-05/HC-06) ve BLE terminal uygulaması.\nGeliştirici: Koza Akademi\n" + GitInfoText + "\nRepo: https://github.com/eekilinc/KozaBluetooh" + telemetryLine,
            "KozaBluetooth\nClassic Bluetooth (HC-05/HC-06) and BLE terminal app for Windows 10/11.\nDeveloper: Koza Akademi\n" + GitInfoText + "\nRepo: https://github.com/eekilinc/KozaBluetooh" + telemetryLine);
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
