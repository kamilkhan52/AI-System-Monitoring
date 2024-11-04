using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;  // Explicitly use WPF's media namespace

namespace SystemMetricsApp.Converters;

public class CpuUsageToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double usage)
        {
            // Green (low usage) to Red (high usage)
            byte red = (byte)(usage * 2.55);
            byte green = (byte)(255 - (usage * 2.55));
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, red, green, 0));  // Solid color for progress bar
        }
        return new SolidColorBrush(System.Windows.Media.Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
