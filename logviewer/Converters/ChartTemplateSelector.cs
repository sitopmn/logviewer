using logviewer.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace logviewer.Converters
{
    /// <summary>
    /// A template selector for display chart types
    /// </summary>
    public class ChartTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets the template for column charts
        /// </summary>
        public DataTemplate ColumnTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for line charts
        /// </summary>
        public DataTemplate LineTemplate { get; set; }

        /// <summary>
        /// Gets or sets the templates for scatter charts
        /// </summary>
        public DataTemplate ScatterTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for step charts
        /// </summary>
        public DataTemplate StepTemplate { get; set; }

        /// <summary>
        /// Selects a template for the given column
        /// </summary>
        /// <param name="item">The column to select the template for</param>
        /// <param name="container">Not used</param>
        /// <returns>A <see cref="DataTemplate"/> for visualizing the column</returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ColumnData column)
            {
                switch (column.Visualization)
                {
                    case VisualizationType.Column:
                        return ColumnTemplate;

                    case VisualizationType.Line:
                        return LineTemplate;

                    case VisualizationType.Scatter:
                        return ScatterTemplate;

                    case VisualizationType.Step:
                        return StepTemplate;

                    default:
                        return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
