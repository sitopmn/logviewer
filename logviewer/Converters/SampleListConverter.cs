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
using System.Windows.Media;

namespace logviewer.Converters
{
    /// <summary>
    /// Converter for converting a list of log items into charting samples
    /// </summary>
    public class SampleListConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts a list of log items into chart samples
        /// </summary>
        /// <param name="values">Array containing the log items, the axis column and the series column</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>A list of <see cref="DataPoint"/> instances</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var items = values[0] as IEnumerable;
            var axis = values[1] as ColumnData;
            var column = values[2] as ColumnData;
            if (items != null && axis != null && column != null)
            {
                var xField = axis.DisplayField;
                var yField = column.DisplayField;
                return items
                    .Cast<ILogItem>()
                    .Select((item, index) => new { item, index })
                    .Where(i => i.item != null && i.item.Fields != null && i.item.Fields.ContainsKey(xField) && i.item.Fields[xField] != null && i.item.Fields.ContainsKey(yField) && i.item.Fields[yField] != null)
                    .Select(i => new DataPoint(ConvertValue(i.item.Fields[xField], axis.DisplayType), ConvertValue(i.item.Fields[yField], column.DisplayType)) { UserData = i.index })
                    .ToList();
            }
            else
            {
                return Enumerable.Empty<DataPoint>();
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

        private object ConvertValue(object value, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return value.ToString();
            }
            else if (sourceType == typeof(DateTime?))
            {
                return ((DateTime?)value).Value.Ticks;
            }
            else if (sourceType == typeof(TimeSpan?))
            {
                return ((TimeSpan?)value).Value.TotalSeconds;
            }
            else
            {
                return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
        }
    }
}
