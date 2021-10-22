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
            var res = false;
            var vm = new AddFolderPathVm(this);
            using (vm)
            {
                DataContext = vm;
                ShowDialog();
                res = vm.ListChanged;
            }

            return res;
        }
    }
}