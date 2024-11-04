using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using MessageBox = System.Windows.MessageBox;

namespace SystemMetricsApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application  // Explicitly specify System.Windows.Application
{
    public App()
    {
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"An unhandled exception occurred: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            File.AppendAllText("debug.log", $"Unhandled exception: {args.ExceptionObject}\n");
        };

        base.OnStartup(e);
    }
}

