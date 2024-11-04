using System.ComponentModel;
using System.Diagnostics;

namespace SystemMetricsApp.ViewModels;

public class CoreUsageViewModel : INotifyPropertyChanged
{
    private readonly PerformanceCounter _counter;
    private double _usage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CoreName { get; }
    
    public double Usage
    {
        get => _usage;
        private set
        {
            if (_usage != value)
            {
                _usage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Usage)));
            }
        }
    }

    public CoreUsageViewModel(int coreIndex)
    {
        CoreName = $"Core {coreIndex}";
        _counter = new PerformanceCounter("Processor", "% Processor Time", coreIndex == 0 ? "_Total" : $"{coreIndex - 1}");
    }

    public void Update()
    {
        Usage = _counter.NextValue();
    }
}
