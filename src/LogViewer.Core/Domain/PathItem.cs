using System;

namespace LogViewer.Core.Domain
{
    [Serializable]
    public class PathItem : BindableObject
    {
        /// <summary>
        /// Name Property
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                RaisePropertyChanged(PropName);
            }
        }

        private string _name;
        public static string PropName = "Name";

        /// <summary>
        /// Path Property
        /// </summary>
        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                RaisePropertyChanged(PropPath);
            }
        }

        private string _path;
        public static string PropPath = "Path";

        public PathItem()
        {
            _name = string.Empty;
            _path = string.Empty;
        }

        public PathItem(string name, string path)
        {
            _name = name;
            _path = path;
        }
    }
}