using System.Windows;

namespace BluetoothDeskApp;

public partial class GuideWindow : Window
{
    public GuideWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
