using logviewer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace logviewer
{
    /// <summary>
    /// Static class for extension methods
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts a dictionary of names and types to <see cref="ColumnData"/> instances
        /// </summary>
        /// <param name="fields">The dictionary to convert</param>
        /// <returns>An enumerable of <see cref="ColumnData"/> instances</returns>
        public static IEnumerable<ColumnData> ToColumnData(this IDictionary<string, Type> fields)
        {
            return fields.Select((f, i) => new ColumnData()
            {
                HeaderText = f.Key == "_raw" ? "Raw Message" : f.Key,
                DisplayMember = "Fields[" + EscapePropertyPath(f.Key) + "]",
                DisplayField = f.Key,
                DisplayType = f.Value,
                DisplayFormat = f.Value == typeof(DateTime?) ? "{0:" + Properties.Settings.Default.DateTimeFormat + "}" : "{0}",
            });
        }

        /// <summary>
        /// Escapes a binding property path for binding to query result items
        /// </summary>
        /// <param name="input">The input path</param>
        /// <returns>The escaped path</returns>
        private static string EscapePropertyPath(string input)
        {
            input = input.Replace("]", "^]");
            input = input.Replace(",", "^,");
            return input;
        }
    }
}
