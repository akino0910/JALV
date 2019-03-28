using System.Windows;
using JALV.Common.Interfaces;
using JALV.ViewModel;

namespace JALV
{
    /// <summary>
    /// Interaction logic for AddFolderPath.xaml
    /// </summary>
    public partial class AddFolderPath : Window, IWinSimple
    {
        public AddFolderPath()
        {
            InitializeComponent();
            //this.Closing += delegate { _vm.Dispose(); };
        }

        public bool EditList()
        {
            bool res = false;
            AddFolderPathVM _vm = new AddFolderPathVM(this);
            using (_vm)
            {
                this.DataContext = _vm;
                this.ShowDialog();
                res = _vm.ListChanged;
            }
            return res;
        }
    }
}
