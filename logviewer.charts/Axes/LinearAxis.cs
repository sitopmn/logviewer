using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace logviewer.charts
{
    public class LinearAxis : Axis
    {
        static LinearAxis()
        {
            OrientationProperty.OverrideMetadata(typeof(LinearAxis), new FrameworkPropertyMetadata((d, e) =>
            {
                ((LinearAxis)d)._orientationCache = (AxisOrientation)e.NewValue;
                ScalingPropertyChanged(d, e);
            }));
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(LinearAxis), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender, ScalingPropertyChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(LinearAxis), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender, ScalingPropertyChanged));

        public static readonly DependencyProperty TickFormatProperty =
            DependencyProperty.Register("TickFormat", typeof(object), typeof(LinearAxis), new FrameworkPropertyMetadata("{0}", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsMoveableProperty =
            DependencyProperty.Register("IsMoveable", typeof(bool), typeof(LinearAxis), new PropertyMetadata(false));

        public static readonly DependencyProperty IsScaleableProperty =
            DependencyProperty.Register("IsScaleable", typeof(bool), typeof(LinearAxis), new PropertyMetadata(false));

        public static readonly DependencyProperty AutoScaleMarginProperty =
            DependencyProperty.Register("AutoScaleMargin", typeof(double), typeof(LinearAxis), new PropertyMetadata(0.0, ScalingPropertyChanged));

        public static readonly DependencyProperty LabelsProperty =
            DependencyProperty.Register("Labels", typeof(IList), typeof(LinearAxis), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (d, e) =>
            {
                ((LinearAxis)d)._labelsCache = e.NewValue as IList;
                ScalingPropertyChanged(d, e);
            }));

        public static readonly DependencyProperty ShowLabelsProperty =
            DependencyProperty.Register("ShowLabels", typeof(bool), typeof(LinearAxis), new FrameworkPropertyMetadata(true, ShowLabelPropertyChanged));

        public static readonly DependencyProperty LabelOpacityProperty =
            DependencyProperty.Register("LabelOpacity", typeof(double), typeof(LinearAxis), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));
        
        private double? _startPosition;

        private double _startMinimum;

        private double _startMaximum;

        private double _actualMinimum = 0;

        private double _actualMaximum = 1;

        private bool _longTicks = false;

        private double _axisOffset;

        private IList _labelsCache;

        private AxisOrientation _orientationCache;

        public LinearAxis()
        {
        }

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public bool IsMoveable
        {
            get { return (bool)GetValue(IsMoveableProperty); }
            set { SetValue(IsMoveableProperty, value); }
        }

        public bool IsScaleable
        {
            get { return (bool)GetValue(IsScaleableProperty); }
            set { SetValue(IsScaleableProperty, value); }
        }

        public double AutoScaleMargin
        {
            get { return (double)GetValue(AutoScaleMarginProperty); }
            set { SetValue(AutoScaleMarginProperty, value); }
        }

        public object TickFormat
        {
            get { return (object)GetValue(TickFormatProperty); }
            set { SetValue(TickFormatProperty, value); }
        }

        public IList Labels
        {
            get { return (IList)GetValue(LabelsProperty); }
            set { SetValue(LabelsProperty, value); }
        }

        public bool ShowLabels
        {
            get { return (bool)GetValue(ShowLabelsProperty); }
            set { SetValue(ShowLabelsProperty, value); }
        }

        public double LabelOpacity
        {
            get { return (double)GetValue(LabelOpacityProperty); }
            set { SetValue(LabelOpacityProperty, value); }
        }

        public override double GetItemPixel(object item)
        {
            try
            {
                double numeric = GetItemValue(item);
                if (_orientationCache == AxisOrientation.Horizontal)
                {
                    return (numeric - _actualMinimum) * (ActualWidth - _axisOffset) / (_actualMaximum - _actualMinimum);
                }
                else
                {
                    return (ActualHeight - _axisOffset) - (numeric - _actualMinimum) * (ActualHeight - _axisOffset) / (_actualMaximum - _actualMinimum);
                }
            }
            catch
            {
                return double.NaN;
            }
        }

        public override object GetPixelItem(double pixel)
        {
            if (Orientation == AxisOrientation.Horizontal)
            {
                return pixel * (_actualMaximum - _actualMinimum) / (ActualWidth - _axisOffset) + _actualMinimum;
            }
            else
            {
                return (ActualHeight - pixel) * (_actualMaximum - _actualMinimum) / (ActualHeight - _axisOffset) + _actualMinimum;
            }
        }

        public override string GetItemLabel(object item)
        {
            if (Labels != null && Labels.Contains(item))
            {
                return item.ToString();
            }
            else
            {
                return FormatTick(GetItemValue(item));
            }
        }

        public override bool OnChartMouseDown(MouseButtonEventArgs arg, Point position)
        {
            if (IsMoveable && arg.LeftButton == MouseButtonState.Pressed)
            {
                _startPosition = Orientation == AxisOrientation.Horizontal ? position.X : position.Y;
                _startMinimum = _actualMinimum;
                _startMaximum = _actualMaximum;
                return true;
            }

            return false;
        }

        public override bool OnChartMouseMove(MouseEventArgs arg, Point position)
        {
            if (_startPosition.HasValue)
            {
                var currentPosition = Orientation == AxisOrientation.Horizontal ? position.X : position.Y;
                var startValue = (double)GetPixelItem(_startPosition.Value);
                var currentValue = (double)GetPixelItem(currentPosition);
                var delta = currentValue - startValue;
                Minimum = _startMinimum - delta;
                Maximum = _startMaximum - delta;
                return true;
            }

            return false;
        }

        public override bool OnChartMouseUp(MouseButtonEventArgs arg, Point position)
        {
            if (_startPosition.HasValue)
            {
                _startPosition = null;
                return true;
            }

            return false;
        }

        public override bool OnChartMouseWheel(MouseWheelEventArgs arg, Point position)
        {
            if (IsScaleable)
            {
                var currentPosition = Orientation == AxisOrientation.Horizontal ? position.X : position.Y;
                var currentValue = (double)GetPixelItem(currentPosition);
                var currentSpan = _actualMaximum - _actualMinimum;

                var factor = (currentValue - _actualMinimum) / currentSpan;
                var span = currentSpan * (arg.Delta > 0 ? 0.9 : 1.1);
                Minimum = currentValue - span * factor;
                Maximum = currentValue + span * (1 - factor);
                return true;
            }

            return false;
        }

        internal void Scale()
        {
            if (double.IsNaN(Minimum) || double.IsNaN(Maximum))
            {
                try
                {
                    if (Labels != null)
                    {
                        var first = Labels.Cast<object>().FirstOrDefault();
                        var last = Labels.Cast<object>().LastOrDefault();
                        if (first != null && last != null)
                        {
                            _actualMinimum = 0;
                            _actualMaximum = GetItemValue(last);
                        }
                        else
                        {
                            _actualMinimum = double.IsNaN(Minimum) ? 0 : Minimum;
                            _actualMaximum = double.IsNaN(Maximum) ? 1 : Maximum;
                        }
                    }
                    else
                    {
                        var count = 0;
                        var activeSeries = Series
                            .Select(r => r as CartesianSeries)
                            .Where(s => s != null && s.Data != null && (Orientation == AxisOrientation.Horizontal ? s.AxisX == this : s.AxisY == this));
                        var limits = activeSeries
                            .SelectMany(s => s.Data.Select(p => Orientation == AxisOrientation.Horizontal ? GetItemValue(p.X) : GetItemValue(p.Y)))
                            .Aggregate(
                            new Tuple<double, double>(double.MaxValue, double.MinValue),
                            (l, i) =>
                            {
                                var minimum = l.Item1;
                                var maximum = l.Item2;
                                if (i < minimum)
                                {
                                    minimum = i;
                                }

                                if (i > maximum)
                                {
                                    maximum = i;
                                }

                                count += 1;

                                return new Tuple<double, double>(minimum, maximum);
                            });
                        if (count > 0)
                        {
                            var margin = (limits.Item2 - limits.Item1) * AutoScaleMargin;
                            _actualMinimum = double.IsNaN(Minimum) ? limits.Item1 - margin : Minimum;
                            _actualMaximum = double.IsNaN(Maximum) ? limits.Item2 + margin : Maximum;
                        }
                        else
                        {
                            _actualMinimum = double.IsNaN(Minimum) ? 0 : Minimum;
                            _actualMaximum = double.IsNaN(Maximum) ? 1 : Maximum;
                        }
                    }
                }
                catch
                {
                    _actualMinimum = 0;
                    _actualMaximum = 1;
                }
            }
            else
            {
                _actualMinimum = Minimum;
                _actualMaximum = Maximum;
            }

            InvalidateMeasure();
            RaiseUpdated();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            RaiseUpdated();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var pen = new Pen(Foreground, 1);
            Size tickText;
            int tickCount;

            if (Labels != null)
            {
                var label = Labels.Count > 0 ? Labels.Cast<string>().Aggregate((a, b) => a.Length > b.Length ? a : b) : string.Empty;
                var text = new FormattedText(label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Sans Serif"), 10, pen.Brush);
                tickText = new Size(text.Width, text.Height);
                tickCount = Labels.Count;
            }
            else
            {
                var tickText1 = new FormattedText(FormatTick(_actualMaximum), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Sans Serif"), 10, pen.Brush);
                var tickText2 = new FormattedText(FormatTick(_actualMinimum), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Sans Serif"), 10, pen.Brush);
                tickText = new Size(Math.Max(tickText1.Width, tickText2.Width), Math.Max(tickText1.Height, tickText2.Height));
                tickCount = 4;
            }

            if (Orientation == AxisOrientation.Horizontal)
            {
                if (Labels != null && tickCount * (tickText.Width + 5) > constraint.Width)
                {
                    _longTicks = true;
                    _axisOffset = tickText.Width * 0.8;
                    return new Size(Math.Min(constraint.Width, (tickText.Width * 0.8 + 5) * tickCount), Math.Min(5 + tickText.Width * 0.8, constraint.Height));
                }
                else
                {
                    _longTicks = false;
                    _axisOffset = 0;
                    return new Size(Math.Min(constraint.Width, (tickText.Width + 5) * tickCount), Math.Min(5 + tickText.Height, constraint.Height));
                }
            }
            else
            {
                return new Size(Math.Min(constraint.Width, tickText.Width + 10), Math.Min(constraint.Height, (tickText.Height + 5) * tickCount));
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var pen = new Pen(Foreground, 1);

            var labelBrush = Foreground.Clone();
            labelBrush.Opacity = LabelOpacity;

            drawingContext.DrawRectangle(Background, null, new Rect(0, 0, ActualWidth, ActualHeight));
            drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Left), new Point(0, 0), new Point(0, ActualHeight));
            drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Bottom), new Point(0, ActualHeight), new Point(ActualWidth, ActualHeight));
            drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Right), new Point(ActualWidth, ActualHeight), new Point(ActualWidth, 0));
            drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Top), new Point(ActualWidth, 0), new Point(0, 0));

            if (Orientation == AxisOrientation.Horizontal)
            {
                // draw the axis line
                drawingContext.DrawLine(pen, new Point(0, 0), new Point(ActualWidth - _axisOffset, 0));

                if (Labels != null)
                {
                    foreach (var label in Labels)
                    {
                        var tickText = new FormattedText(label.ToString(), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Sans Serif"), 10, labelBrush);
                        var x = GetItemPixel(label);
                        if (x >= 0 && x <= ActualWidth)
                        {
                            drawingContext.PushTransform(new TranslateTransform(x, 5));
                            drawingContext.PushTransform(new RotateTransform(_longTicks ? 45 : 0));
                            drawingContext.DrawText(tickText, new Point(0, 0));
                            drawingContext.Pop();
                            drawingContext.Pop();
                            drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, 0 + 5));
                        }
                    }
                }
                else
                { 
                    // calculate the maximum number of ticks
                    var tickText = new FormattedText(FormatTick(_actualMinimum), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Sans Serif"), 10, labelBrush);
                    var tickSpacing = CalculateTickSpacing(_actualMaximum - _actualMinimum, (int)Math.Floor(ActualWidth / (tickText.Width + ActualWidth * 0.1)));

                    // draw the ticks
                    if (!double.IsNaN(tickSpacing) && tickSpacing > 0.1)
                    {
                        var startTick = _actualMinimum - _actualMinimum % tickSpacing;
                        for (var tick = startTick; tick <= _actualMaximum; tick += tickSpacing)
                        {
                            tickText = new FormattedText(FormatTick(tick), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Sans Serif"), 10, labelBrush);
                            var x = GetItemPixel(tick);
                            if (x >= 0 && x <= ActualWidth)
                            {
                                drawingContext.DrawText(tickText, new Point(x - tickText.Width / 2, 0 + 5));
                                drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, 0 + 5));
                            }
                        }
                    }
                }

            }
            else
            {
                // draw the axis line
                drawingContext.DrawLine(pen, new Point(ActualWidth, 0), new Point(ActualWidth, ActualHeight - _axisOffset));

                if (Labels != null)
                {
                    foreach (var label in Labels)
                    {
                        var tickText = new FormattedText(label.ToString(), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Sans Serif"), 10, labelBrush);
                        var y = GetItemPixel(label);
                        if (y >= 0 && y <= ActualHeight)
                        {
                            drawingContext.DrawText(tickText, new Point(ActualWidth - tickText.Width - 10, y - tickText.Height / 2));
                            drawingContext.DrawLine(pen, new Point(ActualWidth - 5, y), new Point(ActualWidth, y));
                        }
                    }
                }
                else
                {
                    // calculate the maximum number of ticks
                    var tickText = new FormattedText(FormatTick(_actualMinimum), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Sans Serif"), 10, labelBrush);
                    var tickSpacing = CalculateTickSpacing(_actualMaximum - _actualMinimum, (int)Math.Floor(ActualHeight / (tickText.Height + ActualHeight * 0.1)));

                    // draw the ticks
                    if (!double.IsNaN(tickSpacing) && tickSpacing > 0.1)
                    {
                        var startTick = _actualMinimum - _actualMinimum % tickSpacing;
                        for (var tick = startTick; tick <= _actualMaximum; tick += tickSpacing)
                        {
                            tickText = new FormattedText(FormatTick(tick), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Sans Serif"), 10, labelBrush);
                            var y = GetItemPixel(tick);
                            if (y >= 0 && y <= ActualHeight)
                            {
                                drawingContext.DrawText(tickText, new Point(ActualWidth - tickText.Width - 10, y - tickText.Height / 2));
                                drawingContext.DrawLine(pen, new Point(ActualWidth - 5, y), new Point(ActualWidth, y));
                            }
                        }
                    }
                }
            }
        }

        private static void ScalingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LinearAxis axis)
            {
                axis.Scale();
            }
        }

        private static void ShowLabelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LinearAxis axis)
            {
                if (axis.ShowLabels)
                {
                    axis.BeginAnimation(LabelOpacityProperty, new DoubleAnimation(1.0, new Duration(TimeSpan.FromMilliseconds(100))));
                }
                else
                {
                    axis.BeginAnimation(LabelOpacityProperty, new DoubleAnimation(0.05, new Duration(TimeSpan.FromMilliseconds(100))));
                }
            }
        }

        private double GetItemValue(object item)
        {
            if (_labelsCache != null && _labelsCache.Contains(item))
            {
                return _labelsCache.IndexOf(item) + 0.5;
            }
            else
            {
                if (item is string str)
                {
                    double result;
                    if (double.TryParse(str, out result))
                    {
                        return result;
                    }
                    else
                    {
                        return default(double);
                    }
                }
                else
                {
                    return Convert.ToDouble(item);
                }
            }
        }

        private string FormatTick(double tick)
        {
            if (TickFormat is string formatString)
            {
                return string.Format(formatString, tick);
            }
            else if (TickFormat is Func<double, string> formatFunc)
            {
                return formatFunc(tick);
            }
            else
            {
                return tick.ToString();
            }
        }

        private double CalculateTickSpacing(double range, int maximumTickCount)
        {
            var minimum = range / maximumTickCount;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(minimum)));
            var residual = minimum / magnitude;
            if (residual > 4)
            {
                return 8 * magnitude;
            }
            else if (residual > 2)
            {
                return 4 * magnitude;
            }
            else if (residual > 1)
            {
                return 2 * magnitude;
            }
            else
            {
                return magnitude;
            }
        }
    }
}
