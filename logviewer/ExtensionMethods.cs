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
                HeaderText = f.Key,
                DisplayMember = "Fields[" + EscapePropertyPath(f.Key) + "]",
                DisplayField = f.Key,
                DisplayType = f.Value,
                DisplayFormat = f.Value == typeof(DateTime?) ? "{0:" + Properties.Settings.Default.DateTimeFormat + "}" : "{0}",
            });
        }

        /// <summary>
        /// Selects items of an enumerable and skips when the selector throws an exception
        /// </summary>
        /// <typeparam name="TSource">Element type of the source enumerable</typeparam>
        /// <typeparam name="TResult">Element type of the result enumerable</typeparam>
        /// <param name="source">Source enumerable</param>
        /// <param name="selector">Selector to apply to the elements of the source enumerable</param>
        /// <returns>Result enumerable</returns>
        public static IEnumerable<TResult> TrySelect<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            foreach (var i in source)
            {
                TResult r;
                try
                {
                    r = selector(i);
                }
                catch
                {
                    continue;
                }

                yield return r;
            }
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
