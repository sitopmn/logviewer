using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace logviewer.charts
{
    public class SpectrogramSeries : CartesianSeries
    {
        public static readonly DependencyProperty ColorPaletteProperty =
            DependencyProperty.Register("ColorPalette", typeof(Palette), typeof(SpectrogramSeries), new PropertyMetadata(new Palette(20, Palette.DefaultHeatmap)));

        private WriteableBitmap _bitmap;
        
        private bool _isDrawing;

        private bool _isRequested;

        public Palette ColorPalette
        {
            get { return (Palette)GetValue(ColorPaletteProperty); }
            set { SetValue(ColorPaletteProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_bitmap == null || (int)ActualWidth != _bitmap.PixelWidth || (int)ActualHeight != _bitmap.PixelHeight)
            {
                _bitmap = new WriteableBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Bgra32, null);
            }

            if (_bitmap != null)
            {
                drawingContext.DrawDrawing(new ImageDrawing(_bitmap, new Rect(0, 0, ActualWidth, ActualHeight)));
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);     
        }

        protected override void OnDataChanged()
        {
            base.OnDataChanged();
            DrawSpectrogram();
        }

        protected override void OnAxisChanged()
        {
            base.OnAxisChanged();
            DrawSpectrogram();
        }

        private async void DrawSpectrogram()
        {
            if (_bitmap == null)
            {
                return;
            }

            // stop here if a drawing operation is already in progress
            if (_isDrawing)
            {
                _isRequested = true;
                return;
            }

            // set the drawing flag
            _isDrawing = true;

            // preprocess the data to draw
            var mapped = Data.Where(p => p.Y is double[]).Select(p => new Tuple<double, double[]>(AxisX.GetItemPixel(p.X), (double[])p.Y)).OrderBy(p => p.Item1).ToList();
            var palette = ColorPalette;

            // lock the backbuffer
            var bitmap = _bitmap;
            bitmap.Lock();
            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;
            var stride = bitmap.BackBufferStride;
            var backbuffer = bitmap.BackBuffer;

            // fill the backbuffer from a background thread
            await Task.Run(() =>
            {
                // map data points to axis pixels
                if (mapped.Count > 0)
                {
                    var rows = mapped.First().Item2.Length;
                    var rowHeight = height / (double)rows;
                    var maximumValue = mapped.Aggregate(new double[rows], (c, p) =>
                    {
                        for (var i = 0; i < c.Length; i++) c[i] = Math.Max(c[i], p.Item2[i]);
                        return c;
                    });

                    // iterate over all pixels in the series
                    var lastColumn = 0;
                    var nextColumn = 1;
                    for (var x = 0.0; x < width; x++)
                    {
                        // find the next data column
                        while (nextColumn < mapped.Count - 1 && x > mapped[nextColumn].Item1)
                        {
                            lastColumn = nextColumn;
                            nextColumn += 1;
                        }

                        // calculate the horizontal relative position in between the two columns
                        var factorX = (x - mapped[lastColumn].Item1) / (mapped[nextColumn].Item1 - mapped[lastColumn].Item1);

                        // draw the column
                        var lastRow = 0;
                        var nextRow = 1;
                        for (var y = 0.0; y < height; y++)
                        {
                            // find the next data row
                            while (nextRow < mapped[nextColumn].Item2.Length - 1 && y > nextRow * rowHeight)
                            {
                                lastRow = nextRow;
                                nextRow += 1;
                            }

                            // calculate the horizontal relative position in between the two rows
                            var factorY = (y - lastRow * rowHeight) / rowHeight;

                            // interpolate the point value 
                            double c;
                            if (factorX >= 0 && factorX <= 1 && factorY >= 0 && factorY <= 1)
                            {
                                var a00 = mapped[lastColumn].Item2[lastRow];
                                var a10 = mapped[nextColumn].Item2[lastRow] - mapped[lastColumn].Item2[lastRow];
                                var a01 = mapped[lastColumn].Item2[nextRow] - mapped[lastColumn].Item2[lastRow];
                                var a11 = mapped[nextColumn].Item2[nextRow] + mapped[lastColumn].Item2[lastRow] - (mapped[nextColumn].Item2[lastRow] + mapped[lastColumn].Item2[nextRow]);
                                c = a00 + a10 * factorX + a01 * factorY + a11 * factorX * factorY;
                            }
                            else
                            {
                                c = 0;
                            }

                            // scale the pixel value to 0..1
                            var scaledMaximum = factorY * (maximumValue[nextRow] - maximumValue[lastRow]) + maximumValue[lastRow];
                            var scaledValue = scaledMaximum > 1e-3 ? c / scaledMaximum : 0;

                            // get the color for the pixel
                            var color = palette[(int)(scaledValue * (palette.Count - 1))];

                            // draw the pixel
                            var value = (byte)(255 * scaledValue);
                            unsafe
                            {
                                var address = (byte*)(backbuffer + ((int)(height - 1 - y) * stride) + (int)x * 4);
                                *(address++) = color.B;
                                *(address++) = color.G;
                                *(address++) = color.R;
                                *(address++) = color.A;
                            }
                        }
                    }
                }
                else
                {
                    var color = palette[0];
                    for (var i = 0; i < width * height * 4; i += 4)
                    {
                        unsafe
                        {
                            var address = (byte*)(backbuffer + i);
                            *(address++) = color.B;
                            *(address++) = color.G;
                            *(address++) = color.R;
                            *(address++) = color.A;
                        }
                    }
                }
            });

            // write the bitmap contents
            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            bitmap.Unlock();

            // invalidate visuals to redraw
            _isDrawing = false;
            InvalidateVisual();

            if (_isRequested)
            {
                _isRequested = false;
                DrawSpectrogram();
            }
        }
    }
}
