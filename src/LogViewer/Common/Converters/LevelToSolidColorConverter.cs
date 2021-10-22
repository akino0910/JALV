using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LogViewer.Common.Converters
{
    public class LevelToSolidColorConverter
        : IValueConverter
    {
        private readonly SolidColorBrush _debugColor =
            Application.Current.FindResource("DebugLevelColor") as SolidColorBrush;

        private readonly SolidColorBrush _infoColor =
            Application.Current.FindResource("InfoLevelColor") as SolidColorBrush;

        private readonly SolidColorBrush _warnColor =
            Application.Current.FindResource("WarnLevelColor") as SolidColorBrush;

        private readonly SolidColorBrush _errorColor =
            Application.Current.FindResource("ErrorLevelColor") as SolidColorBrush;

        private readonly SolidColorBrush _fatalColor =
            Application.Current.FindResource("FatalLevelColor") as SolidColorBrush;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null == value)
                return Brushes.Transparent;

            var levelIndex = (int)value;
            switch (levelIndex)
            {
                case 1:
                    return _debugColor ?? Brushes.Transparent;
                case 2:
                    return _infoColor ?? Brushes.Transparent;
                case 3:
                    return _warnColor ?? Brushes.Transparent;
                case 4:
                    return _errorColor ?? Brushes.Transparent;
                case 5:
                    return _fatalColor ?? Brushes.Transparent;
                default:
                    return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}