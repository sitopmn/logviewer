using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace logviewer.core
{
    public class OffsetConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var offset = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            return System.Convert.ToDouble(value, CultureInfo.InvariantCulture) + offset;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var offset = System.Convert.ToDouble(values[1], CultureInfo.InvariantCulture);
            return System.Convert.ToDouble(values[0], CultureInfo.InvariantCulture) + offset;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
