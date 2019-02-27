using logviewer.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.Model
{
    /// <summary>
    /// Class describing a column of log data
    /// </summary>
    public class ColumnData : NotificationObject
    {
        /// <summary>
        /// Gets or sets the header text of the column
        /// </summary>
        public string HeaderText
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets the name of the log item field to display in the column
        /// </summary>
        public string DisplayField
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets the display member path for the field in the log item
        /// </summary>
        public string DisplayMember
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets the data type of the field the column displays
        /// </summary>
        public Type DisplayType
        {
            get => GetValue<Type>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets the format string for displaying the value
        /// </summary>
        public string DisplayFormat
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets the visualization type of the column
        /// </summary>
        public VisualizationType Visualization
        {
            get => GetValue<VisualizationType>();
            set => SetValue(value);
        }
    }
}
