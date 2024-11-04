using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Threading;

namespace SystemMetricsApp.ViewModels
{
    public class MemoryBandwidthMetrics
    {
        public double TotalBandwidth { get; set; }
        public double ReadBandwidth { get; set; }
        public double WriteBandwidth { get; set; }
        public DateTime Timestamp { get; set; }

        private readonly DispatcherTimer _timer;
        private readonly string _uprofPath;
        private readonly string _metricsFilePath;
        private readonly string _outputDirectory;

        public event Action<double, double, double>? BandwidthUpdated;

        public MemoryBandwidthMetrics(string uprofPath)
        {
            _uprofPath = uprofPath;

            _outputDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _metricsFilePath = Path.Combine(_outputDirectory, "memory_metrics.csv");

            LogToFile($"Metrics file path: {_metricsFilePath}");

            LogToFile("Initializing MemoryBandwidthMetrics");

            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
                LogToFile($"Created output directory: {_outputDirectory}");
            }

            LogToFile($"uProf path: {_uprofPath}");
            LogToFile($"Metrics file path: {_metricsFilePath}");
            LogToFile($"Output directory: {_outputDirectory}");

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            LogToFile("DispatcherTimer initialized and started");
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            LogToFile("Timer ticked");
            try
            {
                await UpdateMemoryBandwidth();
            }
            catch (Exception ex)
            {
                LogToFile($"Exception in Timer_Tick: {ex.Message}");
            }
        }

        private async Task UpdateMemoryBandwidth()
        {
            LogToFile("UpdateMemoryBandwidth called");
            try
            {
                string tempFile = Path.Combine(_outputDirectory, $"memory_metrics_{Guid.NewGuid()}.csv");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = _uprofPath,
                    Arguments = $"-m dc -a -A system -d 2 -t 100 -o \"{tempFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas",
                    WorkingDirectory = _outputDirectory
                };

                LogToFile($"Starting uProf process with arguments: {startInfo.Arguments}");

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    
                    if (!string.IsNullOrEmpty(output)) LogToFile($"Process output: {output}");
                    if (!string.IsNullOrEmpty(error)) LogToFile($"Process error: {error}");

                    await process.WaitForExitAsync();
                    LogToFile($"uProf process exited with code: {process.ExitCode}");
                    
                    await Task.Delay(500);
                    
                    if (File.Exists(tempFile))
                    {
                        LogToFile($"CSV file size: {new FileInfo(tempFile).Length} bytes");
                        ParseMemoryBandwidthData(tempFile);
                    }
                    else
                    {
                        LogToFile("CSV file was not created");
                    }
                    
                    try { File.Delete(tempFile); } catch { }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error in UpdateMemoryBandwidth: {ex.Message}");
            }
        }

        private void ParseMemoryBandwidthData(string filePath)
        {
            LogToFile("Parsing memory bandwidth data");
            try
            {
                var lines = File.ReadAllLines(filePath);
                LogToFile($"Read {lines.Length} lines from CSV");

                bool dataSection = false;
                foreach (var line in lines)
                {
                    LogToFile($"Processing line: {line}");
                    
                    if (line.Contains("System (Aggregated)"))
                    {
                        dataSection = true;
                        continue;
                    }

                    if (dataSection && !string.IsNullOrWhiteSpace(line) && !line.StartsWith("All DC Fills"))
                    {
                        var values = line.Split(',');
                        if (values.Length >= 7)
                        {
                            if (double.TryParse(values[3], out double localMemoryFills) &&
                                double.TryParse(values[5], out double remoteMemoryFills))
                            {
                                ReadBandwidth = localMemoryFills;
                                WriteBandwidth = remoteMemoryFills;
                                TotalBandwidth = localMemoryFills + remoteMemoryFills;

                                Timestamp = DateTime.Now;
                                LogToFile($"Updated bandwidths - Local Memory: {localMemoryFills}, Remote Memory: {remoteMemoryFills}, Total: {TotalBandwidth}");
                                
                                BandwidthUpdated?.Invoke(TotalBandwidth, ReadBandwidth, WriteBandwidth);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error parsing memory bandwidth data: {ex.Message}");
            }
        }

        private void LogToFile(string message)
        {
            try
            {
                string logFilePath = Path.Combine(_outputDirectory, "debug.log");
                File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write to log: {ex.Message}");
            }
        }
    }
}
