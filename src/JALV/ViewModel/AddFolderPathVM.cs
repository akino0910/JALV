using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using JALV.Common;
using JALV.Common.Interfaces;
using JALV.Core;
using JALV.Core.Domain;
using JALV.Properties;

namespace JALV.ViewModel
{
    public class AddFolderPathVm
        : BindableObject
    {
        public AddFolderPathVm(IWinSimple win)
        {
            _callingWin = win;

            CommandExit = new CommandRelay(CommandExitExecute, p => true);
            CommandSave = new CommandRelay(CommandSaveExecute, CommandSaveCanExecute);
            CommandAdd = new CommandRelay(CommandAddExecute, CommandAddCanExecute);
            CommandRemove = new CommandRelay(CommandRemoveExecute, CommandRemoveCanExecute);
            CommandSelectFolder = new CommandRelay(CommandSelectFolderExecute, p => true);

            var path = Constants.FoldersFilePath;
            IList<PathItem> folders = null;
            ;
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


            _pathList = folders != null
                ? new ObservableCollection<PathItem>(folders)
                : new ObservableCollection<PathItem>();

            ListChanged = false;
        }

        #region Commands

        /// <summary>
        /// Exit Command
        /// </summary>
        public ICommandAncestor CommandExit { get; protected set; }

        /// <summary>
        /// Save Command
        /// </summary>
        public ICommandAncestor CommandSave { get; protected set; }

        /// <summary>
        /// Add Command
        /// </summary>
        public ICommandAncestor CommandAdd { get; protected set; }

        /// <summary>
        /// Remove Command
        /// </summary>
        public ICommandAncestor CommandRemove { get; protected set; }

        /// <summary>
        /// SelectFolder Command
        /// </summary>
        public ICommandAncestor CommandSelectFolder { get; protected set; }

        protected virtual object CommandExitExecute(object parameter)
        {
            _callingWin.Close();
            return null;
        }

        protected virtual object CommandSaveExecute(object parameter)
        {
            if (PathList != null)
            {
                //Clear item with empty information
                for (var i = PathList.Count - 1; i >= 0; i--)
                {
                    var item = PathList[i];
                    if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Path))
                    {
                        PathList.RemoveAt(i);
                        continue;
                    }

                    item.Path = item.Path.TrimEnd('\\');
                }

                //Order list to save
                IList<PathItem> orderList = (from p in PathList
                    orderby p.Name
                    select p).ToList();
                PathList.Clear();
                foreach (var item in orderList)
                    PathList.Add(item);

                //Save XML File
                var path = Constants.FoldersFilePath;

                try
                {
                    DataService.SaveFolderFile(orderList, path);
                    MessageBox.Show(Resources.AddFolderPathVM_commandSaveExecute_SuccessMessage,
                        Resources.AddFolderPathVM_commandSaveExecute_SuccessTitle, MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    ListChanged = true;
                }
                catch (Exception ex)
                {
                    var message = string.Format(Resources.GlobalHelper_SaveFolderFile_Error_Text, path, ex.Message);
                    MessageBox.Show(message, Resources.GlobalHelper_SaveFolderFile_Error_Title, MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
                }
            }

            return null;
        }

        protected virtual bool CommandSaveCanExecute(object parameter)
        {
            return true;
        }

        protected virtual object CommandAddExecute(object parameter)
        {
            var newItem = new PathItem();
            PathList?.Add(newItem);
            return null;
        }

        protected virtual bool CommandAddCanExecute(object parameter)
        {
            return true;
        }

        protected virtual object CommandRemoveExecute(object parameter)
        {
            PathList?.Remove(SelectedPath);
            return null;
        }

        protected virtual bool CommandRemoveCanExecute(object parameter)
        {
            return SelectedPath != null;
        }

        protected virtual object CommandSelectFolderExecute(object parameter)
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.Description = "Select Log Folder";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK && SelectedPath != null)
                    SelectedPath.Path = dlg.SelectedPath;
            }

            return null;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Notify saved list
        /// </summary>
        public bool ListChanged;

        /// <summary>
        /// PathList Property
        /// </summary>
        public ObservableCollection<PathItem> PathList
        {
            get => _pathList;
            set
            {
                _pathList = value;
                RaisePropertyChanged(PropPathList);
            }
        }

        private ObservableCollection<PathItem> _pathList;
        public static string PropPathList = "PathList";

        /// <summary>
        /// SelectedPath Property
        /// </summary>
        public PathItem SelectedPath
        {
            get => _selectedPath;
            set
            {
                _selectedPath = value;
                RaisePropertyChanged(PropSelectedPath);
                CommandRemove.OnCanExecuteChanged();
            }
        }

        private PathItem _selectedPath;
        public static string PropSelectedPath = "SelectedPath";

        #endregion

        #region Privates

        private readonly IWinSimple _callingWin;

        #endregion
    }
}