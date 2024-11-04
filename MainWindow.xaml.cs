using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Timers;
using System.Windows.Threading;
using SystemMetricsApp.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;  // For UniformGrid
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SystemMetricsApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DispatcherTimer dispatcherTimer;
    private bool showCpuUsage = true;
    private bool showCpuCores = true;
    private bool showMemoryUsage = true;
    private bool showDiskUsage = true;
    private ObservableCollection<CoreUsageViewModel> cores;
    private SystemMetricsViewModel systemMetrics;

    public MainWindow()
    {
        InitializeComponent();

        // Print processor information
        int processorCount = Environment.ProcessorCount;
        Console.WriteLine($"Number of logical processors: {processorCount}");

        // Initialize cores collection (skip total CPU in grid)
        cores = new ObservableCollection<CoreUsageViewModel>();
        for (int i = 0; i < processorCount; i++)
        {
            cores.Add(new CoreUsageViewModel(i));
            Console.WriteLine($"Added Core {i}");
        }
        CoresGrid.ItemsSource = cores;
        InitializeGridLayout();

        // Initialize system metrics
        systemMetrics = new SystemMetricsViewModel();
        systemMetrics.Initialize();  // Initialize CPU counter
        DataContext = systemMetrics;

        // Setup timer
        dispatcherTimer = new DispatcherTimer();
        dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
        dispatcherTimer.Tick += DispatcherTimer_Tick;
        dispatcherTimer.Start();
    }

    private void InitializeGridLayout()
    {
        int totalCores = cores.Count;
        (int columns, int rows) = GetOptimalGridDimensions(totalCores);

        // Get the UniformGrid from the visual tree after it's loaded
        this.Loaded += (s, e) =>
        {
            if (CoresGrid.ItemsPanel.LoadContent() is UniformGrid uniformGrid)
            {
                uniformGrid.Columns = columns;
                // Rows will be calculated automatically
            }
        };
    }

    private (int columns, int rows) GetOptimalGridDimensions(int totalItems)
    {
        // If it's a perfect square, that's our best option
        int sqrt = (int)Math.Sqrt(totalItems);
        if (sqrt * sqrt == totalItems)
        {
            return (sqrt, sqrt);
        }

        // Find factors of totalItems
        var factors = GetFactors(totalItems);
        
        // If we have no factors (prime number), use the ceiling sqrt approach
        if (factors.Count == 0)
        {
            int columns = (int)Math.Ceiling(Math.Sqrt(totalItems));
            int rows = (int)Math.Ceiling((double)totalItems / columns);
            return (columns, rows);
        }

        // Find the factor pair that's closest to a square
        int bestColumns = factors[0];
        int bestRows = totalItems / factors[0];
        double bestRatio = Math.Max((double)bestColumns / bestRows, (double)bestRows / bestColumns);

        foreach (int factor in factors)
        {
            int otherFactor = totalItems / factor;
            double ratio = Math.Max((double)factor / otherFactor, (double)otherFactor / factor);
            
            if (ratio < bestRatio)
            {
                bestRatio = ratio;
                bestColumns = factor;
                bestRows = otherFactor;
            }
        }

        // Ensure wider than tall if not square
        if (bestColumns < bestRows)
        {
            (bestColumns, bestRows) = (bestRows, bestColumns);
        }

        return (bestColumns, bestRows);
    }

    private List<int> GetFactors(int number)
    {
        var factors = new List<int>();
        int sqrt = (int)Math.Sqrt(number);
        
        for (int i = 2; i <= sqrt; i++)
        {
            if (number % i == 0)
            {
                factors.Add(i);
                if (i != number / i) // Avoid adding the same factor twice for perfect squares
                {
                    factors.Add(number / i);
                }
            }
        }
        
        factors.Sort();
        return factors;
    }

    private void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
        // Update CPU metrics
        systemMetrics.UpdateCpuUsage();

        // Update all cores
        foreach (var core in cores)
        {
            core.Update();
        }

        // Update memory metrics
        systemMetrics.UpdateMemoryUsage();

        // Add test process updates
        systemMetrics.UpdateTopProcesses();
    }

    private float GetTotalMemoryInMBytes()
    {
        var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
        ulong totalMemory = computerInfo.TotalPhysicalMemory;
        return (float)(totalMemory / (1024 * 1024));
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = (SystemMetricsViewModel)DataContext;  // Get the ViewModel from DataContext
        var settingsWindow = new SettingsWindow(viewModel);
        settingsWindow.ShowDialog();
    }

    private void UpdateMetricsVisibility()
    {
        systemMetrics.CpuVisibility = showCpuUsage ? Visibility.Visible : Visibility.Collapsed;
        systemMetrics.MemoryVisibility = showMemoryUsage ? Visibility.Visible : Visibility.Collapsed;
    }

    // Add exit method
    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();  // Explicitly use System.Windows.Application
    }

    // Add these properties
    public bool ShowCpuUsage
    {
        get => showCpuUsage;
        set
        {
            showCpuUsage = value;
            CpuUsagePanel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool ShowCpuCores
    {
        get => showCpuCores;
        set
        {
            showCpuCores = value;
            CoresGrid.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool ShowMemoryUsage
    {
        get => showMemoryUsage;
        set
        {
            showMemoryUsage = value;
            MemoryUsagePanel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool ShowDiskUsage
    {
        get => showDiskUsage;
        set
        {
            showDiskUsage = value;
            DiskUsagePanel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool IsClickThrough { get; private set; }

    public void SetClickThrough(bool enabled)
    {
        IsClickThrough = enabled;  // Set the property
        var hwnd = new WindowInteropHelper(this).Handle;
        var extendedStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
        
        if (enabled)
        {
            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, extendedStyle | Win32.WS_EX_TRANSPARENT);
        }
        else
        {
            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, extendedStyle & ~Win32.WS_EX_TRANSPARENT);
        }
    }

    private void AIButton_Click(object sender, RoutedEventArgs e)
    {
        var aiAssistantWindow = new AIAssistantWindow((SystemMetricsViewModel)DataContext);
        aiAssistantWindow.Show();
    }
}
