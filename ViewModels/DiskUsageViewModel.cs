using System.ComponentModel;

namespace SystemMetricsApp.ViewModels
{
    public class DiskUsageViewModel : INotifyPropertyChanged
    {
        private string _driveName = string.Empty;
        public string DriveName
        {
            get => _driveName;
            set { _driveName = value; OnPropertyChanged(nameof(DriveName)); }
        }

        private long _totalSize;
        public long TotalSize
        {
            get => _totalSize;
            set { _totalSize = value; OnPropertyChanged(nameof(TotalSize)); }
        }

        private long _usedSpace;
        public long UsedSpace
        {
            get => _usedSpace;
            set { _usedSpace = value; OnPropertyChanged(nameof(UsedSpace)); }
        }

        private double _usagePercentage;
        public double UsagePercentage
        {
            get => _usagePercentage;
            set { _usagePercentage = value; OnPropertyChanged(nameof(UsagePercentage)); }
        }

        private string _usageText = string.Empty;
        public string UsageText
        {
            get => _usageText;
            set { _usageText = value; OnPropertyChanged(nameof(UsageText)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
