using System.Windows;
using LogViewer.Common;

namespace LogViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var framerate = FrameRateHelper.DesiredFrameRate;
            BusyIndicatorBehavior.Framerate = framerate;
            FrameRateHelper.SetTimelineDefaultFramerate(framerate);

            new MainWindow(e.Args).Show();
        }
    }
}