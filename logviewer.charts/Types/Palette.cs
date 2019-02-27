using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace logviewer.charts
{
    public class Palette : IReadOnlyList<Color>
    {
        /// <summary>
        /// The material design palette "blue"
        /// </summary>
        public static Tuple<double, Color>[] MaterialDesignBlue =
        {
            new Tuple<double, Color>(0.00, (Color)ColorConverter.ConvertFromString("#2196F3")),
            new Tuple<double, Color>(0.25, (Color)ColorConverter.ConvertFromString("#8f7ad2")),
            new Tuple<double, Color>(0.50, (Color)ColorConverter.ConvertFromString("#ee68b3")),
            new Tuple<double, Color>(0.75, (Color)ColorConverter.ConvertFromString("#ff716b")),
            new Tuple<double, Color>(1.00, (Color)ColorConverter.ConvertFromString("#ffa600"))
        };

        /// <summary>
        /// The viridis palette
        /// </summary>
        public static Tuple<double, Color>[] Viridis =
        {
            new Tuple<double, Color>(0.00, (Color)ColorConverter.ConvertFromString("#46024E")),
            new Tuple<double, Color>(0.33, (Color)ColorConverter.ConvertFromString("#2e6f8e")),
            new Tuple<double, Color>(0.66, (Color)ColorConverter.ConvertFromString("#29af7f")),
            new Tuple<double, Color>(1.00, (Color)ColorConverter.ConvertFromString("#bddf26")),
        };

        /// <summary>
        /// The default material design palette
        /// </summary>
        public static Tuple<double, Color>[] DefaultSeries = MaterialDesignBlue;

        /// <summary>
        /// The default heatmap palette
        /// </summary>
        public static Tuple<double, Color>[] DefaultHeatmap = Viridis;

        private readonly Color[] _palette;

        public Palette(int count)
            : this(count, DefaultSeries)
        { }

        public Palette(int count, params Tuple<double, Color>[] colors)
        {
            _palette = new Color[count];

            // precalculate the color palette
            var start = (1.0 / count) / 2.0;
            var increment = 1.0 / count;
            for (var i = 0; i < count; i++)
            {
                var position = Math.Max(0.001, Math.Min(start + i * increment, 0.999));
                var first = colors.Last(n => n.Item1 < position);
                var last = colors.First(n => n.Item1 >= position);
                var factor = (position - first.Item1) / (last.Item1 - first.Item1);
                _palette[i] = (last.Item2 - first.Item2) * (float)factor + first.Item2;
            }
        }

        public int Count => _palette.Length;

        public Color this[int index]
        {
            get => _palette[index];
        }

        public IEnumerator<Color> GetEnumerator()
        {
            return (IEnumerator<Color>)_palette.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _palette.GetEnumerator();
        }
    }
}
