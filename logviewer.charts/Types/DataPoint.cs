using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace logviewer.charts
{
    public class DataPoint
    {
        public object X;
        public object Y;
        public object UserData;
        public Color Color;

        public DataPoint()
            : this(null, null, Colors.White)
        { }

        public DataPoint(object x, object y)
            : this(x, y, Colors.White)
        { }

        public DataPoint(object x, object y, Color c)
        {
            X = x;
            Y = y;
            Color = c;
        }

    }
}
