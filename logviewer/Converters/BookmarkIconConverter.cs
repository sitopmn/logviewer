using logviewer.Model;
using MaterialDesignThemes.Wpf;
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
    /// Converter for defining the icon displayed for a bookmarked query
    /// </summary>
    public class BookmarkIconConverter : IValueConverter
    {
        /// <summary>
        /// Converts a bookmark into an icon kind
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SearchContext context)
            {
                if (context.IsFromRepository)
                {
                    return PackIconKind.Cloud;
                }
                else
                {
                    return PackIconKind.Bookmark;
                }
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
