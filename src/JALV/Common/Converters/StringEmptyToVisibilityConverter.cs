using System;
using System.Windows;
using System.Windows.Data;

namespace JALV.Common.Converters
{
    /// <summary>
    /// If empty string then collapsed
    /// </summary>
    public class StringEmptyToVisibilityConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null == value)
                return Visibility.Collapsed;

            if (String.IsNullOrEmpty((string)value))
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
