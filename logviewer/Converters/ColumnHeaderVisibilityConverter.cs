using logviewer.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace logviewer.Converters
{
    /// <summary>
    /// Converter for deciding on log list column header visibility
    /// </summary>
    public class ColumnHeaderVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a column list into a visibility for the column header panel
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IList<ColumnData> columns && columns.Count > 0 && !columns.All(c => c.HeaderText == "message"))
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
