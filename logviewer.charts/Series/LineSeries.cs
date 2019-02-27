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
    public class LineSeries : CartesianSeries
    {
        static LineSeries()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LineSeries), new FrameworkPropertyMetadata(typeof(LineSeries)));
        }

        public static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register("LineThickness", typeof(double), typeof(LineSeries), new PropertyMetadata(1d, (s, e) =>
            {
                if (s is LineSeries chart)
                {
                    chart._pen = null;
                    chart.InvalidateVisual();
                }
            }));
        
        private Pen _pen;

        public double LineThickness
        {
            get { return (double)GetValue(LineThicknessProperty); }
            set { SetValue(LineThicknessProperty, value); }
        }
        
        public override DataPoint GetPixelPoint(double x, double y)
        {
            return base.GetPixelPoint(x);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // define the pen for drawing
            if (_pen == null || _pen.Brush != Foreground)
            {
                _pen = new Pen(Foreground, LineThickness);
                _pen.Freeze();
            }

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

                // set the clipping
                drawingContext.PushClip(new RectangleGeometry(new Rect(0, -5, ActualWidth, ActualHeight + 10)));

                var previous = Data.FirstOrDefault();
                var previousX = 0.0;
                var previousY = 0.0;
                if (previous != null)
                {
                    previousX = axisX.GetItemPixel(previous.X);
                    previousY = axisY.GetItemPixel(previous.Y);
                    var geometry = new StreamGeometry();
                    var started = false;
                    using (var context = geometry.Open())
                    {
                        foreach (var current in Data.Skip(1))
                        {
                            var currentX = axisX.GetItemPixel(current.X);
                            var currentY = axisY.GetItemPixel(current.Y);
                            if (double.IsNaN(currentX) || double.IsNaN(currentY) || double.IsNaN(previousX) || double.IsNaN(previousY)) return;

                            if (currentX >= 0 && !started)
                            {
                                started = true;
                                context.BeginFigure(new Point(previousX, previousY), false, false);
                            }

                            if (started && currentX >= 0 && previousX < ActualWidth)
                            {
                                context.LineTo(new Point(currentX, currentY), true, false);
                            }
                            previousX = currentX;
                            previousY = currentY;
                        }
                    }

                    drawingContext.DrawGeometry(_pen.Brush, _pen, geometry);
                }

                drawingContext.Pop();
            }
        }
    }
}
