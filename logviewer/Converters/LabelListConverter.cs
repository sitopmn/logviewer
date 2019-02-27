using logviewer.charts;
using logviewer.Interfaces;
using logviewer.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace logviewer.Converters
{
    /// <summary>
    /// Converter for converting log items into a list of axis labels
    /// </summary>
    public class LabelListConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts log items into a list of axis labels
        /// </summary>
        /// <param name="values">An array consisting of the log item and the column containing the label data</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>A list of labels extracted from the log items</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var items = values[0] as IEnumerable;
            var axis = values[1] as ColumnData;
            if (items != null && axis != null)
            {
                if (axis.DisplayType == typeof(string))
                {
                    var xField = axis.DisplayField;
                    var labels = items
                        .Cast<ILogItem>()
                        .Where(i => i != null && i.Fields != null && i.Fields.ContainsKey(xField) && i.Fields[xField] != null)
                        .Select(i => i.Fields[xField].ToString())
                        .Distinct();
                    if (parameter != null && System.Convert.ToBoolean(parameter))
                    {
                        return labels.OrderBy(l => l).ToList();
                    }
                    else
                    {
                        return labels.ToList();
                    }
                }
                else
                {
                    return null;
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
        /// <param name="targetTypes">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>Not used</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
