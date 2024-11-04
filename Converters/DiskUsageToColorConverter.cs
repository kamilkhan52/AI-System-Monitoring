using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SystemMetricsApp.Converters
{
    public class DiskUsageToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                if (percentage < 70)
                    return new SolidColorBrush(Colors.Green);
                else if (percentage < 90)
                    return new SolidColorBrush(Colors.Yellow);
                else
                    return new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
