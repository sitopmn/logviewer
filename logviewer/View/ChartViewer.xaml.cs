using logviewer.charts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace logviewer.View
{
    /// <summary>
    /// Interaktionslogik für ChartViewer.xaml
    /// </summary>
    public partial class ChartViewer : UserControl
    {
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="CursorVisible"/> property
        /// </summary>
        public static readonly DependencyProperty CursorVisibleProperty =
            DependencyProperty.Register("CursorVisible", typeof(bool), typeof(ChartViewer), new PropertyMetadata(false));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="CursorPositionX"/> property
        /// </summary>
        public static readonly DependencyProperty CursorPositionXProperty =
            DependencyProperty.Register("CursorPositionX", typeof(double), typeof(ChartViewer), new PropertyMetadata(0.0));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="CursorLabelX"/> property
        /// </summary>
        public static readonly DependencyProperty CursorLabelXProperty =
            DependencyProperty.Register("CursorLabelX", typeof(string), typeof(ChartViewer), new PropertyMetadata(string.Empty));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="CursorLabelY"/> property
        /// </summary>
        public static readonly DependencyProperty CursorLabelYProperty =
            DependencyProperty.Register("CursorLabelY", typeof(string), typeof(ChartViewer), new PropertyMetadata(string.Empty));
        
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="CursorPositionY"/> property
        /// </summary>
        public static readonly DependencyProperty CursorPositionYProperty =
            DependencyProperty.Register("CursorPositionY", typeof(double), typeof(ChartViewer), new PropertyMetadata(0.0));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="PointSelectedCommand"/> property
        /// </summary>
        public static readonly DependencyProperty PointSelectedCommandProperty =
            DependencyProperty.Register("PointSelectedCommand", typeof(ICommand), typeof(ChartViewer), new PropertyMetadata(null));

        /// <summary>
        /// The X axis of the charts
        /// </summary>
        private LinearAxis XAxis;

        /// <summary>
        /// The canvas used for drawing the cursor
        /// </summary>
        private Canvas CursorCanvas;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChartViewer"/> class-
        /// </summary>
        public ChartViewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets a value indicating the cursor is visible
        /// </summary>
        public bool CursorVisible
        {
            get { return (bool)GetValue(CursorVisibleProperty); }
            set { SetValue(CursorVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the horizontal cursor position
        /// </summary>
        public double CursorPositionX
        {
            get { return (double)GetValue(CursorPositionXProperty); }
            set { SetValue(CursorPositionXProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical cursor position
        /// </summary>
        public double CursorPositionY
        {
            get { return (double)GetValue(CursorPositionYProperty); }
            set { SetValue(CursorPositionYProperty, value); }
        }

        /// <summary>
        /// Gets or sets the label for the horizontal cursor
        /// </summary>
        public string CursorLabelX
        {
            get { return (string)GetValue(CursorLabelXProperty); }
            set { SetValue(CursorLabelXProperty, value); }
        }

        /// <summary>
        /// Gets or sets the label for the vertical cursor
        /// </summary>
        public string CursorLabelY
        {
            get { return (string)GetValue(CursorLabelYProperty); }
            set { SetValue(CursorLabelYProperty, value); }
        }

        /// <summary>
        /// Gets or sets the command to select a point on the chart
        /// </summary>
        public ICommand PointSelectedCommand
        {
            get { return (ICommand)GetValue(PointSelectedCommandProperty); }
            set { SetValue(PointSelectedCommandProperty, value); }
        }

        /// <summary>
        /// Resets zoom and pan to their default values
        /// </summary>
        public void ResetZoomAndPan()
        {
            if (XAxis != null)
            {
                XAxis.Minimum = double.NaN;
                XAxis.Maximum = double.NaN;
            }
        }

        /// <summary>
        /// Fetches control instances from the control template
        /// </summary>
        public override void OnApplyTemplate()
        {
            XAxis = Template.FindName("XAxis", this) as LinearAxis;
            CursorCanvas = Template.FindName("CursorCanvas", this) as Canvas;
        }

        /// <summary>
        /// Handles mouse motions on the series for moving the cursor
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Argument of the event</param>
        private void Series_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is CartesianSeries series && XAxis != null)
            {
                var mousePosition = e.GetPosition(series);
                var mousePoint = series.GetPixelPoint(mousePosition.X, mousePosition.Y);
                if (mousePoint != null)
                {
                    var cursorPoint = new Point(series.AxisX.GetItemPixel(mousePoint.X), series.AxisY.GetItemPixel(mousePoint.Y));
                    var canvasPoint = series.TranslatePoint(cursorPoint, CursorCanvas);
                    CursorPositionX = canvasPoint.X;
                    CursorPositionY = canvasPoint.Y;
                    CursorLabelX = series.AxisX.GetItemLabel(mousePoint.X);
                    CursorLabelY = series.AxisY.GetItemLabel(mousePoint.Y);
                    SetCursorVisibility(series, true);
                }
                else
                {
                    SetCursorVisibility(series, false);
                }
            }
        }

        /// <summary>
        /// Hides the cursor when the series is left
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Argument of the event</param>
        private void Series_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is CartesianSeries series)
            {
                SetCursorVisibility(series, false);
            }
        }

        /// <summary>
        /// Sets the visibility of the cursor and adjusts visibility of axis labels accordingly
        /// </summary>
        /// <param name="series"></param>
        /// <param name="visible"></param>
        private void SetCursorVisibility(CartesianSeries series, bool visible)
        {
            if (visible)
            {
                CursorVisible = true;
                if (series.AxisX is LinearAxis x) x.ShowLabels = false;
                if (series.AxisY is LinearAxis y) y.ShowLabels = false;
            }
            else
            {
                CursorVisible = false;
                if (series.AxisX is LinearAxis x) x.ShowLabels = true;
                if (series.AxisY is LinearAxis y) y.ShowLabels = true;
            }
        }

        /// <summary>
        /// Handles a double click on a series
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Argument of the event</param>
        private void Series_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is CartesianSeries series && XAxis != null)
            {
                var mousePosition = e.GetPosition(series);
                var mousePoint = series.GetPixelPoint(mousePosition.X, mousePosition.Y);

                if (mousePoint != null && mousePoint.UserData != null && PointSelectedCommand != null && PointSelectedCommand.CanExecute(mousePoint.UserData))
                {
                    PointSelectedCommand.Execute(mousePoint.UserData);
                }
            }
        }
    }
}
