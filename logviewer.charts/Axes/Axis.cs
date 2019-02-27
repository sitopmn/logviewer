using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace logviewer.charts
{
    public abstract class Axis : Control
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(Axis), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(AxisOrientation), typeof(Axis), new FrameworkPropertyMetadata(AxisOrientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure));

        private readonly HashSet<Series> _series = new HashSet<Series>();

        public event EventHandler Updated;

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public AxisOrientation Orientation
        {
            get { return (AxisOrientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        protected List<Series> Series => _series.ToList();

        public void AddSeries(Series s)
        {
            _series.Add(s);
        }

        public void RemoveSeries(Series s)
        {
            _series.Remove(s);
        }

        public virtual bool OnChartMouseDown(MouseButtonEventArgs arg, Point position)
        {
            return false;
        }

        public virtual bool OnChartMouseUp(MouseButtonEventArgs arg, Point position)
        {
            return false;
        }

        public virtual bool OnChartMouseMove(MouseEventArgs arg, Point position)
        {
            return false;
        }

        public virtual bool OnChartMouseWheel(MouseWheelEventArgs arg, Point position)
        {
            return false;
        }

        public abstract double GetItemPixel(object item);

        public abstract object GetPixelItem(double pixel);

        public abstract string GetItemLabel(object item);

        protected void RaiseUpdated()
        {
            InvalidateVisual();
            Updated?.Invoke(this, EventArgs.Empty);
        }
    }
}
