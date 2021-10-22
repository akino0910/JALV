using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LogViewer.Common.Converters
{
    /// <summary>
    /// Convert bool to visibility
    /// false -> Visibility.Visible
    /// true or null -> Visibility.Collapsed
    /// </summary>
    public class BoolToOppositeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null == value)
                return Visibility.Collapsed;

            var oppositeValue = !(bool)value;

            if (oppositeValue)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}