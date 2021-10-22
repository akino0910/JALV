using System;

namespace LogViewer.Core.Domain
{
    [Serializable]
    public class LogItem
    {
        public int Id { get; set; }
        public string Path { get; set; }

        public DateTime TimeStamp { get; set; }

        //public string Delta { get; set; }
        public double? Delta { get; set; }
        public string Logger { get; set; }
        public string Thread { get; set; }
        public string Message { get; set; }
        public string MachineName { get; set; }
        public string RequestId { get; set; }
        public string App { get; set; }
        public string Throwable { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
        public string UserName { get; set; }
        public string HostName { get; set; }
        public string File { get; set; }
        public string Line { get; set; }
        public string Uncategorized { get; set; }

        /// <summary>
        /// LevelIndex
        /// </summary>
        public LevelIndex LevelIndex { get; set; }

        /// <summary>
        /// Level Property
        /// </summary>
        public string Level
        {
            get => _level;
            set
            {
                if (value != _level)
                {
                    _level = value;
                    AssignLevelIndex(_level);
                }
            }
        }

        #region Privates

        private string _level;

        private void AssignLevelIndex(string level)
        {
            var ul = !string.IsNullOrWhiteSpace(level) ? level.Trim().ToUpper() : string.Empty;
            switch (ul)
            {
                case "DEBUG":
                    LevelIndex = LevelIndex.Debug;
                    break;
                case "INFO":
                    LevelIndex = LevelIndex.Info;
                    break;
                case "WARN":
                    LevelIndex = LevelIndex.Warn;
                    break;
                case "ERROR":
                    LevelIndex = LevelIndex.Error;
                    break;
                case "FATAL":
                    LevelIndex = LevelIndex.Fatal;
                    break;
                default:
                    LevelIndex = LevelIndex.None;
                    break;
            }
        }

        #endregion
    }

    public enum LevelIndex
    {
        None = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Fatal = 5
    }
}