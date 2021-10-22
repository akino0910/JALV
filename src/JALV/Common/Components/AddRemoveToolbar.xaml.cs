using System.Windows;
using System.Windows.Controls;

namespace JALV.Common
{
    /// <summary>
    /// Interaction logic for MainToolbar.xaml
    /// </summary>
    public partial class AddRemoveToolbar : ToolBar
    {
        public AddRemoveToolbar()
        {
            InitializeComponent();

            Loaded += delegate(object sender, RoutedEventArgs e)
            {
                var toolBar = sender as ToolBar;
                var overflowGrid = (FrameworkElement)toolBar?.Template.FindName("OverflowGrid", toolBar);
                if (overflowGrid != null)
                    overflowGrid.Visibility = Visibility.Collapsed;
            };
        }
    }
}