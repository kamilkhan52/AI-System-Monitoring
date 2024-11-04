using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SystemMetricsApp.Converters
{
    public class CacheMetricsToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double rate)
            {
                // For miss rates, higher is worse (red)
                // For hit rates, higher is better (green)
                bool isHitRate = parameter?.ToString() == "hit";
                
                if (isHitRate)
                {
                    if (rate >= 90) return new SolidColorBrush(Colors.LightGreen);
                    if (rate >= 70) return new SolidColorBrush(Colors.YellowGreen);
                    if (rate >= 50) return new SolidColorBrush(Colors.Yellow);
                    return new SolidColorBrush(Colors.Orange);
                }
                else
                {
                    if (rate >= 50) return new SolidColorBrush(Colors.Red);
                    if (rate >= 30) return new SolidColorBrush(Colors.Orange);
                    if (rate >= 10) return new SolidColorBrush(Colors.Yellow);
                    return new SolidColorBrush(Colors.LightGreen);
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 