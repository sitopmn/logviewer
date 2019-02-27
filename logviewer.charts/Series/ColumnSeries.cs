using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace logviewer.charts
{
    public class ColumnSeries : CartesianSeries
    {
        public static readonly DependencyProperty ColumnWidthProperty =
            DependencyProperty.Register("ColumnWidth", typeof(double), typeof(ColumnSeries), new PropertyMetadata(double.NaN));

        static ColumnSeries()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColumnSeries), new FrameworkPropertyMetadata(typeof(ColumnSeries)));
        }

        public double ColumnWidth
        {
            get { return (double)GetValue(ColumnWidthProperty); }
            set { SetValue(ColumnWidthProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // fill the background
            if (Background != null)
            {
                drawingContext.DrawRectangle(Background, null, new Rect(0, 0, ActualWidth, ActualHeight));
            }

            // draw the border
            if (BorderBrush != null)
            {
                drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Left), new Point(0, 0), new Point(0, ActualHeight));
                drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Bottom), new Point(0, ActualHeight), new Point(ActualWidth, ActualHeight));
                drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Right), new Point(ActualWidth, ActualHeight), new Point(ActualWidth, 0));
                drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Top), new Point(ActualWidth, 0), new Point(0, 0));
            }

            // draw the chart
            if (Data != null && AxisX != null && AxisY != null)
            {
                var axisX = AxisX;
                var axisY = AxisY;
                var foreground = Foreground;

                // set the clipping
                drawingContext.PushClip(new RectangleGeometry(new Rect(0, -5, ActualWidth, ActualHeight + 10)));

                // draw the points
                var previousX = 0.0;
                var previousY = 0.0;
                var first = true;
                foreach (var current in Data)
                {
                    var currentX = axisX.GetItemPixel(current.X);
                    var currentY = axisY.GetItemPixel(current.Y);
                    if (double.IsNaN(currentX) || double.IsNaN(currentY)) return;

                    if (currentX >= 0 && currentX <= ActualWidth)
                    {
                        var width = GetColumnWidth(first ? new Point?() : new Point(previousX, previousY), new Point(currentX, currentY));
                        drawingContext.DrawRectangle(foreground, null, new Rect(currentX - width / 2, currentY, width, ActualHeight - currentY));
                    }

                    previousX = currentX;
                    previousY = currentY;
                    first = false;
                }

                drawingContext.Pop();
            }
        }

        private double GetColumnWidth(Point? previous, Point current)
        {
            if (double.IsNaN(ColumnWidth))
            {
                if (AxisX is LinearAxis linearAxis)
                {
                    if (linearAxis.Labels != null)
                    {
                        return Math.Max(0, linearAxis.ActualWidth / linearAxis.Labels.Count - 2);
                    }
                    else if (Data is IReadOnlyCollection<DataPoint> col)
                    {
                        return Math.Max(0, linearAxis.ActualWidth / col.Count - 2);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                return ColumnWidth;
            }
        }
    }
}
