using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace LogViewer.Common
{
    /// <summary>
    /// Provides a circular progress bar
    /// </summary>
    public partial class CircularProgressBar : UserControl
    {
        public CircularProgressBar()
        {
            InitializeComponent();
            tbMessage.Text = Properties.Resources.CircularProgressBar_CircularProgressBar_BusyText;
            Timeline.SetDesiredFrameRate(sbAnimation, BusyIndicatorBehavior.Framerate);
        }
    }
}