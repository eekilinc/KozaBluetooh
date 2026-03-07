using System.Windows;
using BluetoothDeskApp.ViewModels;

namespace BluetoothDeskApp;

public partial class AboutWindow : Window
{
    public AboutWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
