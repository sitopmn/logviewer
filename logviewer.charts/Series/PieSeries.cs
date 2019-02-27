using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace logviewer.charts
{
    public class PieSeries : Series
    {
        static PieSeries()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PieSeries), new FrameworkPropertyMetadata(typeof(PieSeries)));
        }

        private readonly Dictionary<Color, Brush> _brushes = new Dictionary<Color, Brush>();

        public PieSeries()
        {
            MinWidth = 200;
        }

        protected override void OnDataChanged()
        {
            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var size = new Size(0, 0);

            // convert data for display
            var converted = Data.Select(ConvertData).ToList();
            var first = converted.FirstOrDefault();
            if (first != null)
            {
                double total = converted.Sum(c => c.Item2);
                double current = 0;

                var leftLabelCount = 0;
                var rightLabelCount = 0;
                var labelWidth = 0.0;
                var labelHeight = 0.0;

                // draw the slices and calculate the label positions
                foreach (var c in converted)
                {
                    if (current >= total * 0.75 || current <= total * 0.25)
                    {
                        rightLabelCount += 1;
                    }
                    else
                    {
                        leftLabelCount += 1;
                    }

                    var label = new FormattedText(c.Item1, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI, Lucida Sans Unicode, Verdana"), 12, GetBrush(c.Item3));
                    labelWidth = Math.Max(labelWidth, label.Width);
                    labelHeight = Math.Max(labelHeight, label.Height);

                    current += c.Item2;
                }

                size.Width = (labelWidth + 20) * 2 + 100;
                size.Height = Math.Max(leftLabelCount, rightLabelCount) * (labelHeight + 10);
            }

            return size;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));

            var center = new Point(ActualWidth / 2, ActualHeight / 2);
            drawingContext.DrawRectangle(Background, null, new Rect(0, 0, ActualWidth, ActualHeight));
            drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Left), new Point(0, 0), new Point(0, ActualHeight));
            drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Left), new Point(0, ActualHeight), new Point(ActualWidth, ActualHeight));
            drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Left), new Point(ActualWidth, ActualHeight), new Point(ActualWidth, 0));
            drawingContext.DrawLine(new Pen(BorderBrush, BorderThickness.Left), new Point(ActualWidth, 0), new Point(0, 0));

            // convert data for display
            var converted = Data.Select(ConvertData).ToList();
            var first = converted.FirstOrDefault();
            if (first != null)
            {
                var longestLabel = converted.Skip(1).Aggregate(converted.FirstOrDefault(), (a, b) => a.Item1.Length > b.Item1.Length ? a : b);
                var longestFormattedLabel = new FormattedText(longestLabel.Item1, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI, Lucida Sans Unicode, Verdana"), 12, GetBrush(longestLabel.Item3));
                var pieRadius = Math.Max(100, Math.Min(ActualWidth - longestFormattedLabel.Width * 2, ActualHeight)) / 2;
                var rect = new Rect(center.X - pieRadius, center.Y - pieRadius, pieRadius * 2, pieRadius * 2);
                double total = converted.Sum(c => c.Item2);
                double current = 0;
                var labels = new List<Label>();

                // draw the slices and calculate the label positions
                foreach (var c in converted)
                {
                    var amount = c.Item2;
                    var startRadians = (current * 360 / (double)total) * Math.PI / 180.0;
                    var sweepRadians = (amount * 360 / (double)total) * Math.PI / 180.0;

                    // take care of a full circle...
                    if (Math.Abs(sweepRadians - 360 * Math.PI / 180.0) < 0.001)
                    {
                        sweepRadians -= 0.001;
                    }

                    // draw the slice
                    var arc = CreateArcGeometry(rect, startRadians, sweepRadians);
                    drawingContext.DrawGeometry(GetBrush(c.Item3), null, arc);

                    // store the anchor point for the label on the pie
                    var xa = rect.X + rect.Width / 2 + (Math.Cos(startRadians + sweepRadians / 2) * rect.Width / 2);
                    var ya = rect.Y + rect.Height / 2 + (Math.Sin(startRadians + sweepRadians / 2) * rect.Height / 2);
                    labels.Add(new Label() { Category = c, Anchor = new Point(xa, ya), LabelY = ya });

                    current += amount;
                }

                // rearrange label positions to avoid overlapping labels
                var temp = new FormattedText(first.Item1, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI, Lucida Sans Unicode, Verdana"), 12, GetBrush(first.Item3));
                ArrangeLabels(labels.Where(l => l.Anchor.X < rect.Left + rect.Width / 2).OrderBy(l => l.LabelY).ToList(), temp.Height + 10);
                ArrangeLabels(labels.Where(l => l.Anchor.X >= rect.Left + rect.Width / 2).OrderBy(l => l.LabelY).ToList(), temp.Height + 10);

                // draw labels
                foreach (var l in labels)
                {
                    var amount = l.Category.Item2;
                    var label = new FormattedText(l.Category.Item1, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI, Lucida Sans Unicode, Verdana"), 12, GetBrush(l.Category.Item3));
                    if (l.Anchor.X < rect.Left + rect.Width / 2)
                    {
                        drawingContext.DrawLine(new Pen(GetBrush(l.Category.Item3), 1), new Point(center.X - pieRadius - 10 - label.Width, l.LabelY), new Point(center.X - pieRadius - 10, l.LabelY));
                        drawingContext.DrawLine(new Pen(GetBrush(l.Category.Item3), 1), l.Anchor, new Point(center.X - pieRadius - 10, l.LabelY));
                        drawingContext.DrawText(label, new Point(center.X - pieRadius - 10 - label.Width, l.LabelY - label.Height - 3));
                    }
                    else
                    {
                        drawingContext.DrawLine(new Pen(GetBrush(l.Category.Item3), 1), l.Anchor, new Point(center.X + pieRadius + 10, l.LabelY));
                        drawingContext.DrawLine(new Pen(GetBrush(l.Category.Item3), 1), new Point(center.X + pieRadius + 10 + label.Width, l.LabelY), new Point(center.X + pieRadius + 10, l.LabelY));
                        drawingContext.DrawText(label, new Point(center.X + pieRadius + 10, l.LabelY - label.Height - 3));
                    }
                }
            }

            drawingContext.Pop();
        }

        private Tuple<string, double, Color> ConvertData(DataPoint d)
        {
            var amount = 0.0;
            try
            {
                amount = Convert.ToDouble(d.Y, CultureInfo.InvariantCulture);
            }
            catch
            { }

            var formatted = string.Format("{0} [{1}]", d.X.ToString(), amount);

            return new Tuple<string, double, Color>(formatted, amount, d.Color);
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            InvalidateVisual();
        }

        /// <summary>
        /// Arranges labels in the given list
        /// </summary>
        /// <param name="labels">List of labels to arrange. The list must be sorted by y position</param>
        private void ArrangeLabels(List<Label> labels, double minimumSeparation)
        {
            // walk the labels top -> bottom
            for (var i = 0; i < labels.Count; i++)
            {
                // if the label is above the clip area move it down
                if (labels[i].LabelY - minimumSeparation < 0)
                {
                    labels[i].LabelY = minimumSeparation;
                }

                // if the label overlaps with the label above
                if (i > 0 && labels[i].LabelY - minimumSeparation < labels[i - 1].LabelY)
                {
                    // move it 20px below the label above
                    labels[i].LabelY = labels[i - 1].LabelY + minimumSeparation;

                    // if the label moves out of the control
                    if (labels[i].LabelY >= ActualHeight)
                    {
                        // move it to the bottom of the control
                        labels[i].LabelY = ActualHeight - 1;

                        // and move labels above up to make some room for the bottom label
                        for (var j = i - 1; j >= 0; j--)
                        {
                            if (labels[j].LabelY > labels[j + 1].LabelY - minimumSeparation)
                            {
                                labels[j].LabelY = labels[j + 1].LabelY - minimumSeparation;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets a brush for the given color
        /// </summary>
        /// <param name="color">The color for the brush</param>
        /// <returns>A brush with the given color</returns>
        private Brush GetBrush(Color color)
        {
            if (!_brushes.ContainsKey(color))
            {
                _brushes[color] = new SolidColorBrush(color);
                _brushes[color].Freeze();
            }

            return _brushes[color];
        }

        /// <summary>
        /// Create an Arc geometry drawing of an ellipse or circle
        /// </summary>
        /// <param name="rect">Box to hold the whole ellipse described by the arc</param>
        /// <param name="startDegrees">Start angle of the arc degrees within the ellipse. 0 degrees is a line to the right.</param>
        /// <param name="sweepDegrees">Sweep angle, -ve = Counterclockwise, +ve = Clockwise</param>
        /// <returns>GeometryDrawing object</returns>
        private Geometry CreateArcGeometry(Rect rect, double startRadians, double sweepRadians)
        {
            // x and y radius
            double dx = rect.Width / 2;
            double dy = rect.Height / 2;

            // determine the start point 
            double xs = rect.X + dx + (Math.Cos(startRadians) * dx);
            double ys = rect.Y + dy + (Math.Sin(startRadians) * dy);

            // determine the end point 
            double xe = rect.X + dx + (Math.Cos(startRadians + sweepRadians) * dx);
            double ye = rect.Y + dy + (Math.Sin(startRadians + sweepRadians) * dy);

            // draw the arc into a stream geometry
            StreamGeometry streamGeom = new StreamGeometry();
            using (StreamGeometryContext ctx = streamGeom.Open())
            {
                bool isLargeArc = Math.Abs(sweepRadians) > Math.PI;
                SweepDirection sweepDirection = sweepRadians < 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;

                ctx.BeginFigure(new Point(dx + rect.Left, dy + rect.Top), true, true);
                ctx.LineTo(new Point(xs, ys), false, true);
                ctx.ArcTo(new Point(xe, ye), new Size(dx, dy), 0, isLargeArc, sweepDirection, true, true);
                ctx.LineTo(new Point(dx + rect.Left, dy + rect.Top), false, true);
            }

            return streamGeom;
        }

        private class Label
        {
            public Tuple<string, double, Color> Category;
            public Point Anchor;
            public double LabelY;
        }
    }
}
