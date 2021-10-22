using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Shell;
using System.Windows.Threading;
using JALV.Common;
using JALV.Common.Interfaces;
using JALV.Core;
using JALV.Core.Domain;
using JALV.Properties;

namespace JALV.ViewModel
{
    public class MainWindowVm
        : BindableObject
    {
        public MainWindowVm(IWinSimple win)
        {
            _callingWin = win;

            CommandExit = new CommandRelay(CommandExitExecute, p => true);
            CommandOpenFile = new CommandRelay(CommandOpenFileExecute, CommandOpenFileCanExecute);
            CommandSelectFolder = new CommandRelay(CommandSelectFolderExecute, CommandSelectFolderCanExecute);
            CommandSaveFolder = new CommandRelay(CommandSaveFolderExecute, CommandSaveFolderCanExecute);
            CommandRefresh = new CommandRelay(CommandRefreshExecute, CommandRefreshCanExecute);
            CommandRefreshFiles = new CommandRelay(CommandRefreshFilesExecute, CommandRefreshFilesCanExecute);
            CommandClear = new CommandRelay(CommandClearExecute, CommandClearCanExecute);
            CommandDelete = new CommandRelay(CommandDeleteExecute, CommandDeleteCanExecute);
            CommandOpenSelectedFolder =
                new CommandRelay(CommandOpenSelectedFolderExecute, CommandOpenSelectedFolderCanExecute);
            CommandSelectAllFiles = new CommandRelay(CommandSelectAllFilesExecute, CommandSelectAllFilesCanExecute);
            CommandIncreaseInterval = new CommandRelay(CommandIncreaseIntervalExecute, p => true);
            CommandDecreaseInterval = new CommandRelay(CommandDecreaseIntervalExecute, p => true);
            CommandAbout = new CommandRelay(CommandAboutExecute, p => true);

            FileList = new ObservableCollection<FileItem>();
            Items = new ObservableCollection<LogItem>();
            LoadFolderList();

            SelectedFile = null;
            IsFileSelectionEnabled = false;
            IsLoading = false;

            _selectAll = true;
            _selectDebug = _selectInfo = _selectWarn = _selectError = _selectFatal = false;
            _showLevelDebug = _showLevelInfo = _showLevelWarn = _showLevelError = _showLevelFatal = true;

            _bkLoader = new BackgroundWorker();
            _bkLoader.WorkerSupportsCancellation = true;
            _bkLoader.DoWork += BkLoaderRun;
            _bkLoader.RunWorkerCompleted += BkLoaderCompleted;

            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += dispatcherTimer_Tick;

            AutoRefreshInterval = Constants.DefaultRefreshInterval;
            IsAutoRefreshEnabled = false;

            RefreshWindowTitle();
        }

        protected override void OnDispose()
        {
            if (_dispatcherTimer != null)
            {
                _dispatcherTimer.Stop();
                _dispatcherTimer.Tick -= dispatcherTimer_Tick;
            }

            if (_bkLoader != null)
            {
                if (_bkLoader.IsBusy)
                    _bkLoader.CancelAsync();
                _bkLoader.DoWork -= BkLoaderRun;
                _bkLoader.RunWorkerCompleted -= BkLoaderCompleted;
                _bkLoader.Dispose();
            }

            GridManager?.Dispose();

            Items.Clear();
            FileList.Clear();

            base.OnDispose();
        }

        #region Costants

        public const string NotifyScrollIntoView = "ScrollIntoView";

        #endregion

        #region Commands

        /// <summary>
        /// Exit Command
        /// </summary>
        public ICommandAncestor CommandExit { get; protected set; }

        /// <summary>
        /// OpenFile Command
        /// </summary>
        public ICommandAncestor CommandOpenFile { get; protected set; }

        /// <summary>
        /// SelectFolder Command
        /// </summary>
        public ICommandAncestor CommandSelectFolder { get; protected set; }

        /// <summary>
        /// SaveFolder Command
        /// </summary>
        public ICommandAncestor CommandSaveFolder { get; protected set; }

        /// <summary>
        /// Refresh Command
        /// </summary>
        public ICommandAncestor CommandRefresh { get; protected set; }

        /// <summary>
        /// RefreshFiles Command
        /// </summary>
        public ICommandAncestor CommandRefreshFiles { get; protected set; }

        /// <summary>
        /// Clear Command
        /// </summary>
        public ICommandAncestor CommandClear { get; protected set; }

        /// <summary>
        /// Delete Command
        /// </summary>
        public ICommandAncestor CommandDelete { get; protected set; }

        /// <summary>
        /// OpenSelectedFolder Command
        /// </summary>
        public ICommandAncestor CommandOpenSelectedFolder { get; protected set; }

        /// <summary>
        /// SelectAllFiles Command
        /// </summary>
        public ICommandAncestor CommandSelectAllFiles { get; protected set; }

        /// <summary>
        /// About Command
        /// </summary>
        public ICommandAncestor CommandAbout { get; protected set; }

        protected virtual object CommandExitExecute(object parameter)
        {
            _callingWin.Close();
            return null;
        }

        protected virtual object CommandOpenFileExecute(object parameter)
        {
            using (var dlg = new System.Windows.Forms.OpenFileDialog())
            {
                var addFile = parameter != null && parameter.Equals("ADD");
                dlg.Filter =
                    $"{Resources.MainWindowVM_commandOpenFileExecute_JsonFilesCaption} (*.json)|*.json|{Resources.MainWindowVM_commandOpenFileExecute_XmlFilesCaption} (*.xml)|*.xml|{Resources.MainWindowVM_commandOpenFileExecute_AllFilesCaption} (*.*)|*.*";
                dlg.DefaultExt = "json";
                dlg.Multiselect = true;
                dlg.Title = addFile
                    ? Resources.MainWindowVM_commandOpenFileExecute_Add_Log_File
                    : Resources.MainWindowVM_commandOpenFileExecute_Open_Log_File;

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var files = dlg.FileNames;
                    SelectedFolder = null;
                    LoadFileList(files, addFile);
                }
            }

            return null;
        }

