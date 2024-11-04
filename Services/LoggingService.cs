using System;
using System.IO;
using System.Diagnostics;

namespace SystemMetricsApp.Services
{
    public class LoggingService
    {
        private static LoggingService? _instance;
        private readonly string _logPath;
        private bool _isEnabled = true;
        private LogLevel _minimumLevel = LogLevel.Trace;
        private static readonly object _lockObject = new object();

        public enum LogLevel
        {
            Trace,
            Info,
            Warning,
            Error
        }

        private LoggingService()
        {
            _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
            
            // Rotate logs if file gets too large (> 10MB)
            if (File.Exists(_logPath) && new FileInfo(_logPath).Length > 10_000_000)
            {
                try
                {
                    File.Move(_logPath, _logPath + ".old", true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to rotate log file: {ex.Message}");
                }
            }
        }

        public static LoggingService Instance => _instance ??= new LoggingService();

        public void SetEnabled(bool enabled) => _isEnabled = enabled;
        public void SetMinimumLevel(LogLevel level) => _minimumLevel = level;

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (!_isEnabled || level < _minimumLevel) return;

            lock (_lockObject)
            {
                try
                {
                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{level}|{message}\n";
                    File.AppendAllText(_logPath, logMessage);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to write to log: {ex.Message}");
                }
            }
        }

        public void Trace(string message) => Log(message, LogLevel.Trace);
        public void Info(string message) => Log(message, LogLevel.Info);
        public void Warning(string message) => Log(message, LogLevel.Warning);
        public void Error(string message) => Log(message, LogLevel.Error);
    }
} 