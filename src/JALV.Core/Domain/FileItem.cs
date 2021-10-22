using System;

namespace JALV.Core.Domain
{
    [Serializable]
    public class FileItem : BindableObject
    {
        /// <summary>
        /// Checked Property
        /// </summary>
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    RaisePropertyChanged(PropChecked);
                }
            }
        }

        private bool _checked;
        public static string PropChecked = "Checked";

        /// <summary>
        /// FileName Property
        /// </summary>
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                RaisePropertyChanged(PropFileName);
            }
        }

        private string _fileName;
        public static string PropFileName = "FileName";

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

        public FileItem(string fileName, string path)
        {
            Checked = false;
            FileName = fileName;
            Path = path;
        }
    }
}