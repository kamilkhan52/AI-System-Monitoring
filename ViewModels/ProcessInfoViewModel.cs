using System.ComponentModel;

namespace SystemMetricsApp.ViewModels;

public class ProcessInfoViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _processName = "";
    public string ProcessName
    {
        get => _processName;
        set
        {
            if (_processName != value)
            {
                _processName = value;
                OnPropertyChanged(nameof(ProcessName));
            }
        }
    }

    private double _cpuUsage;
    public double CpuUsage
    {
        get => _cpuUsage;
        set
        {
            if (_cpuUsage != value)
            {
                _cpuUsage = value;
                OnPropertyChanged(nameof(CpuUsage));
            }
        }
    }

    private double _memoryUsageMB;
    public double MemoryUsageMB
    {
        get => _memoryUsageMB;
        set
        {
            if (_memoryUsageMB != value)
            {
                _memoryUsageMB = value;
                OnPropertyChanged(nameof(MemoryUsageMB));
            }
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
