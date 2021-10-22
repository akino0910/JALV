using System.Windows.Controls;

namespace LogViewer.Common
{
    /// <summary>
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : Menu
    {
        public MainMenu()
        {
            InitializeComponent();
        }

        public RecentFileList RecentFileList => RecentFileListMenu;
    }
}