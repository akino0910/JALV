using System.Windows;
using LogViewer.Core;
using LogViewer.Properties;

namespace LogViewer.Common
{
    public class GlobalHelper
    {
        public static string DisplayDateTimeFormat
        {
            get
            {
                var localizedFormat = Resources.GlobalHelper_DISPLAY_DATETIME_FORMAT;
                return string.IsNullOrWhiteSpace(localizedFormat) ? Constants.DisplayDatetimeFormat : localizedFormat;
            }
        }

        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                new System.Threading.ThreadStart(() => { }));
        }
    }
}