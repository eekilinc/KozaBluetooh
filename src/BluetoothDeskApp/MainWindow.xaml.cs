using System.Windows;
using System.Windows.Media;
using BluetoothDeskApp.Services;
using BluetoothDeskApp.ViewModels;

namespace BluetoothDeskApp;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel(
            new ClassicBluetoothService(),
            new BleBluetoothService(),
            new SimulatorService(),
            new GitInfoService());

        DataContext = _vm;

        if (TryFindResource("AppIcon") is ImageSource appIcon)
        {
            Icon = appIcon;
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow(_vm)
        {
            Owner = this
        };
        about.ShowDialog();
    }

    private void OnGuideClick(object sender, RoutedEventArgs e)
    {
        var guide = new GuideWindow
        {
            Owner = this
        };
        guide.ShowDialog();
    }

    private void OnFullscreenToggleClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.MenuItem item)
        {
            return;
        }

        if (item.IsChecked)
        {
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
        else
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
    }
}
