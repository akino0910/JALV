using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace JALV.Common
{
    public static class FrameRateHelper
    {
        public static int? DesiredFrameRate;

        static FrameRateHelper()
        {
            switch (RenderCapability.Tier >> 16)
            {
                case 2: // mostly hardware
                    DesiredFrameRate = 30;
                    break;

                case 1: // partially hardware
                    DesiredFrameRate = 20;
                    break;

                case 0: // software
                default:
                    DesiredFrameRate = 10;
                    break;
            }
        }

        public static void SetTimelineDefaultFramerate(int? framerate)
        {
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata
            {
                DefaultValue = framerate
            });
        }
    }
}