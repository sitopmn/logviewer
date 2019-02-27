using logviewer.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace logviewer.Converters
{
    /// <summary>
    /// Converter for generating grid view columns from log columns
    /// </summary>
    public class ColumnsConverter : IValueConverter
    {
        /// <summary>
        /// Converts an array of <see cref="ColumnData"/> instances to <see cref="GridViewColumn"/> instances.
        /// </summary>
        /// <param name="value">The array of <see cref="ColumnData"/> instances</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>An array of <see cref="GridViewColumn"/> instances</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var collection = new GridViewColumnCollection();

            if (value is IEnumerable<ColumnData> columns)
            {
                foreach (var c in columns)
                {
                    collection.Add(new GridViewColumn()
                    {
                        Header = c,
                        DisplayMemberBinding = new Binding(c.DisplayMember)
                        {
                            StringFormat = c.DisplayFormat
                        }
                    });
                }
            }

            return collection;
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
