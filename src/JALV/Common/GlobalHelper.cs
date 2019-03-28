using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using JALV.Core;
using JALV.Core.Domain;
using JALV.Core.Providers;
using JALV.Properties;

namespace JALV.Common
{
    public class GlobalHelper
    {
        public static string DisplayDateTimeFormat
        {
            get
            {
                string localizedFormat = Properties.Resources.GlobalHelper_DISPLAY_DATETIME_FORMAT;
                return String.IsNullOrWhiteSpace(localizedFormat) ? Constants.DISPLAY_DATETIME_FORMAT : localizedFormat;
            }
        }

        public static void DoEvents()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                                                 new System.Threading.ThreadStart(() => { }));
        }
    }
}