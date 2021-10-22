using System;
using System.IO;

namespace JALV.Core
{
    public static class Constants
    {
        public const string DisplayDatetimeFormat = "yyyy-MM-dd, HH:mm:ss.fff";

        public const string LayoutLog4J = "http://jakarta.apache.org/log4j";

        public const int DefaultRefreshInterval = 30;

        public static string FoldersFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JALVFolders.xml");
    }
}