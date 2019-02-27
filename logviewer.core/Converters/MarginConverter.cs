using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace logviewer.core
{
    public class MarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 4)
            {
                throw new ArgumentException("Expected four values", "values");
            }

            return new Thickness(
                System.Convert.ToDouble(values[0], CultureInfo.InvariantCulture),
                System.Convert.ToDouble(values[1], CultureInfo.InvariantCulture),
                System.Convert.ToDouble(values[2], CultureInfo.InvariantCulture),
                System.Convert.ToDouble(values[3], CultureInfo.InvariantCulture));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
