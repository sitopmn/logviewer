using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace logviewer.charts
{ 
    public abstract class CartesianSeries : Series
    {
        public static readonly DependencyProperty AxisXProperty =
            DependencyProperty.Register("AxisX", typeof(Axis), typeof(CartesianSeries), new PropertyMetadata(null, (d, e) =>
            {
                if (d is CartesianSeries chart)
                {
                    if (e.NewValue is Axis newAxis)
                    {
                        newAxis.Updated += chart.OnAxisChanged;
                        newAxis.AddSeries(chart);
                    }

                    if (e.OldValue is Axis oldAxis)
                    {
                        oldAxis.Updated -= chart.OnAxisChanged;
                        oldAxis.RemoveSeries(chart);
                    }

                    chart.OnDataChanged();
                }
            }));

        public static readonly DependencyProperty AxisYProperty =
            DependencyProperty.Register("AxisY", typeof(Axis), typeof(CartesianSeries), new PropertyMetadata(null, (d, e) =>
            {
                if (d is CartesianSeries chart)
                {
                    if (e.NewValue is Axis newAxis)
                    {
                        newAxis.Updated += chart.OnAxisChanged;
                        newAxis.AddSeries(chart);
                    }

                    if (e.OldValue is Axis oldAxis)
                    {
                        oldAxis.Updated -= chart.OnAxisChanged;
                        oldAxis.RemoveSeries(chart);
                    }

                    chart.OnDataChanged();
                }
            }));

        private readonly Dictionary<Color, Brush> _brushes = new Dictionary<Color, Brush>();

        public CartesianSeries()
        {
            Loaded += LoadedHandler;
            Unloaded += UnloadedHandler;
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            if (AxisX != null)
            {
                AxisX.AddSeries(this);
            }

            if (AxisY != null)
            {
                AxisY.AddSeries(this);
            }

            OnDataChanged();
        }

        private void UnloadedHandler(object sender, RoutedEventArgs e)
        {
            if (AxisX != null)
            {
                AxisX.RemoveSeries(this);
            }

            if (AxisY != null)
            {
                AxisY.RemoveSeries(this);
            }
        }

        public Axis AxisX
        {
            get { return (Axis)GetValue(AxisXProperty); }
            set { SetValue(AxisXProperty, value); }
        }

        public Axis AxisY
        {
            get { return (Axis)GetValue(AxisYProperty); }
            set { SetValue(AxisYProperty, value); }
        }

        public virtual DataPoint GetPixelPoint(double x)
        {
            if (Data != null && Data.Count() > 0)
            {
                return Data.Aggregate((a, i) =>
                {
                    var da = Math.Abs(x - AxisX.GetItemPixel(a.X));
                    var di = Math.Abs(x - AxisX.GetItemPixel(i.X));
                    return da < di ? a : i;
                });
            }
            else
            {
                return null;
            }
        }

        public virtual DataPoint GetPixelPoint(double x, double y)
        {
            if (Data != null && Data.Count() > 0)
            {
                return Data.Aggregate((a, i) =>
                {
                    var dax = x - AxisX.GetItemPixel(a.X);
                    var day = y - AxisY.GetItemPixel(a.Y);
                    var da = Math.Sqrt(dax * dax + day * day);
                    var dix = x - AxisX.GetItemPixel(i.X);
                    var diy = y - AxisY.GetItemPixel(i.Y);
                    var di = Math.Sqrt(dix * dix + diy * diy);
                    return da < di ? a : i;
                });
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a brush for the given color
        /// </summary>
        /// <param name="color">The color for the brush</param>
        /// <returns>A brush with the given color</returns>
        protected Brush GetBrush(Color color)
        {
            if (!_brushes.ContainsKey(color))
            {
                _brushes[color] = new SolidColorBrush(color);
                _brushes[color].Freeze();
            }

            return _brushes[color];
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
        }

        protected virtual void OnAxisChanged()
        {
            InvalidateVisual();
        }

        protected override void OnDataChanged()
        {
            if (AxisX is LinearAxis lx) lx.Scale();
            if (AxisY is LinearAxis ly) ly.Scale();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            if (AxisX != null && AxisX.OnChartMouseDown(e, position))
            {
                e.Handled = true;
            }

            if (AxisY != null && AxisY.OnChartMouseDown(e, position))
            {
                e.Handled = true;
            }

            if (e.Handled)
            {
                InvalidateVisual();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            if (AxisX != null && AxisX.OnChartMouseMove(e, position))
            {
                e.Handled = true;
            }

            if (AxisY != null && AxisY.OnChartMouseMove(e, position))
            {
                e.Handled = true;
            }

            if (e.Handled)
            {
                InvalidateVisual();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            if (AxisX != null && AxisX.OnChartMouseUp(e, position))
            {
                e.Handled = true;
            }

            if (AxisY != null && AxisY.OnChartMouseUp(e, position))
            {
                e.Handled = true;
            }

            if (e.Handled)
            {
                InvalidateVisual();
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var position = e.GetPosition(this);
            if (AxisX != null && AxisX.OnChartMouseWheel(e, position))
            {
                e.Handled = true;
            }

            if (AxisY != null && AxisY.OnChartMouseWheel(e, position))
            {
                e.Handled = true;
            }

            if (e.Handled)
            {
                InvalidateVisual();
            }

            base.OnMouseWheel(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            AxisX?.OnChartMouseUp(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left), e.GetPosition(this));
            AxisY?.OnChartMouseUp(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left), e.GetPosition(this));
        }

        private void OnAxisChanged(object sender, EventArgs e)
        {
            OnAxisChanged();
        }
    }
}
