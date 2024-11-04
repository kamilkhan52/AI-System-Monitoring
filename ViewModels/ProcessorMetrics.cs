using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Threading;
using System.Text;
using SystemMetricsApp.Services;

namespace SystemMetricsApp.ViewModels
{
    public class ProcessorMetrics
    {
        // Processor Info
        public string ProcessorName { get; private set; } = string.Empty;
        public int CoreCount { get; private set; }
        public int ThreadCount { get; private set; }
        public double BaseFrequencyMHz { get; private set; }

        // Add new AMD-specific info
        public int CCDCount { get; private set; }
        public int CCXCount { get; private set; }
        public string CCDTopology { get; private set; } = string.Empty;
        public bool SMTEnabled { get; private set; }
        public string MemoryInfo { get; private set; } = string.Empty;

        // Cache Metrics
        public double L1ICacheMissRate { get; private set; }    // IC Fetch Miss Ratio
        public double L1DCacheMissRate { get; private set; }    // L1 DC Miss (pti)
        public double L2CacheMissRate { get; private set; }     // L2 Miss (pti)
        public double L2HitRate { get; private set; }           // L2 Hit (pti)
        public DateTime Timestamp { get; private set; }

        // Add this property
        public string ProcessorInfo { get; private set; } = string.Empty;

        private readonly DispatcherTimer _timer;
        private readonly string _uprofPath;
        private readonly string _outputDirectory;

        private readonly LoggingService _logger = LoggingService.Instance;

        // Event to notify UI of updates
        public event Action<ProcessorMetrics>? MetricsUpdated;

        public ProcessorMetrics(string uprofPath)
        {
            _uprofPath = uprofPath;
            _outputDirectory = AppDomain.CurrentDomain.BaseDirectory;

            _logger.Info("Initializing ProcessorMetrics");

            // Collect processor info once at startup
            CollectProcessorInfo();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            _logger.Info("DispatcherTimer initialized and started");
        }

        private void CollectProcessorInfo()
        {
            try
            {
                string tempFile = Path.Combine(_outputDirectory, $"processor_info_{Guid.NewGuid()}.csv");
                var startInfo = new ProcessStartInfo
                {
                    FileName = _uprofPath,
                    Arguments = $"-m dc -a -A system -d 1 -o \"{tempFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas"
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit();
                    if (File.Exists(tempFile))
                    {
                        ParseProcessorInfoOnly(tempFile);
                        try { File.Delete(tempFile); } catch { }
                    }
                }

                UpdateProcessorInfo();
                _logger.Info($"Initial processor info collected: {ProcessorInfo}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error collecting processor info: {ex.Message}");
            }
        }

        private void ParseProcessorInfoOnly(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                bool processorInfoSection = false;

                foreach (var line in lines)
                {
                    if (line.StartsWith("Processor Name:"))
                    {
                        processorInfoSection = true;
                        ProcessorName = line.Split(',')[1].Trim();
                    }
                    else if (processorInfoSection)
                    {
                        ParseProcessorInfo(line);

                        // Exit once we've passed the processor info section
                        if (line.StartsWith("Profile Time:"))
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error parsing processor info: {ex.Message}");
            }
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            _logger.Trace("Timer ticked");
            try
            {
                await UpdateCacheMetricsOnly();
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception in Timer_Tick: {ex.Message}");
            }
        }

        private async Task UpdateCacheMetricsOnly()
        {
            _logger.Trace("UpdateCacheMetricsOnly called");
            try
            {
                string tempFile = Path.Combine(_outputDirectory, $"cache_metrics_{Guid.NewGuid()}.csv");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = _uprofPath,
                    Arguments = $"-m dc,cache_miss,l1,l2 -a -A system -d 1 -o \"{tempFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas",
                    WorkingDirectory = _outputDirectory
                };

                _logger.Trace($"Starting uProf process with arguments: {startInfo.Arguments}");

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    _logger.Trace($"uProf process exited with code: {process.ExitCode}");
                    
                    await Task.Delay(500);
                    
                    if (File.Exists(tempFile))
                    {
                        ParseMetricsData(tempFile);
                    }
                    else
                    {
                        _logger.Warning("Metrics file was not created");
                    }
                    
                    try { File.Delete(tempFile); } catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in UpdateCacheMetricsOnly: {ex.Message}");
            }
        }

        private void ParseMetricsData(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                bool metricsSection = false;

                foreach (var line in lines)
                {
                    // Only look for metrics section now, skip processor info
                    if (line.StartsWith("System (Aggregated)"))
                    {
                        metricsSection = true;
                        continue;
                    }

                    if (metricsSection && !line.StartsWith("IC Fetch"))
                    {
                        ParseMetricsLine(line);
                        break; // Exit after parsing metrics
                    }
                }

                // Only invoke the event, no need to update processor info again
                MetricsUpdated?.Invoke(this);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error parsing metrics data: {ex.Message}");
            }
        }

        private void ParseProcessorInfo(string line)
        {
            var parts = line.Split(',');
            if (parts.Length < 2) return;

            var value = parts[1].Trim();
            if (line.StartsWith("Number of Cores :"))
                CoreCount = int.Parse(value);
            else if (line.StartsWith("Number of Threads :"))
                ThreadCount = int.Parse(value);
            else if (line.StartsWith("Core P0 state frequency (MHz):"))
                BaseFrequencyMHz = double.Parse(value);
            else if (line.StartsWith("Number of Core Complexes(CCX) :"))
                CCXCount = int.Parse(value);
            else if (line.StartsWith("Number of Sockets :"))
                CCDCount = int.Parse(value);
            else if (line.StartsWith("SMT Enabled in HW:"))
                SMTEnabled = bool.Parse(value);
        }

        private void ParseMetricsLine(string line)
        {
            var values = line.Split(',');
            if (values.Length >= 28)
            {
                L1ICacheMissRate = double.Parse(values[0]);
                L1DCacheMissRate = double.Parse(values[24]);
                L2CacheMissRate = double.Parse(values[16]);
                L2HitRate = double.Parse(values[20]);
                
                Timestamp = DateTime.Now;
                
                _logger.Info($"Cache metrics updated - L1I Miss: {L1ICacheMissRate:F1}% | " +
                            $"L1D Miss: {L1DCacheMissRate:F1}pti | L2 Hit: {L2HitRate:F1}pti");
            }
        }

        private void UpdateProcessorInfo()
        {
            var info = new StringBuilder();
            info.AppendLine($"{ProcessorName}");
            info.AppendLine($"Cores: {CoreCount} | Threads: {ThreadCount}");
            info.AppendLine($"Base Frequency: {BaseFrequencyMHz / 1000.0:F1} GHz");
            info.AppendLine($"SMT: {(SMTEnabled ? "Enabled" : "Disabled")}");
            info.AppendLine($"CCDs: {CCDCount} | CCXs: {CCXCount}");

            ProcessorInfo = info.ToString().TrimEnd();
            _logger.Info($"Processor info updated: {ProcessorInfo}");
        }

        public void UpdatePollingInterval(TimeSpan interval)
        {
            _timer.Interval = interval;
            _logger.Info($"Updated polling interval to {interval.TotalSeconds} seconds");
        }
    }
} 