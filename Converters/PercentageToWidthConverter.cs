using System;
using System.Globalization;
using System.Windows.Data;

namespace SystemMetricsApp.Converters
{
    public class PercentageToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                // Get the container width from parameter or use default
                double containerWidth = parameter != null ? System.Convert.ToDouble(parameter) : 100;
                return (percentage / 100.0) * containerWidth;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
