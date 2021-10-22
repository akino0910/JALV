using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace JALV.Common
{
    /// <summary>
    /// Refs: http://www.codeproject.com/Articles/39244/Scroll-Synchronization
    /// </summary>
    public class ScrollSynchronizer : DependencyObject
    {
        /// <summary>
        /// Identifies the attached property ScrollGroup
        /// </summary>
        public static readonly DependencyProperty ScrollGroupProperty =
            DependencyProperty.RegisterAttached("ScrollGroup", typeof(string), typeof(ScrollSynchronizer),
                new PropertyMetadata(OnScrollGroupChanged));

        /// <summary>
        /// List of all registered scroll viewers.
        /// </summary>
        private static readonly Dictionary<ScrollViewer, string> ScrollViewers = new Dictionary<ScrollViewer, string>();

#if SILVERLIGHT
		/// <summary>
		/// List of all registered scrollbars.
		/// </summary>
		private static Dictionary<ScrollBar, ScrollViewer> horizontalScrollBars =
 new Dictionary<ScrollBar, ScrollViewer>();

		/// <summary>
		/// List of all registered scrollbars.
		/// </summary>
		private static Dictionary<ScrollBar, ScrollViewer> verticalScrollBars =
 new Dictionary<ScrollBar, ScrollViewer>();
#endif

        /// <summary>
        /// Contains the latest horizontal scroll offset for each scroll group.
        /// </summary>
        private static readonly Dictionary<string, double> HorizontalScrollOffsets = new Dictionary<string, double>();

        /// <summary>
        /// Contains the latest vertical scroll offset for each scroll group.
        /// </summary>
        private static readonly Dictionary<string, double> VerticalScrollOffsets = new Dictionary<string, double>();

        /// <summary>
        /// Sets the value of the attached property ScrollGroup.
        /// </summary>
        /// <param name="obj">Object on which the property should be applied.</param>
        /// <param name="scrollGroup">Value of the property.</param>
        public static void SetScrollGroup(DependencyObject obj, string scrollGroup)
        {
            obj.SetValue(ScrollGroupProperty, scrollGroup);
        }

        /// <summary>
        /// Gets the value of the attached property ScrollGroup.
        /// </summary>
        /// <param name="obj">Object for which the property should be read.</param>
        /// <returns>Value of the property StartTime</returns>
        public static string GetScrollGroup(DependencyObject obj)
        {
            return (string)obj.GetValue(ScrollGroupProperty);
        }

        /// <summary>
        /// Occurs, when the ScrollGroupProperty has changed.
        /// </summary>
        /// <param name="d">The DependencyObject on which the property has changed value.</param>
        /// <param name="e">Event data that is issued by any event that tracks changes to the effective value of this property.</param>
        private static void OnScrollGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = d as ScrollViewer;
            if (scrollViewer != null)
            {
                if (!string.IsNullOrEmpty((string)e.OldValue))
                {
                    // Remove scrollviewer
                    if (ScrollViewers.ContainsKey(scrollViewer))
                    {
#if SILVERLIGHT
						horizontalScrollBars.Remove(horizontalScrollBars.First(s => s.Value == scrollViewer).Key);
						verticalScrollBars.Remove(verticalScrollBars.First(s => s.Value == scrollViewer).Key);
						scrollViewer.Loaded += new RoutedEventHandler(ScrollViewer_Loaded);
#else
                        scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
#endif
                        ScrollViewers.Remove(scrollViewer);
                    }
                }

                if (!string.IsNullOrEmpty((string)e.NewValue))
                {
                    // If group already exists, set scrollposition of new scrollviewer to the scrollposition of the group
                    if (HorizontalScrollOffsets.Keys.Contains((string)e.NewValue))
                    {
                        scrollViewer.ScrollToHorizontalOffset(HorizontalScrollOffsets[(string)e.NewValue]);
                    }
                    else
                    {
                        HorizontalScrollOffsets.Add((string)e.NewValue, scrollViewer.HorizontalOffset);
                    }

                    if (VerticalScrollOffsets.Keys.Contains((string)e.NewValue))
                    {
                        scrollViewer.ScrollToVerticalOffset(VerticalScrollOffsets[(string)e.NewValue]);
                    }
                    else
                    {
                        VerticalScrollOffsets.Add((string)e.NewValue, scrollViewer.VerticalOffset);
                    }

                    // Add scrollviewer
                    ScrollViewers.Add(scrollViewer, (string)e.NewValue);
#if !SILVERLIGHT
                    scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
#else
					scrollViewer.Loaded += new RoutedEventHandler(ScrollViewer_Loaded);
#endif
                }
            }
        }

#if !SILVERLIGHT
        /// <summary>
        /// Occurs, when the scroll offset of one scrollviewer has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">EventArgs of the event.</param>
        private static void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var changedScrollViewer = sender as ScrollViewer;
            if (e.VerticalChange != 0 && e.HorizontalChange != 0)
                Scroll(changedScrollViewer, true, true);
            else if (e.VerticalChange != 0)
                Scroll(changedScrollViewer, true, false);
            else if (e.HorizontalChange != 0)
                Scroll(changedScrollViewer, false, true);
        }