        protected virtual bool CommandOpenFileCanExecute(object parameter)
        {
            return true;
        }

        protected virtual object CommandSelectFolderExecute(object parameter)
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.Description = Resources.MainWindowVM_commandSelectFolderExecute_Description;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var selectedPath = dlg.SelectedPath;
                    SelectedFolder = null;
                    for (var i = 0; i < FolderList.Count; i++)
                    {
                        var item = FolderList[i];
                        if (item.Path.Equals(selectedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            SelectedFolder = item;
                            return null;
                        }
                    }

                    LoadFolderFiles(selectedPath);
                }
            }

            return null;
        }

        protected virtual bool CommandSelectFolderCanExecute(object parameter)
        {
            return true;
        }

        protected virtual object CommandSaveFolderExecute(object parameter)
        {
            var win = new AddFolderPath { Owner = _callingWin as Window };
            if (win.EditList())
                LoadFolderList();
            return null;
        }

        protected virtual bool CommandSaveFolderCanExecute(object parameter)
        {
            return true;
        }

        protected virtual object CommandClearExecute(object parameter)
        {
            if (GridManager != null)
            {
                GridManager.ResetSearchTextBox();
                RefreshView();
            }

            return null;
        }

        protected virtual bool CommandClearCanExecute(object parameter)
        {
            return true;
        }

        protected virtual object CommandRefreshExecute(object parameter)
        {
            Items.Clear();

            if (IsFileSelectionEnabled)
            {
                foreach (var item in FileList)
                {
                    if (item.Checked)
                        LoadLogFile(item.Path, true);
                }
            }
            else
            {
                if (FileList.Count > 0 && SelectedFile != null)
                    LoadLogFile(SelectedFile.Path);
            }

            return null;
        }

        protected virtual bool CommandRefreshCanExecute(object parameter)
        {
            return true;
        }

        protected virtual object CommandRefreshFilesExecute(object parameter)
        {
            if (_selectedFolder != null)
            {
                Items.Clear();

                if (IsFileSelectionEnabled)
                {
                    //Reload file list and restore all checked items
                    IList<string> checkedItems = (from f in FileList
                        where f.Checked
                        select f.Path).ToList();
                    SelectedFile = null;
                    LoadFolderFiles(_selectedFolder.Path);

                    if (checkedItems != null && checkedItems.Count > 0)
                    {
                        foreach (var filePath in checkedItems)
                        {
                            var selItem = (from f in FileList
                                where filePath.Equals(f.Path, StringComparison.OrdinalIgnoreCase)
                                select f).FirstOrDefault();
                            if (selItem != null)
                                selItem.Checked = true;
                        }
                    }
                }
                else
                {
                    //Reload file list and restore selected item
                    var selectedFilePath = SelectedFile != null ? SelectedFile.Path : string.Empty;
                    SelectedFile = null;
                    LoadFolderFiles(_selectedFolder.Path);

                    if (!string.IsNullOrWhiteSpace(selectedFilePath))
                    {
                        var selItem = (from f in FileList
                            where selectedFilePath.Equals(f.Path, StringComparison.OrdinalIgnoreCase)
                            select f).FirstOrDefault();
                        if (selItem != null)
                            SelectedFile = selItem;
                    }
                }
            }

            return null;
        }

        protected virtual bool CommandRefreshFilesCanExecute(object parameter)
        {
            return SelectedFolder != null;
        }

        protected virtual object CommandDeleteExecute(object parameter)
        {
            if (IsFileSelectionEnabled)
            {
                if (MessageBox.Show(Resources.MainWindowVM_commandDeleteExecute_DeleteCheckedFiles_ConfirmText,
                        Resources.MainWindowVM_commandDeleteExecute_DeleteCheckedFiles_ConfirmTitle,
                        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    return null;

                //Delete all selected file
                for (var i = FileList.Count - 1; i >= 0; i--)
                {
                    var item = FileList[i];
                    if (item.Checked && DeleteFile(item.Path))
                    {
                        Items.Clear();
                        SelectedFile = null;
                        FileList.RemoveAt(i);
                    }
                }
            }
            else
            {
                if (MessageBox.Show(Resources.MainWindowVM_commandDeleteExecute_DeleteSelectedFile_ConfirmText,
                        Resources.MainWindowVM_commandDeleteExecute_DeleteSelectedFile_ConfirmTitle,
                        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    return null;

                //Delete selected file
                if (SelectedFile != null)
                {
                    var indexToDelete = FileList.IndexOf(SelectedFile);
                    if (DeleteFile(SelectedFile.Path))
                    {
                        Items.Clear();
                        SelectedFile = null;
                        FileList.RemoveAt(indexToDelete);
                    }
                }
            }

            return null;
        }

        protected virtual bool CommandDeleteCanExecute(object parameter)
        {
            if (IsFileSelectionEnabled)
            {
                if (FileList == null || FileList.Count == 0)
                    return false;

                return (from f in FileList
                    where f.Checked
                    select f).Count() > 0;
            }

            return SelectedFile != null;
        }

        protected virtual object CommandOpenSelectedFolderExecute(object parameter)
        {
            var path = SelectedFolder != null ? SelectedFolder.Path : string.Empty;
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                Process.Start("explorer.exe", path);
            return null;
        }

        protected virtual bool CommandOpenSelectedFolderCanExecute(object parameter)
        {
            return SelectedFolder != null;
        }

        protected virtual object CommandSelectAllFilesExecute(object parameter)
        {
            if (parameter == null)
                return null;

            if (IsFileSelectionEnabled)
            {
                try
                {
                    _loadingAllFiles = true;

                    switch (parameter.ToString())
                    {
                        case "ALL":
                            IList<string> files = new List<string>();
                            foreach (var item in FileList)
                            {
                                files.Add(item.Path);
                                item.Checked = true;
                            }

                            if (_bkLoader != null)
                            {
                                while (IsLoading)
                                    GlobalHelper.DoEvents();

                                IsLoading = true;

                                object[] args = { files.ToArray(), false };
                                _bkLoader.RunWorkerAsync(args);
                            }

                            break;

                        case "NONE":
                            foreach (var item in FileList)
                                item.Checked = false;

                            Items.Clear();
                            UpdateCounters();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Resources.GlobalHelper_ParseLogFile_Error_Title, MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
                }
                finally
                {
                    _loadingAllFiles = false;
                }
            }

            return null;
        }

        protected virtual bool CommandSelectAllFilesCanExecute(object parameter)
        {
            return IsFileSelectionEnabled && FileList.Count > 0;
        }

        protected virtual object CommandAboutExecute(object parameter)
        {
            var win = new About { Owner = _callingWin as Window };
            win.ShowDialog();
            return null;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// RefreshUI Action
        /// </summary>
        public Action<string, object> RefreshUi { get; set; }

        /// <summary>
        /// WindowTitle Property
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                RaisePropertyChanged(PropWindowTitle);
            }
        }

        private string _windowTitle;
        public static string PropWindowTitle = "WindowTitle";

        /// <summary>
        /// RecentFileList Manager
        /// </summary>
        public RecentFileList RecentFileList
        {
            get => _recentFileList;
            set
            {
                _recentFileList = value;
                if (_recentFileList != null)
                {
                    _recentFileList.MenuClick += (s, e) =>
                    {
                        SelectedFolder = null;
                        LoadFileList(new[] { e.Filepath });
                    };
                    UpdateJumpList();
                }
            }
        }

        private RecentFileList _recentFileList;
        public static string PropRecentFileList = "RecentFileList";

        /// <summary>
        /// IsLoading Property
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                RaisePropertyChanged(PropIsLoading);
            }
        }

        private bool _isLoading;
        public static string PropIsLoading = "IsLoading";

        /// <summary>
        /// IsFileSelectionEnabled Property
        /// </summary>
        public bool IsFileSelectionEnabled
        {
            get => _isFileSelectionEnabled;
            set
            {
                _isFileSelectionEnabled = value;
                RaisePropertyChanged(PropIsFileSelectionEnabled);

                if (_isFileSelectionEnabled)
                {
                    Items.Clear();
                    if (FileList.Count > 0 && SelectedFile != null)
                        SelectedFile.Checked = true;
                }
                else
                {
                    Items.Clear();
                    foreach (var item in FileList)
                        item.Checked = false;
                    SelectedFile = null;
                }

                RefreshCommandsCanExecute();
                RefreshWindowTitle();
            }
        }

        private bool _isFileSelectionEnabled;
        public static string PropIsFileSelectionEnabled = "IsFileSelectionEnabled";

        /// <summary>
        /// SelectedFile Property
        /// </summary>
        public FileItem SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (value != _selectedFile)
                {
                    _selectedFile = value;
                    RaisePropertyChanged(PropSelectedFile);

                    if (!_loadingFileList && _selectedFile != null)
                    {
                        var path = _selectedFile.Path;
                        SelectedFileDir = !string.IsNullOrWhiteSpace(path) ? Path.GetDirectoryName(path) : string.Empty;
                        if (!IsFileSelectionEnabled)
                            LoadLogFile(path);
                    }

                    RefreshCommandsCanExecute();
                    RefreshWindowTitle();
                }
            }
        }

        private FileItem _selectedFile;
        public static string PropSelectedFile = "SelectedFile";

        /// <summary>
        /// SelectedFolder Property
        /// </summary>
        public PathItem SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                if (value != _selectedFolder)
                {
                    _selectedFolder = value;
                    RaisePropertyChanged(PropSelectedFolder);

                    Items.Clear();
                    if (_selectedFolder != null)
                        LoadFolderFiles(_selectedFolder.Path);

                    RefreshCommandsCanExecute();
                }
            }
        }

        private PathItem _selectedFolder;
        public static string PropSelectedFolder = "SelectedFolder";

        /// <summary>
        /// SelectedFileDir Property
        /// </summary>
        public string SelectedFileDir
        {
            get => _selectedFileDir;
            set
            {
                _selectedFileDir = value;
                RaisePropertyChanged(PropSelectedFileDir);
            }
        }

        private string _selectedFileDir;
        public static string PropSelectedFileDir = "SelectedFileDir";

        /// <summary>
        /// FolderList Property
        /// </summary>
        public ObservableCollection<PathItem> FolderList
        {
            get => _folderList;
            set
            {
                _folderList = value;
                RaisePropertyChanged(PropFolderList);
            }
        }

        private ObservableCollection<PathItem> _folderList;
        public static string PropFolderList = "FolderList";

        /// <summary>
        /// FileList Property
        /// </summary>
        public ObservableCollection<FileItem> FileList
        {
            get => _fileList;
            set
            {
                _fileList = value;
                RaisePropertyChanged(PropFileList);

                RefreshCommandsCanExecute();
            }
        }

        private ObservableCollection<FileItem> _fileList;
        public static string PropFileList = "FileList";

        /// <summary>
        /// LogItems Property
        /// </summary>
        public ObservableCollection<LogItem> Items
        {
            get => _items;
            set
            {
                _items = value;
                RaisePropertyChanged(PropItems);
            }
        }

        private ObservableCollection<LogItem> _items;
        public static string PropItems = "Items";

        /// <summary>
        /// SelectedLogItem Property
        /// </summary>
        public LogItem SelectedLogItem
        {
            get => _selectedLogItem;
            set
            {
                _selectedLogItem = value;
                RaisePropertyChanged(PropSelectedLogItem);

                _goToLogItemId = _selectedLogItem != null ? _selectedLogItem.Id.ToString() : string.Empty;
                RaisePropertyChanged(PropGoToLogItemId);

                RefreshWindowTitle();
            }
        }

        private LogItem _selectedLogItem;
        public static string PropSelectedLogItem = "SelectedLogItem";

        /// <summary>
        /// ShowLevelDebug Property
        /// </summary>
        public bool ShowLevelDebug
        {
            get => _showLevelDebug;
            set
            {
                if (value != _showLevelDebug)
                {
                    _showLevelDebug = value;
                    RaisePropertyChanged(PropShowLevelDebug);
                    ResetLevelSelection();
                    RefreshView();
                }
            }
        }

        private bool _showLevelDebug;
        public static string PropShowLevelDebug = "ShowLevelDebug";

        /// <summary>
        /// ShowLevelInfo Property
        /// </summary>
        public bool ShowLevelInfo
        {
            get => _showLevelInfo;
            set
            {
                if (value != _showLevelInfo)
                {
                    _showLevelInfo = value;
                    RaisePropertyChanged(PropShowLevelInfo);
                    ResetLevelSelection();
                    RefreshView();
                }
            }
        }

        private bool _showLevelInfo;
        public static string PropShowLevelInfo = "ShowLevelInfo";

        /// <summary>
        /// ShowLevelWarn Property
        /// </summary>
        public bool ShowLevelWarn
        {
            get => _showLevelWarn;
            set
            {
                if (value != _showLevelWarn)
                {
                    _showLevelWarn = value;
                    RaisePropertyChanged(PropShowLevelWarn);
                    ResetLevelSelection();
                    RefreshView();
                }
            }
        }

        private bool _showLevelWarn;
        public static string PropShowLevelWarn = "ShowLevelWarn";

        /// <summary>
        /// ShowLevelError Property
        /// </summary>
        public bool ShowLevelError
        {
            get => _showLevelError;
            set
            {
                if (value != _showLevelError)
                {
                    _showLevelError = value;
                    RaisePropertyChanged(PropShowLevelError);
                    ResetLevelSelection();
                    RefreshView();
                }
            }
        }

        private bool _showLevelError;
        public static string PropShowLevelError = "ShowLevelError";

        /// <summary>
        /// ShowLevelFatal Property
        /// </summary>
        public bool ShowLevelFatal
        {
            get => _showLevelFatal;
            set
            {
                if (value != _showLevelFatal)
                {
                    _showLevelFatal = value;
                    RaisePropertyChanged(PropShowLevelFatal);
                    ResetLevelSelection();
                    RefreshView();
                }
            }
        }

        private bool _showLevelFatal;
        public static string PropShowLevelFatal = "ShowLevelFatal";

        /// <summary>
        /// SelectAll Property
        /// </summary>
        public bool SelectAll
        {
            get => _selectAll;
            set
            {
                if (value != _selectAll)
                {
                    _selectAll = value;
                    RaisePropertyChanged(PropSelectAll);

                    if (_selectAll)
                    {
                        _showLevelDebug = _showLevelInfo = _showLevelWarn = _showLevelError = _showLevelFatal = true;
                        RefreshCheckBoxBinding();
                        RefreshView();
                    }
                }
            }
        }

        private bool _selectAll;
        public static string PropSelectAll = "SelectAll";

        /// <summary>
        /// SelectDebug Property
        /// </summary>
        public bool SelectDebug
        {
            get => _selectDebug;
            set
            {
                if (value != _selectDebug)
                {
                    _selectDebug = value;
                    RaisePropertyChanged(PropSelectDebug);

                    if (_selectDebug)
                    {
                        _showLevelInfo = _showLevelWarn = _showLevelError = _showLevelFatal = false;
                        _showLevelDebug = true;
                        RefreshCheckBoxBinding();
                        RefreshView();
                    }
                }
            }
        }

        private bool _selectDebug;
        public static string PropSelectDebug = "SelectDebug";

        /// <summary>
        /// SelectInfo Property
        /// </summary>
        public bool SelectInfo
        {
            get => _selectInfo;
            set
            {
                if (value != _selectInfo)
                {
                    _selectInfo = value;
                    RaisePropertyChanged(PropSelectInfo);

                    if (_selectInfo)
                    {
                        _showLevelDebug = _showLevelWarn = _showLevelError = _showLevelFatal = false;
                        _showLevelInfo = true;
                        RefreshCheckBoxBinding();
                        RefreshView();
                    }
                }
            }
        }

        private bool _selectInfo;
        public static string PropSelectInfo = "SelectInfo";

        /// <summary>
        /// SelectWarn Property
        /// </summary>
        public bool SelectWarn
        {
            get => _selectWarn;
            set
            {
                if (value != _selectWarn)
                {
                    _selectWarn = value;
                    RaisePropertyChanged(PropSelectWarn);

                    if (_selectWarn)
                    {
                        _showLevelDebug = _showLevelInfo = _showLevelError = _showLevelFatal = false;
                        _showLevelWarn = true;
                        RefreshCheckBoxBinding();
                        RefreshView();
                    }
                }
            }
        }

        private bool _selectWarn;
        public static string PropSelectWarn = "SelectWarn";

        /// <summary>
        /// SelectError Property
        /// </summary>
        public bool SelectError
        {
            get => _selectError;
            set
            {
                if (value != _selectError)
                {
                    _selectError = value;
                    RaisePropertyChanged(PropSelectError);

                    if (_selectError)
                    {
                        _showLevelDebug = _showLevelInfo = _showLevelWarn = _showLevelFatal = false;
                        _showLevelError = true;
                        RefreshCheckBoxBinding();
                        RefreshView();
                    }
                }
            }
        }

        private bool _selectError;
        public static string PropSelectError = "SelectError";

        /// <summary>
        /// SelectFatal Property
        /// </summary>
        public bool SelectFatal
        {
            get => _selectFatal;
            set
            {
                if (value != _selectFatal)
                {
                    _selectFatal = value;
                    RaisePropertyChanged(PropSelectFatal);

                    if (_selectFatal)
                    {
                        _showLevelDebug = _showLevelInfo = _showLevelWarn = _showLevelError = false;
                        _showLevelFatal = true;
                        RefreshCheckBoxBinding();
                        RefreshView();
                    }
                }
            }
        }

        private bool _selectFatal;
        public static string PropSelectFatal = "SelectFatal";

        /// <summary>
        /// GoToLogItemId Property
        /// </summary>
        public string GoToLogItemId
        {
            get => _goToLogItemId;
            set
            {
                _goToLogItemId = value;

                var idGoTo = 0;
                int.TryParse(value, out idGoTo);
                var currentId = SelectedLogItem?.Id ?? 0;

                if (idGoTo > 0 && idGoTo != currentId)
                {
                    var selectItem = (from it in Items
                        where it.Id == idGoTo
                        select it).FirstOrDefault();

                    if (selectItem != null)
                        SelectedLogItem = selectItem;
                }
                else
                    _goToLogItemId = currentId != 0 ? currentId.ToString() : string.Empty;

                RaisePropertyChanged(PropGoToLogItemId);
            }
        }

        private string _goToLogItemId;
        public static string PropGoToLogItemId = "GoToLogItemId";

        #endregion

        #region Public Methods

        public void LoadFileList(string[] pathList, bool add = false)
        {
            SelectedFile = null;

            _loadingFileList = true;

            if (!add)
                FileList.Clear();

            foreach (var path in pathList)
            {
                //Ignore path if is not valid
                if (!File.Exists(path) && !Directory.Exists(path))
                    continue;

                //Get files list to add
                string[] files = null;
                var attr = File.GetAttributes(path);
                if (attr.HasFlag(FileAttributes.Directory))
                    files = Directory.GetFiles(path);
                else
                    files = new[] { path };

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var newItem = new FileItem(fileName, file);
                    newItem.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
                    {
                        if (e.PropertyName.Equals(FileItem.PropChecked) && !_loadingAllFiles)
                        {
                            if (newItem.Checked)
                                LoadLogFile(newItem.Path, true);
                            else
                                RemoveItems(newItem.Path);

                            RefreshCommandsCanExecute();
                        }
                    };
                    FileList.Add(newItem);
                }
            }

            _loadingFileList = false;

            //Load item if only one
            if (FileList.Count == 1)
            {
                if (IsFileSelectionEnabled)
                {
                    SelectedFile = FileList[0];
                    SelectedFile.Checked = true;
                }
                else
                    SelectedFile = FileList[0];
            }
        }

        #endregion

        #region Privates

        private readonly IWinSimple _callingWin;

        private bool _loadingFileList;

        private bool _loadingAllFiles;

        private void RefreshCheckBoxBinding()
        {
            RaisePropertyChanged(PropShowLevelDebug);
            RaisePropertyChanged(PropShowLevelInfo);
            RaisePropertyChanged(PropShowLevelWarn);
            RaisePropertyChanged(PropShowLevelError);
            RaisePropertyChanged(PropShowLevelFatal);
        }

        private void ResetLevelSelection()
        {
            SelectAll = false;
            SelectDebug = false;
            SelectInfo = false;
            SelectWarn = false;
            SelectError = false;
            SelectFatal = false;
        }

        private void LoadFolderList()
        {
            FileList.Clear();
            SelectedFolder = null;
            var path = Constants.FoldersFilePath;
            IList<PathItem> folders = null;
            try
            {
                folders = DataService.ParseFolderFile(path);
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.GlobalHelper_ParseFolderFile_Error_Text, path, ex.Message);
                MessageBox.Show(message, Resources.GlobalHelper_ParseFolderFile_Error_Title, MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }

            FolderList = folders != null
                ? new ObservableCollection<PathItem>(folders)
                : new ObservableCollection<PathItem>();
        }

        private void LoadFolderFiles(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath);
                LoadFileList(files);
            }
            else
            {
                FileList.Clear();
                MessageBox.Show(string.Format(Resources.MainWindowVM_loadFolderFiles_ErrorMessage_Text, folderPath),
                    Resources.MainWindowVM_loadFolderFiles_ErrorMessageText_Title, MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }
        }

        private void LoadLogFile(string path, bool merge = false)
        {
            if (_bkLoader != null)
            {
                while (IsLoading)
                    GlobalHelper.DoEvents();

                IsLoading = true;

                RecentFileList.InsertFile(path);
                UpdateJumpList();

                object[] args = { new[] { path }, merge };
                _bkLoader.RunWorkerAsync(args);
            }
        }

        private void RemoveItems(string path)
        {
            //Less performance
            //for (int i = Items.Count - 1; i >= 0; i--)
            //{
            //    if (Items[i].Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            //        Items.RemoveAt(i);
            //}

            //Best performance
            var selectedItems = from it in Items
                where !it.Path.Equals(path, StringComparison.OrdinalIgnoreCase)
                select it;
            Items = new ObservableCollection<LogItem>(selectedItems);

            var itemId = 1;
            foreach (var item in Items)
                item.Id = itemId++;

            UpdateCounters();
        }

        private bool DeleteFile(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                fileInfo?.Delete();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Resources.MainWindowVM_deleteFile_ErrorMessage_Text, path, ex.Message),
                    Resources.MainWindowVM_deleteFile_ErrorMessage_Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void RefreshCommandsCanExecute()
        {
            CommandRefreshFiles.OnCanExecuteChanged();
            CommandDelete.OnCanExecuteChanged();
            CommandOpenSelectedFolder.OnCanExecuteChanged();
            CommandSelectAllFiles.OnCanExecuteChanged();
        }

        private void UpdateJumpList()
        {
            var myJumpList = JumpList.GetJumpList(Application.Current);

            if (myJumpList == null)
            {
                myJumpList = new JumpList();
                JumpList.SetJumpList(Application.Current, myJumpList);
            }

            myJumpList.JumpItems.Clear();
            if (RecentFileList != null && RecentFileList.RecentFiles != null)
            {
                foreach (var item in RecentFileList.RecentFiles)
                {
                    try
                    {
                        var myJumpTask = new JumpTask
                        {
                            CustomCategory = Resources.MainWindowVM_updateJumpList_CustomCategoryName,
                            Title = Path.GetFileName(item),
                            //myJumpTask.Description = "";
                            ApplicationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                AppDomain.CurrentDomain.FriendlyName),
                            Arguments = item
                        };
                        myJumpList.JumpItems.Add(myJumpTask);
                    }
                    catch (Exception)
                    {
                        //throw;
                    }
                }
            }

            myJumpList.Apply();
        }

        private void RaiseRefreshUi(string eventName, object parameter = null)
        {
            RefreshUi?.Invoke(eventName, parameter);
        }

        private void RefreshWindowTitle()
        {
            var fileName = string.Empty;
            if (IsFileSelectionEnabled && SelectedLogItem != null)
            {
                fileName = (from f in FileList
                    where SelectedLogItem.Path.Equals(f.Path, StringComparison.OrdinalIgnoreCase)
                    select f.FileName).FirstOrDefault();
            }

            if (!IsFileSelectionEnabled && SelectedFile != null)
                fileName = SelectedFile.FileName;

            var title = string.Empty;
            if (!string.IsNullOrWhiteSpace(fileName))
                title = $"{Resources.MainWindow_Title} / {fileName}";
            else
                title = Resources.MainWindow_Title;

            if (!title.Equals(WindowTitle, StringComparison.OrdinalIgnoreCase))
                WindowTitle = title;
        }

        #endregion

        #region BackgroundWorker Methods (bkLoader)

        private readonly BackgroundWorker _bkLoader;

        private void BkLoaderRun(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as object[];
            if (args == null)
                return;

            var pathList = args[0] as string[];
            var merge = (bool)args[1];
            var res = new List<LogItem>();
            var count = 0;

            if (pathList != null)
            {
                foreach (var path in pathList)
                {
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    try
                    {
                        var list = DataService.ParseLogFile(path);
                        if (list != null)
                        {
                            res.AddRange(list);
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var message = string.Format(Resources.GlobalHelper_ParseLogFile_Error_Text, path, ex.Message);
                        MessageBox.Show(message, Resources.GlobalHelper_ParseLogFile_Error_Title, MessageBoxButton.OK,
                            MessageBoxImage.Exclamation);
                    }

                    var worker = sender as BackgroundWorker;
                    if (worker != null && worker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            //If loaded more than one list, then sort items by timestamp
            if (count > 1)
            {
                res = (from it in res
                    orderby it.TimeStamp
                    select it).ToList();

                var itemId = 1;
                foreach (var item in res)
                    item.Id = itemId++;
            }

            e.Result = new object[] { res, merge };
        }

        private void BkLoaderCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                MessageBox.Show(string.Format(Resources.MainWindowVM_bkLoaderCompleted_UnreadableFile_Text, e.Error),
                    Resources.MainWindowVM_bkLoaderCompleted_UnreadableFile_Title, MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            else
            {
                if (!e.Cancelled && e.Result != null)
                {
                    var res = e.Result as object[];
                    var list = res[0] as IList<LogItem>;
                    var merge = (bool)res[1];

                    if (merge && Items.Count > 0)
                    {
                        //Merge result list with existing items
                        IList<LogItem> mergeList = new List<LogItem>(Items);
                        var startId = mergeList.Count;

                        foreach (var item in list)
                            mergeList.Add(item);

                        mergeList = (from it in mergeList
                            orderby it.TimeStamp
                            select it).ToList();

                        var itemId = 1;
                        foreach (var item in mergeList)
                            item.Id = itemId++;

                        list = mergeList;
                    }

                    Items.Clear();
                    Items = new ObservableCollection<LogItem>(list);

                    UpdateCounters();

                    if (Items.Count > 0)
                    {
                        var lastItem = (from it in Items
                            where LevelCheckFilter(it)
                            select it).LastOrDefault();

                        SelectedLogItem = lastItem ?? Items[Items.Count - 1];
                    }
                }
            }

            IsLoading = false;
        }

        #endregion

        #region FilteredGridManager

        /// <summary>
        /// GridManager Property
        /// </summary>
        public FilteredGridManager GridManager { get; set; }

        public void InitDataGrid()
        {
            if (GridManager != null)
            {
                IList<ColumnItem> dgColumns = new List<ColumnItem>
                {
                    new ColumnItem("Id", 37, null, CellAlignment.Center, string.Empty)
                        { Header = Resources.MainWindowVM_InitDataGrid_IdColumn_Header },
                    new ColumnItem("TimeStamp", 120, null, CellAlignment.Center, GlobalHelper.DisplayDateTimeFormat)
                        { Header = Resources.MainWindowVM_InitDataGrid_TimeStampColumn_Header },
                    new ColumnItem("Level", null, 50, CellAlignment.Center)
                        { Header = Resources.MainWindowVM_InitDataGrid_LevelColumn_Header },
                    new ColumnItem("Message", null, 300)
                        { Header = Resources.MainWindowVM_InitDataGrid_MessageColumn_Header },
                    new ColumnItem("Logger", 150, null)
                        { Header = Resources.MainWindowVM_InitDataGrid_LoggerColumn_Header },
                    new ColumnItem("MachineName", 110, null, CellAlignment.Center)
                        { Header = Resources.MainWindowVM_InitDataGrid_MachineNameColumn_Header },
                    new ColumnItem("HostName", 110, null, CellAlignment.Center)
                        { Header = Resources.MainWindowVM_InitDataGrid_HostNameColumn_Header },
                    new ColumnItem("UserName", 110, null, CellAlignment.Center)
                        { Header = Resources.MainWindowVM_InitDataGrid_UserNameColumn_Header },
                    new ColumnItem("App", 150, null) { Header = Resources.MainWindowVM_InitDataGrid_AppColumn_Header },
                    new ColumnItem("Thread", 44, null, CellAlignment.Center)
                        { Header = Resources.MainWindowVM_InitDataGrid_ThreadColumn_Header },
                    new ColumnItem("Class", null, 300)
                        { Header = Resources.MainWindowVM_InitDataGrid_ClassColumn_Header },
                    new ColumnItem("Method", 200, null)
                        { Header = Resources.MainWindowVM_InitDataGrid_MethodColumn_Header }
                    //new ColumnItem("Delta", 60, null, CellAlignment.CENTER, null, "Δ"),
                    //new ColumnItem("Path", 50)
                };
                GridManager.BuildDataGrid(dgColumns);
                GridManager.AssignSource(new Binding(PropItems) { Source = this, Mode = BindingMode.OneWay });
                GridManager.OnBeforeCheckFilter = LevelCheckFilter;
            }
        }

        public void RefreshView()
        {
            if (GridManager != null)
            {
                var view = GridManager.GetCollectionView();
                view?.Refresh();
                UpdateFilteredCounters(view);
            }

            RaiseRefreshUi(NotifyScrollIntoView);
        }

        private bool LevelCheckFilter(object item)
        {
            var logItem = item as LogItem;
            if (logItem != null)
            {
                switch (logItem.LevelIndex)
                {
                    case LevelIndex.Debug:
                        return ShowLevelDebug;
                    case LevelIndex.Info:
                        return ShowLevelInfo;
                    case LevelIndex.Warn:
                        return ShowLevelWarn;
                    case LevelIndex.Error:
                        return ShowLevelError;
                    case LevelIndex.Fatal:
                        return ShowLevelFatal;
                }
            }

            return true;
        }

        #endregion

        #region Counters

        /// <summary>
        /// ItemsDebugCount Property
        /// </summary>
        public int ItemsDebugCount
        {
            get => _itemsDebugCount;
            set
            {
                _itemsDebugCount = value;
                RaisePropertyChanged(PropItemsDebugCount);
            }
        }

        private int _itemsDebugCount;
        public static string PropItemsDebugCount = "ItemsDebugCount";

        /// <summary>
        /// ItemsInfoCount Property
        /// </summary>
        public int ItemsInfoCount
        {
            get => _itemsInfoCount;
            set
            {
                _itemsInfoCount = value;
                RaisePropertyChanged(PropItemsInfoCount);
            }
        }

        private int _itemsInfoCount;
        public static string PropItemsInfoCount = "ItemsInfoCount";

        /// <summary>
        /// ItemsWarnCount Property
        /// </summary>
        public int ItemsWarnCount
        {
            get => _itemsWarnCount;
            set
            {
                _itemsWarnCount = value;
                RaisePropertyChanged(PropItemsWarnCount);
            }
        }

        private int _itemsWarnCount;
        public static string PropItemsWarnCount = "ItemsWarnCount";

        /// <summary>
        /// ItemsErrorCount Property
        /// </summary>
        public int ItemsErrorCount
        {
            get => _itemsErrorCount;
            set
            {
                _itemsErrorCount = value;
                RaisePropertyChanged(PropItemsErrorCount);
            }
        }

        private int _itemsErrorCount;
        public static string PropItemsErrorCount = "ItemsErrorCount";

        /// <summary>
        /// ItemsFatalCount Property
        /// </summary>
        public int ItemsFatalCount
        {
            get => _itemsFatalCount;
            set
            {
                _itemsFatalCount = value;
                RaisePropertyChanged(PropItemsFatalCount);
            }
        }

        private int _itemsFatalCount;
        public static string PropItemsFatalCount = "ItemsFatalCount";

        /// <summary>
        /// ItemsDebugFilterCount Property
        /// </summary>
        public int ItemsDebugFilterCount
        {
            get => _itemsDebugFilterCount;
            set
            {
                _itemsDebugFilterCount = value;
                RaisePropertyChanged(PropItemsDebugFilterCount);
            }
        }

        private int _itemsDebugFilterCount;
        public static string PropItemsDebugFilterCount = "ItemsDebugFilterCount";

        /// <summary>
        /// ItemsInfoFilterCount Property
        /// </summary>
        public int ItemsInfoFilterCount
        {
            get => _itemsInfoFilterCount;
            set
            {
                _itemsInfoFilterCount = value;
                RaisePropertyChanged(PropItemsInfoFilterCount);
            }
        }

        private int _itemsInfoFilterCount;
        public static string PropItemsInfoFilterCount = "ItemsInfoFilterCount";

        /// <summary>
        /// ItemsWarnFilterCount Property
        /// </summary>
        public int ItemsWarnFilterCount
        {
            get => _itemsWarnFilterCount;
            set
            {
                _itemsWarnFilterCount = value;
                RaisePropertyChanged(PropItemsWarnFilterCount);
            }
        }

        private int _itemsWarnFilterCount;
        public static string PropItemsWarnFilterCount = "ItemsWarnFilterCount";

        /// <summary>
        /// ItemsErrorFilterCount Property
        /// </summary>
        public int ItemsErrorFilterCount
        {
            get => _itemsErrorFilterCount;
            set
            {
                _itemsErrorFilterCount = value;
                RaisePropertyChanged(PropItemsErrorFilterCount);
            }
        }

        private int _itemsErrorFilterCount;
        public static string PropItemsErrorFilterCount = "ItemsErrorFilterCount";

        /// <summary>
        /// ItemsFatalFilterCount Property
        /// </summary>
        public int ItemsFatalFilterCount
        {
            get => _itemsFatalFilterCount;
            set
            {
                _itemsFatalFilterCount = value;
                RaisePropertyChanged(PropItemsFatalFilterCount);
            }
        }

        private int _itemsFatalFilterCount;
        public static string PropItemsFatalFilterCount = "ItemsFatalFilterCount";

        /// <summary>
        /// ItemsFilterCount Property
        /// </summary>
        public int ItemsFilterCount
        {
            get => _itemsFilterCount;
            set
            {
                _itemsFilterCount = value;
                RaisePropertyChanged(PropItemsFilterCount);
            }
        }

        private int _itemsFilterCount;
        public static string PropItemsFilterCount = "ItemsFilterCount";

        private void UpdateCounters()
        {
            ItemsDebugCount = (from it in Items
                where it.Level.Equals("DEBUG", StringComparison.OrdinalIgnoreCase)
                select it).Count();

            ItemsInfoCount = (from it in Items
                where it.Level.Equals("INFO", StringComparison.OrdinalIgnoreCase)
                select it).Count();

            ItemsWarnCount = (from it in Items
                where it.Level.Equals("WARN", StringComparison.OrdinalIgnoreCase)
                select it).Count();

            ItemsErrorCount = (from it in Items
                where it.Level.Equals("ERROR", StringComparison.OrdinalIgnoreCase)
                select it).Count();

            ItemsFatalCount = (from it in Items
                where it.Level.Equals("FATAL", StringComparison.OrdinalIgnoreCase)
                select it).Count();

            RefreshView();
        }

        private void UpdateFilteredCounters(ICollectionView filteredList)
        {
            if (filteredList != null)
            {
                var fltList = filteredList.Cast<LogItem>();
                if (fltList != null)
                {
                    ItemsFilterCount = fltList.Count();

                    ItemsDebugFilterCount = (from it in fltList
                        where it.Level.Equals("DEBUG", StringComparison.OrdinalIgnoreCase)
                        select it).Count();

                    ItemsInfoFilterCount = (from it in fltList
                        where it.Level.Equals("INFO", StringComparison.OrdinalIgnoreCase)
                        select it).Count();

                    ItemsWarnFilterCount = (from it in fltList
                        where it.Level.Equals("WARN", StringComparison.OrdinalIgnoreCase)
                        select it).Count();

                    ItemsErrorFilterCount = (from it in fltList
                        where it.Level.Equals("ERROR", StringComparison.OrdinalIgnoreCase)
                        select it).Count();

                    ItemsFatalFilterCount = (from it in fltList
                        where it.Level.Equals("FATAL", StringComparison.OrdinalIgnoreCase)
                        select it).Count();
                }
            }
            else
            {
                ItemsFilterCount = 0;
                ItemsDebugFilterCount = 0;
                ItemsInfoFilterCount = 0;
                ItemsWarnFilterCount = 0;
                ItemsErrorFilterCount = 0;
                ItemsFatalFilterCount = 0;
            }
        }

        #endregion

        #region Auto Refresh

        /// <summary>
        /// IsAutoRefreshEnabled Property
        /// </summary>
        public bool IsAutoRefreshEnabled
        {
            get => _isAutoRefreshEnabled;
            set
            {
                _isAutoRefreshEnabled = value;
                RaisePropertyChanged(PropIsAutoRefreshEnabled);

                if (_dispatcherTimer != null)
                {
                    if (_isAutoRefreshEnabled)
                        _dispatcherTimer.Start();
                    else
                        _dispatcherTimer.Stop();
                }
            }
        }

        private bool _isAutoRefreshEnabled;
        public static string PropIsAutoRefreshEnabled = "IsAutoRefreshEnabled";

        /// <summary>
        /// AutoRefreshInterval Property
        /// </summary>
        public int AutoRefreshInterval
        {
            get => _autoRefreshInterval;
            set
            {
                _autoRefreshInterval = value;
                RaisePropertyChanged(PropAutoRefreshInterval);
                RaisePropertyChanged(PropAutoRefreshIntervalLocalized);
                if (_dispatcherTimer != null)
                    _dispatcherTimer.Interval = new TimeSpan(0, 0, _autoRefreshInterval);
            }
        }

        private int _autoRefreshInterval;
        public static string PropAutoRefreshInterval = "AutoRefreshInterval";

        public string AutoRefreshIntervalLocalized =>
            string.Format(Resources.MainWindowVM_AutoRefreshIntervalLocalized_Format,
                AutoRefreshInterval.ToString(
                    System.Globalization.CultureInfo.GetCultureInfo(Resources.CultureName)));

        public static string PropAutoRefreshIntervalLocalized = "AutoRefreshIntervalLocalized";


        /// <summary>
        /// IncreaseInterval Command
        /// </summary>
        public ICommandAncestor CommandIncreaseInterval { get; protected set; }

        /// <summary>
        /// DecreaseInterval Command
        /// </summary>
        public ICommandAncestor CommandDecreaseInterval { get; protected set; }

        protected virtual object CommandIncreaseIntervalExecute(object parameter)
        {
            AutoRefreshInterval += Constants.DefaultRefreshInterval;
            return null;
        }

        protected virtual object CommandDecreaseIntervalExecute(object parameter)
        {
            if (AutoRefreshInterval > Constants.DefaultRefreshInterval)
                AutoRefreshInterval -= Constants.DefaultRefreshInterval;
            return null;
        }

        private readonly DispatcherTimer _dispatcherTimer;

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            DateTime? currentLog = null;

            if (SelectedLogItem != null)
                currentLog = SelectedLogItem.TimeStamp;

            CommandRefresh.Execute(null);

            if (currentLog.HasValue)
            {
                while (IsLoading)
                    GlobalHelper.DoEvents();

                var currentItem = (from it in Items
                    where DateTime.Compare(it.TimeStamp, currentLog.Value) == 0
                    select it).FirstOrDefault();

                if (currentItem != null)
                    SelectedLogItem = currentItem;
            }
        }

        #endregion
    }
}