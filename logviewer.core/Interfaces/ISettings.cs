using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.Interfaces
{
    /// <summary>
    /// Interface for accessing application settings
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// Gets the current date/time format string
        /// </summary>
        string DateTimeFormat { get; }

        /// <summary>
        /// Gets the maximum number of items to aggregate
        /// </summary>
        int AggregateLimit { get; }
    }
}
