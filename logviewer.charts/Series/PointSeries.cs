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
    public class PointSeries : CartesianSeries
    {
        static PointSeries()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PointSeries), new FrameworkPropertyMetadata(typeof(PointSeries)));
        }
        
        public static readonly DependencyProperty PointRadiusProperty =
            DependencyProperty.Register("PointRadius", typeof(double), typeof(PointSeries), new PropertyMetadata(2.0, (s, e) =>
            {
                if (s is LineSeries chart)
                {
                    chart.InvalidateVisual();
                }
            }));
        
        public double PointRadius
        {
            get { return (double)GetValue(PointRadiusProperty); }
            set { SetValue(PointRadiusProperty, value); }
        }

        public override DataPoint GetPixelPoint(double x, double y)
        {
            return base.GetPixelPoint(x, y);
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
                var radius = PointRadius * 2;

                // set the clipping
                drawingContext.PushClip(new RectangleGeometry(new Rect(0, -5, ActualWidth, ActualHeight + 10)));

                // draw the points
                foreach (var current in Data)
                {
                    var currentX = axisX.GetItemPixel(current.X);
                    var currentY = axisY.GetItemPixel(current.Y);
                    if (double.IsNaN(currentX) || double.IsNaN(currentY)) return;

                    if (currentX >= 0 && currentX <= ActualWidth)
                    {
                        drawingContext.DrawEllipse(foreground, null, new Point(currentX, currentY), radius, radius);
                    }
                }

                drawingContext.Pop();
            }
        }
    }
}
