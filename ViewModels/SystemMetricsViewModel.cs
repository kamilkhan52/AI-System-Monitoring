using System;
using System.ComponentModel;  // For INotifyPropertyChanged
using System.Windows;  // For Visibility enum
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using SystemMetricsApp.Services;

namespace SystemMetricsApp.ViewModels;

public class SystemMetricsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private double _totalMemoryGB;
    public double TotalMemoryGB
    {
        get => _totalMemoryGB;
        set
        {
            if (_totalMemoryGB != value)
            {
                _totalMemoryGB = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalMemoryGB)));
            }
        }
    }

    private double _usedMemoryGB;
    public double UsedMemoryGB
    {
        get => _usedMemoryGB;
        set
        {
            if (_usedMemoryGB != value)
            {
                _usedMemoryGB = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsedMemoryGB)));
                // Update the text when used memory changes
                UpdateMemoryUsageText();
            }
        }
    }

    private double _memoryUsagePercentage;
    public double MemoryUsagePercentage
    {
        get => _memoryUsagePercentage;
        set
        {
            if (_memoryUsagePercentage != value)
            {
                _memoryUsagePercentage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MemoryUsagePercentage)));
            }
        }
    }

    private string _memoryUsageText = "Memory: Calculating...";
    public string MemoryUsageText
    {
        get => _memoryUsageText;
        private set
        {
            if (_memoryUsageText != value)
            {
                _memoryUsageText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MemoryUsageText)));
            }
        }
    }

    private void UpdateMemoryUsageText()
    {
        MemoryUsageText = $"{UsedMemoryGB:0.00} GB of {TotalMemoryGB:0.00} GB used";
    }

    public void UpdateMemoryUsage()
    {
        try
        {
            var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
            ulong totalMemoryBytes = computerInfo.TotalPhysicalMemory;
            ulong availableMemoryBytes = computerInfo.AvailablePhysicalMemory;

            TotalMemoryGB = totalMemoryBytes / (1024.0 * 1024 * 1024);
            double availableMemoryGB = availableMemoryBytes / (1024.0 * 1024 * 1024);
            UsedMemoryGB = TotalMemoryGB - availableMemoryGB;
            MemoryUsagePercentage = (UsedMemoryGB / TotalMemoryGB) * 100;
        }
        catch (Exception)
        {
            // Optionally log the exception
        }
    }

    // Add property
    private Visibility _memoryVisibility = Visibility.Visible;
    public Visibility MemoryVisibility
    {
        get => _memoryVisibility;
        set
        {
            if (_memoryVisibility != value)
            {
                _memoryVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MemoryVisibility)));
            }
        }
    }

    // Add these properties and methods to SystemMetricsViewModel
    private double _cpuUsagePercentage;
    public double CpuUsagePercentage
    {
        get => _cpuUsagePercentage;
        set
        {
            if (_cpuUsagePercentage != value)
            {
                _cpuUsagePercentage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CpuUsagePercentage)));
                UpdateCpuUsageText();
            }
        }
    }

    private string _cpuUsageText = "CPU: Calculating...";
    public string CpuUsageText
    {
        get => _cpuUsageText;
        private set
        {
            if (_cpuUsageText != value)
            {
                _cpuUsageText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CpuUsageText)));
            }
        }
    }

    private Visibility _cpuVisibility = Visibility.Visible;
    public Visibility CpuVisibility
    {
        get => _cpuVisibility;
        set
        {
            if (_cpuVisibility != value)
            {
                _cpuVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CpuVisibility)));
            }
        }
    }

    private PerformanceCounter? _cpuCounter;

    public void Initialize()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
    }

    public void UpdateCpuUsage()
    {
        if (_cpuCounter != null)
        {
            CpuUsagePercentage = _cpuCounter.NextValue();
        }
    }

    private void UpdateCpuUsageText()
    {
        CpuUsageText = $"CPU Usage: {CpuUsagePercentage:0.0}%";
    }

    // Add these properties
    private string _topCpuProcess = "Calculating...";
    public string TopCpuProcess
    {
        get => _topCpuProcess;
        set
        {
            if (_topCpuProcess != value)
            {
                _topCpuProcess = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopCpuProcess)));
            }
        }
    }

    private string _topMemoryProcess = "Calculating...";
    public string TopMemoryProcess
    {
        get => _topMemoryProcess;
        set
        {
            if (_topMemoryProcess != value)
            {
                _topMemoryProcess = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopMemoryProcess)));
            }
        }
    }

    // Add to class fields
    private ObservableCollection<ProcessInfoViewModel> _topProcesses = new();
    public ObservableCollection<ProcessInfoViewModel> TopProcesses
    {
        get => _topProcesses;
        private set
        {
            _topProcesses = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopProcesses)));
        }
    }

    // Add test method to populate with dummy data
    private Dictionary<string, List<(double Cpu, double Memory, DateTime Time)>> _processHistory = new();
    private readonly TimeSpan _historyDuration = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _sampleInterval = TimeSpan.FromSeconds(10);

    public void UpdateTopProcesses()
    {
        try
        {
            var now = DateTime.UtcNow;
            var processes = Process.GetProcesses();
            var currentProcessTimes = new Dictionary<int, (TimeSpan CpuTime, DateTime Timestamp)>();
            
            // Update process history
            foreach (var process in processes)
            {
                try
                {
                    var currentTime = now;
                    var currentCpuTime = process.TotalProcessorTime;
                    currentProcessTimes[process.Id] = (currentCpuTime, currentTime);

                    if (_previousProcessTimes.TryGetValue(process.Id, out var previous))
                    {
                        var cpuUsedMs = (currentCpuTime - previous.CpuTime).TotalMilliseconds;
                        var totalMsPassed = (currentTime - previous.Timestamp).TotalMilliseconds * Environment.ProcessorCount;
                        var cpuUsage = (cpuUsedMs / totalMsPassed) * 100;
                        var memoryMB = process.WorkingSet64 / (1024.0 * 1024.0);

                        if (!_processHistory.ContainsKey(process.ProcessName))
                        {
                            _processHistory[process.ProcessName] = new List<(double, double, DateTime)>();
                        }

                        _processHistory[process.ProcessName].Add((cpuUsage, memoryMB, now));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accessing process: {ex.Message}");
                }
            }

            // Clean up old history
            var cutoff = now - _historyDuration;
            foreach (var processName in _processHistory.Keys.ToList())
            {
                _processHistory[processName].RemoveAll(x => x.Time < cutoff);
                if (!_processHistory[processName].Any())
                {
                    _processHistory.Remove(processName);
                }
            }

            // Calculate averages and find top processes
            var processAverages = _processHistory
                .Select(kvp => new ProcessAverage
                {
                    ProcessName = kvp.Key,
                    AvgCpu = kvp.Value.Average(x => x.Cpu),
                    AvgMemory = kvp.Value.Average(x => x.Memory)
                })
                .ToList();

            var topCpu = processAverages
                .OrderByDescending(p => p.AvgCpu)
                .FirstOrDefault();

            var topMemory = processAverages
                .OrderByDescending(p => p.AvgMemory)
                .FirstOrDefault();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (topCpu != null && topCpu.AvgCpu >= 1.0)
                {
                    // Get current CPU usage for the top process
                    var currentCpuUsage = processes
                        .FirstOrDefault(p => p.ProcessName == topCpu.ProcessName)?
                        .TotalProcessorTime;
                    var previousCpu = _previousProcessTimes
                        .FirstOrDefault(p => processes.Any(proc => proc.ProcessName == topCpu.ProcessName && proc.Id == p.Key));
                    
                    double currentCpuPercent = 0;
                    if (currentCpuUsage.HasValue && previousCpu.Key != 0)
                    {
                        var cpuUsedMs = (currentCpuUsage.Value - previousCpu.Value.CpuTime).TotalMilliseconds;
                        var totalMsPassed = (now - previousCpu.Value.Timestamp).TotalMilliseconds * Environment.ProcessorCount;
                        currentCpuPercent = (cpuUsedMs / totalMsPassed) * 100;
                    }
                    
                    TopCpuProcess = $"CPU Eater: {topCpu.ProcessName} ({currentCpuPercent:F1}%)";
                }
                else
                {
                    TopCpuProcess = "CPU Eater: None!";
                }

                if (topMemory != null && topMemory.AvgMemory >= 1.0)
                {
                    // Get current memory usage for the top process
                    var currentMemoryMB = processes
                        .FirstOrDefault(p => p.ProcessName == topMemory.ProcessName)?
                        .WorkingSet64 / (1024.0 * 1024.0);
                    
                    TopMemoryProcess = $"Memory Eater: {topMemory.ProcessName} ({(currentMemoryMB ?? 0):F0} MB)";
                }
                else
                {
                    TopMemoryProcess = "Memory Eater: None!";
                }
            });

            _previousProcessTimes = currentProcessTimes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating process list: {ex.Message}");
        }
    }

    // Add dictionary for tracking CPU times
    private Dictionary<int, (TimeSpan CpuTime, DateTime Timestamp)> _previousProcessTimes = new();

    // Add this helper class for deduplication
    private class ProcessComparer : IEqualityComparer<ProcessAverage>
    {
        public bool Equals(ProcessAverage? x, ProcessAverage? y)
        {
            if (x == null || y == null) return false;
            return x.ProcessName == y.ProcessName;
        }

        public int GetHashCode(ProcessAverage obj)
        {
            return obj.ProcessName.GetHashCode();
        }
    }

    // Add this class inside SystemMetricsViewModel
    private class ProcessAverage
    {
        public string ProcessName { get; set; } = "";
        public double AvgCpu { get; set; }
        public double AvgMemory { get; set; }
    }

    public ObservableCollection<DiskUsageViewModel> DiskUsages { get; } = new();

    private DispatcherTimer dispatcherTimer;

    // Add this property
    private double _coreGridWidth = 304; // Default width for 4 columns
    public double CoreGridWidth
    {
        get => _coreGridWidth;
        set
        {
            if (_coreGridWidth != value)
            {
                _coreGridWidth = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CoreGridWidth)));
            }
        }
    }

    // Add this method to calculate width based on columns
    private void UpdateWindowWidth(int columns)
    {
        const int CORE_WIDTH = 66; // 60px + 6px margins
        const int WINDOW_PADDING = 40; // 20px on each side
        CoreGridWidth = (CORE_WIDTH * columns) + WINDOW_PADDING;
    }

    // Add these fields at the top of the class
    private readonly string _amduProfPath = @"C:\Program Files\AMD\AMDuProf\bin\AMDuProfPcm.exe";

    // Replace memory bandwidth properties with processor metrics
    private string _processorInfo = "Initializing...";
    public string ProcessorInfo
    {
        get => _processorInfo;
        set
        {
            if (_processorInfo != value)
            {
                _processorInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProcessorInfo)));
            }
        }
    }

    private double _l1ICacheMissRate;
    public double L1ICacheMissRate
    {
        get => _l1ICacheMissRate;
        set
        {
            if (_l1ICacheMissRate != value)
            {
                _l1ICacheMissRate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(L1ICacheMissRate)));
            }
        }
    }

    private double _l1DCacheMissRate;
    public double L1DCacheMissRate
    {
        get => _l1DCacheMissRate;
        set
        {
            if (_l1DCacheMissRate != value)
            {
                _l1DCacheMissRate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(L1DCacheMissRate)));
            }
        }
    }

    private double _l2CacheMissRate;
    public double L2CacheMissRate
    {
        get => _l2CacheMissRate;
        set
        {
            if (_l2CacheMissRate != value)
            {
                _l2CacheMissRate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(L2CacheMissRate)));
            }
        }
    }

    private double _l2HitRate;
    public double L2HitRate
    {
        get => _l2HitRate;
        set
        {
            if (_l2HitRate != value)
            {
                _l2HitRate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(L2HitRate)));
            }
        }
    }

    private string _cacheMetricsStatus = "Initializing...";
    public string CacheMetricsStatus
    {
        get => _cacheMetricsStatus;
        set
        {
            if (_cacheMetricsStatus != value)
            {
                _cacheMetricsStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CacheMetricsStatus)));
            }
        }
    }

    private ProcessorMetrics? _processorMetrics;

    // Add these visibility properties
    private Visibility _coreGridVisibility = Visibility.Visible;
    public Visibility CoreGridVisibility
    {
        get => _coreGridVisibility;
        set
        {
            if (_coreGridVisibility != value)
            {
                _coreGridVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CoreGridVisibility)));
            }
        }
    }

    private Visibility _diskUsageVisibility = Visibility.Collapsed;
    public Visibility DiskUsageVisibility
    {
        get => _diskUsageVisibility;
        set
        {
            if (_diskUsageVisibility != value)
            {
                _diskUsageVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiskUsageVisibility)));
            }
        }
    }

    private Visibility _processorMetricsVisibility = Visibility.Visible;
    public Visibility ProcessorMetricsVisibility
    {
        get => _processorMetricsVisibility;
        set
        {
            if (_processorMetricsVisibility != value)
            {
                _processorMetricsVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProcessorMetricsVisibility)));
            }
        }
    }

    private Visibility _l1CacheVisibility = Visibility.Visible;
    public Visibility L1CacheVisibility
    {
        get => _l1CacheVisibility;
        set
        {
            if (_l1CacheVisibility != value)
            {
                _l1CacheVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(L1CacheVisibility)));
            }
        }
    }

    private Visibility _l2CacheVisibility = Visibility.Visible;
    public Visibility L2CacheVisibility
    {
        get => _l2CacheVisibility;
        set
        {
            if (_l2CacheVisibility != value)
            {
                _l2CacheVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(L2CacheVisibility)));
            }
        }
    }

    public Visibility CCDInfoVisibility { get; set; } = Visibility.Visible;
    public Visibility MemoryChannelsVisibility { get; set; } = Visibility.Visible;

    // Add method to load settings
    private void LoadSettings()
    {
        try
        {
            string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            if (File.Exists(settingsPath))
            {
                string settingsJson = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(settingsJson);
                
                if (settings != null)
                {
                    CoreGridVisibility = settings.TryGetValue("ShowCpuCores", out var coreVal) && coreVal.GetBoolean() 
                        ? Visibility.Visible : Visibility.Collapsed;
                    
                    DiskUsageVisibility = settings.TryGetValue("ShowDiskUsage", out var diskVal) && diskVal.GetBoolean() 
                        ? Visibility.Visible : Visibility.Collapsed;
                    
                    ProcessorMetricsVisibility = settings.TryGetValue("ShowProcessorMetrics", out var procVal) && procVal.GetBoolean() 
                        ? Visibility.Visible : Visibility.Collapsed;
                    
                    L1CacheVisibility = settings.TryGetValue("ShowL1Cache", out var l1Val) && l1Val.GetBoolean() 
                        ? Visibility.Visible : Visibility.Collapsed;
                    
                    L2CacheVisibility = settings.TryGetValue("ShowL2Cache", out var l2Val) && l2Val.GetBoolean() 
                        ? Visibility.Visible : Visibility.Collapsed;

                    // Load polling interval
                    if (settings.TryGetValue("PollingInterval", out var intervalVal) && 
                        intervalVal.TryGetInt32(out int interval))
                    {
                        PollingInterval = interval;
                    }

                    _logger.Info($"Settings loaded - CoreGrid: {CoreGridVisibility}, DiskUsage: {DiskUsageVisibility}, ProcessorMetrics: {ProcessorMetricsVisibility}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading settings: {ex.Message}");
        }
    }

    // Add polling interval property
    private int _pollingInterval = 2; // Default 2 seconds
    public int PollingInterval
    {
        get => _pollingInterval;
        set
        {
            if (_pollingInterval != value)
            {
                _pollingInterval = value;
                UpdatePollingIntervals();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PollingInterval)));
            }
        }
    }

    private void UpdatePollingIntervals()
    {
        if (_processorMetrics != null)
        {
            _processorMetrics.UpdatePollingInterval(TimeSpan.FromSeconds(PollingInterval));
        }
        
        // Update main timer interval
        dispatcherTimer.Interval = TimeSpan.FromSeconds(PollingInterval);
    }

    private readonly LoggingService _logger = LoggingService.Instance;

    public SystemMetricsViewModel()
    {
        _logger.Info("==========================================");
        _logger.Info($"SystemMetricsViewModel initialized at: {DateTime.Now}");
        _logger.Info($"Checking AMD uProf installation at: {_amduProfPath}");
        _logger.Info($"Running as administrator: {IsAdministrator()}");
        _logger.Info($"AMD uProf exists: {File.Exists(_amduProfPath)}");

        dispatcherTimer = new DispatcherTimer();
        dispatcherTimer.Interval = TimeSpan.FromSeconds(_pollingInterval);
        dispatcherTimer.Tick += (s, e) =>
        {
            UpdateDiskUsages();
            UpdateMemoryUsage();
            UpdateCpuUsage();
            UpdateTopProcesses();
        };
        dispatcherTimer.Start();

        // Calculate initial width based on 4 columns
        UpdateWindowWidth(4);

        InitializeProcessorMetrics();

        LoadSettings();
    }

    private void InitializeProcessorMetrics()
    {
        _logger.Info("Initializing processor metrics...");
        if (IsAdministrator() && File.Exists(_amduProfPath))
        {
            _processorMetrics = new ProcessorMetrics(_amduProfPath);
            
            // Subscribe to metrics updates
            _processorMetrics.MetricsUpdated += metrics =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ProcessorInfo = metrics.ProcessorInfo;

                    // Update cache metrics
                    L1ICacheMissRate = metrics.L1ICacheMissRate;
                    L1DCacheMissRate = metrics.L1DCacheMissRate;
                    L2CacheMissRate = metrics.L2CacheMissRate;
                    L2HitRate = metrics.L2HitRate;
                    
                    CacheMetricsStatus = $"L1I Miss: {L1ICacheMissRate:F1}% | L1D Miss: {L1DCacheMissRate:F1}pti | " +
                                       $"L2 Hit: {L2HitRate:F1}pti";
                });
            };
            
            _logger.Info("Started processor metrics monitoring");
            CacheMetricsStatus = "Monitoring...";
        }
        else
        {
            _logger.Warning($"Processor metrics monitoring disabled: Admin: {IsAdministrator()}, AMD uProf exists: {File.Exists(_amduProfPath)}");
            CacheMetricsStatus = "Disabled (Run as Admin)";
        }
    }

    private bool IsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void UpdateDiskUsages()
    {
        try
        {
            var allDrives = DriveInfo.GetDrives();
            var diskUsages = new List<DiskUsageViewModel>();

            foreach (var drive in allDrives)
            {
                try
                {
                    if (!drive.IsReady)
                    {
                        continue;
                    }

                    long totalSize = drive.TotalSize;
                    if (totalSize <= 0)
                    {
                        continue;
                    }

                    long usedSpace = totalSize - drive.AvailableFreeSpace;
                    double usagePercentage = (double)usedSpace / totalSize * 100;

                    var diskUsage = new DiskUsageViewModel
                    {
                        DriveName = $"{drive.Name} ({drive.DriveType})",
                        TotalSize = totalSize,
                        UsedSpace = usedSpace,
                        UsagePercentage = usagePercentage,
                        UsageText = $"{FormatBytes(usedSpace)} of {FormatBytes(totalSize)} used"
                    };

                    diskUsages.Add(diskUsage);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error accessing drive {drive.Name}: {ex.Message}");
                }
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                DiskUsages.Clear();
                foreach (var diskUsage in diskUsages)
                {
                    DiskUsages.Add(diskUsage);
                }
            });
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error in UpdateDiskUsages: {ex.Message}");
        }
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
