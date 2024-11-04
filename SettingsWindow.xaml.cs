using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using SystemMetricsApp.ViewModels;
using SystemMetricsApp.Services;

namespace SystemMetricsApp;

public partial class SettingsWindow : Window
{
    private readonly SystemMetricsViewModel _viewModel;

    public SettingsWindow(SystemMetricsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        // Initialize polling interval
        int currentInterval = _viewModel.PollingInterval;
        switch (currentInterval)
        {
            case 1: PollingIntervalComboBox.SelectedIndex = 0; break;
            case 2: PollingIntervalComboBox.SelectedIndex = 1; break;
            case 5: PollingIntervalComboBox.SelectedIndex = 2; break;
            case 10: PollingIntervalComboBox.SelectedIndex = 3; break;
        }

        // Initialize checkboxes from view model
        ShowCpuUsageCheckbox.IsChecked = _viewModel.CpuVisibility == Visibility.Visible;
        ShowCpuCoresCheckbox.IsChecked = _viewModel.CoreGridVisibility == Visibility.Visible;
        ShowMemoryUsageCheckbox.IsChecked = _viewModel.MemoryVisibility == Visibility.Visible;
        ShowDiskUsageCheckbox.IsChecked = _viewModel.DiskUsageVisibility == Visibility.Visible;
        ShowProcessorMetricsCheckbox.IsChecked = _viewModel.ProcessorMetricsVisibility == Visibility.Visible;
        
        // Initialize processor metrics sub-options
        ShowL1CacheCheckbox.IsChecked = _viewModel.L1CacheVisibility == Visibility.Visible;
        ShowL2CacheCheckbox.IsChecked = _viewModel.L2CacheVisibility == Visibility.Visible;
        ShowCCDInfoCheckbox.IsChecked = _viewModel.CCDInfoVisibility == Visibility.Visible;
        ShowMemoryChannelsCheckbox.IsChecked = _viewModel.MemoryChannelsVisibility == Visibility.Visible;

        TopMostCheckbox.IsChecked = System.Windows.Application.Current.MainWindow.Topmost;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Save polling interval
        int interval = PollingIntervalComboBox.SelectedIndex switch
        {
            0 => 1,
            1 => 2,
            2 => 5,
            3 => 10,
            _ => 2
        };
        _viewModel.PollingInterval = interval;

        // Add polling interval to settings
        var settings = new Dictionary<string, object>
        {
            { "ShowCpuUsage", ShowCpuUsageCheckbox.IsChecked == true },
            { "ShowCpuCores", ShowCpuCoresCheckbox.IsChecked == true },
            { "ShowMemoryUsage", ShowMemoryUsageCheckbox.IsChecked == true },
            { "ShowDiskUsage", ShowDiskUsageCheckbox.IsChecked == true },
            { "ShowProcessorMetrics", ShowProcessorMetricsCheckbox.IsChecked == true },
            { "ShowL1Cache", ShowL1CacheCheckbox.IsChecked == true },
            { "ShowL2Cache", ShowL2CacheCheckbox.IsChecked == true },
            { "ShowCCDInfo", ShowCCDInfoCheckbox.IsChecked == true },
            { "ShowMemoryChannels", ShowMemoryChannelsCheckbox.IsChecked == true },
            { "AlwaysOnTop", TopMostCheckbox.IsChecked == true },
            { "ClickThrough", ClickThroughCheckbox.IsChecked == true },
            { "PollingInterval", interval }
        };

        // Update view model visibility settings
        _viewModel.CpuVisibility = ShowCpuUsageCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        _viewModel.CoreGridVisibility = ShowCpuCoresCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        _viewModel.MemoryVisibility = ShowMemoryUsageCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        _viewModel.DiskUsageVisibility = ShowDiskUsageCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        _viewModel.ProcessorMetricsVisibility = ShowProcessorMetricsCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        
        // Update processor metrics sub-options
        _viewModel.L1CacheVisibility = ShowL1CacheCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        _viewModel.L2CacheVisibility = ShowL2CacheCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        _viewModel.CCDInfoVisibility = ShowCCDInfoCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        _viewModel.MemoryChannelsVisibility = ShowMemoryChannelsCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        // Update window settings
        System.Windows.Application.Current.MainWindow.Topmost = TopMostCheckbox.IsChecked == true;

        // Save settings
        SaveSettings();

        Close();
    }

    private void SaveSettings()
    {
        var settings = new Dictionary<string, object>
        {
            { "ShowCpuUsage", ShowCpuUsageCheckbox.IsChecked == true },
            { "ShowCpuCores", ShowCpuCoresCheckbox.IsChecked == true },
            { "ShowMemoryUsage", ShowMemoryUsageCheckbox.IsChecked == true },
            { "ShowDiskUsage", ShowDiskUsageCheckbox.IsChecked == true },
            { "ShowProcessorMetrics", ShowProcessorMetricsCheckbox.IsChecked == true },
            { "ShowL1Cache", ShowL1CacheCheckbox.IsChecked == true },
            { "ShowL2Cache", ShowL2CacheCheckbox.IsChecked == true },
            { "ShowCCDInfo", ShowCCDInfoCheckbox.IsChecked == true },
            { "ShowMemoryChannels", ShowMemoryChannelsCheckbox.IsChecked == true },
            { "AlwaysOnTop", TopMostCheckbox.IsChecked == true },
            { "ClickThrough", ClickThroughCheckbox.IsChecked == true },
            { "PollingInterval", _viewModel.PollingInterval }
        };

        // Add to SaveSettings()
        settings["EnableLogging"] = EnableLoggingCheckbox.IsChecked == true;
        settings["LogLevel"] = LogLevelComboBox.SelectedIndex;

        // Apply logging settings immediately
        var logger = LoggingService.Instance;
        logger.SetEnabled(EnableLoggingCheckbox.IsChecked == true);
        logger.SetMinimumLevel((LoggingService.LogLevel)LogLevelComboBox.SelectedIndex);

        string settingsJson = JsonSerializer.Serialize(settings);
        File.WriteAllText("settings.json", settingsJson);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }
}
