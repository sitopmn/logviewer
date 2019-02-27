using logviewer.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace logviewer.Converters
{
    /// <summary>
    /// Converter for generating format strings for chart axis ticks
    /// </summary>
    public class TickFormatConverter : IValueConverter
    {
        /// <summary>
        /// Generates a formatting function for axis ticks
        /// </summary>
        /// <param name="value">A <see cref="ColumnData"/> instance</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>A formatting string or a formatting function</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ColumnData column)
            {
                if (column.DisplayType == typeof(DateTime?))
                {
                    Func<double, string> format = d => new DateTime((long)d).ToString(Properties.Settings.Default.DateTimeFormat);
                    return format;
                }
                else
                {
                    return column.DisplayFormat;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="value">Not used</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>Not used</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