#endif

#if SILVERLIGHT
		/// <summary>
		/// Occurs, when the scroll viewer is loaded.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">EventArgs of the event.</param>
		private static void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
		{
			var scrollViewer = (ScrollViewer)sender;
			var group = scrollViewers[scrollViewer];
			scrollViewer.Opacity = 1;
			if (verticalScrollOffsets.Keys.Contains(group))
			{
				scrollViewer.ScrollToVerticalOffset(verticalScrollOffsets[group]);
			}

			scrollViewer.ApplyTemplate();

			var scrollViewerRoot = (FrameworkElement)VisualTreeHelper.GetChild(scrollViewer, 0);
			var horizontalScrollBar = (ScrollBar)scrollViewerRoot.FindName("HorizontalScrollBar");
			var verticalScrollBar = (ScrollBar)scrollViewerRoot.FindName("VerticalScrollBar");

			if (!horizontalScrollBars.Keys.Contains(horizontalScrollBar))
			{
				horizontalScrollBars.Add(horizontalScrollBar, scrollViewer);
			}

			if (!verticalScrollBars.Keys.Contains(verticalScrollBar))
			{
				verticalScrollBars.Add(verticalScrollBar, scrollViewer);
			}

			if (horizontalScrollBar != null)
			{
				horizontalScrollBar.Scroll += new ScrollEventHandler(HorizontalScrollBar_Scroll);
				horizontalScrollBar.ValueChanged +=
 new RoutedPropertyChangedEventHandler<double>(HorizontalScrollBar_ValueChanged);
			}

			if (verticalScrollBar != null)
			{
				verticalScrollBar.Scroll += new ScrollEventHandler(VerticalScrollBar_Scroll);
				verticalScrollBar.ValueChanged +=
 new RoutedPropertyChangedEventHandler<double>(VerticalScrollBar_ValueChanged);
			}
		}

		/// <summary>
		/// Occurs, when the horizontal scroll bar was moved.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">EventArgs of the event.</param>
		private static void HorizontalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			var changedScrollBar = sender as ScrollBar;
			var changedScrollViewer = horizontalScrollBars[changedScrollBar];
			Scroll(changedScrollViewer, false, true);
		}

		/// <summary>
		/// Occurs, when the vertical scroll bar was moved.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">EventArgs of the event.</param>
		private static void VerticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			var changedScrollBar = sender as ScrollBar;
			var changedScrollViewer = verticalScrollBars[changedScrollBar];
			Scroll(changedScrollViewer, true, false);
		}

		/// <summary>
		/// Occurs, when the horizontal scroll bar was moved.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">EventArgs of the event.</param>
		private static void HorizontalScrollBar_Scroll(object sender, ScrollEventArgs e)
		{
			var changedScrollBar = sender as ScrollBar;
			var changedScrollViewer = horizontalScrollBars[changedScrollBar];
			Scroll(changedScrollViewer, false, true);
		}

		/// <summary>
		/// Occurs, when the vertical scroll bar was moved.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">EventArgs of the event.</param>
		private static void VerticalScrollBar_Scroll(object sender, ScrollEventArgs e)
		{
			var changedScrollBar = sender as ScrollBar;
			var changedScrollViewer = verticalScrollBars[changedScrollBar];
			Scroll(changedScrollViewer, true, false);
		}
#endif

        /// <summary>
        /// Scrolls all scroll viewers of a group to the position of the selected scroll viewer.
        /// </summary>
        /// <param name="changedScrollViewer">Sroll viewer, that specifies the current position of the group.</param>
        private static void Scroll(ScrollViewer changedScrollViewer, bool verticalChange, bool horizontalChange)
        {
            var group = ScrollViewers[changedScrollViewer];
            VerticalScrollOffsets[group] = changedScrollViewer.VerticalOffset;
            HorizontalScrollOffsets[group] = changedScrollViewer.HorizontalOffset;

            foreach (var scrollViewer in ScrollViewers.Where(s => s.Value == group && s.Key != changedScrollViewer))
            {
                if (verticalChange && scrollViewer.Key.VerticalOffset != changedScrollViewer.VerticalOffset)
                {
                    scrollViewer.Key.ScrollToVerticalOffset(changedScrollViewer.VerticalOffset);
                }

                if (horizontalChange && scrollViewer.Key.HorizontalOffset != changedScrollViewer.HorizontalOffset)
                {
                    scrollViewer.Key.ScrollToHorizontalOffset(changedScrollViewer.HorizontalOffset);
                }
            }
        }
    }
}