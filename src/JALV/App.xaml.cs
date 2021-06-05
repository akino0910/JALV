using System.Runtime.Versioning;
using System.Windows;
using JALV.Common;

namespace JALV
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            int? framerate = FrameRateHelper.DesiredFrameRate;
            BusyIndicatorBehavior.FRAMERATE = framerate;
            FrameRateHelper.SetTimelineDefaultFramerate(framerate);

            (new MainWindow(e.Args)).Show();
        }
    }
}
