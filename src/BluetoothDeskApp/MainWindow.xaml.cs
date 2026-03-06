using System.Windows;
using BluetoothDeskApp.Services;
using BluetoothDeskApp.ViewModels;

namespace BluetoothDeskApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var vm = new MainViewModel(
            new ClassicBluetoothService(),
            new BleBluetoothService(),
            new SimulatorService(),
            new GitInfoService());

        DataContext = vm;
    }
}
