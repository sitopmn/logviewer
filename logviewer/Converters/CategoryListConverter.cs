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
    /// Converter for building a list of categories from log items
    /// </summary>
    public class CategoryListConverter : IMultiValueConverter
    {
        private const int _displayCount = 10;

        /// <summary>
        /// Converts a list of log items into a list of categories 
        /// </summary>
        /// <param name="values">Array with the log items, the axis column, the value column and the number of log items</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>A list of categories</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var items = values[0] as IEnumerable;
            var axis = values[1] as ColumnData;
            var column = values[2] as ColumnData;
            var count = System.Convert.ToInt32(values[3]);
            if (items != null && axis != null && column != null && count > 0)
            {
                var xField = axis.DisplayField;
                var yField = column.DisplayField;
                var palette = new Palette(Math.Min(count, _displayCount + 1));
                var counter = 0;
                var samples = new List<DataPoint>();
                var otherValue = 0.0;
                var otherCount = 0;
                foreach (var i in items.Cast<ILogItem>().Select((item, index) => new { item, index }).Where(i => i.item != null && i.item.Fields != null && i.item.Fields.ContainsKey(xField) && i.item.Fields[xField] != null && i.item.Fields.ContainsKey(yField) && i.item.Fields[yField] != null))
                {
                    if (counter >= _displayCount)
                    {
                        otherValue += System.Convert.ToDouble(ConvertValue(i.item.Fields[yField], column.DisplayType));
                        otherCount += 1;
                    }
                    else
                    {
                        samples.Add(new DataPoint(ConvertValue(i.item.Fields[xField], axis.DisplayType), ConvertValue(i.item.Fields[yField], column.DisplayType), palette[counter++]) { UserData = i.index });
                    }
                }

                if (otherCount > 0)
                {
                    samples.Add(new DataPoint("Other", otherValue, palette[counter++]));
                }

                return samples;
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
            else
            {
                return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
        }
    }
}
