using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace logviewer.core
{
    public static class ScrollViewerExtensions
    {
        #region vertical offset binding 

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerExtensions), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, VerticalOffsetPropertyChangedHandler));

        private static readonly DependencyProperty VerticalScrollBarProperty =
            DependencyProperty.RegisterAttached("VerticalScollBar", typeof(ScrollBar), typeof(ScrollViewerExtensions), new PropertyMetadata(null));

        public static double GetVerticalOffset(DependencyObject obj)
        {
            return (double)obj.GetValue(VerticalOffsetProperty);
        }

        public static void SetVerticalOffset(DependencyObject obj, double value)
        {
            obj.SetValue(VerticalOffsetProperty, value);
        }

        private static void VerticalOffsetPropertyChangedHandler(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = d as ScrollViewer ?? d.GetVisualChild<ScrollViewer>();
            if (viewer == null)
            { 
                return;
            }

            var bar = d.GetValue(VerticalScrollBarProperty) as ScrollBar;
            if (bar == null)
            {
                bar = viewer.GetVisualChild<ScrollBar>(s => s.Orientation == Orientation.Vertical);
                if (bar == null) return;
                d.SetValue(VerticalScrollBarProperty, bar);
                bar.ValueChanged += (s, e2) => d.SetValue(VerticalOffsetProperty, e2.NewValue);
            }

            if (!double.IsNaN(GetVerticalOffset(d)))
            {
                viewer.ScrollToVerticalOffset(GetVerticalOffset(d));
            }
        }

        #endregion
    }
}
