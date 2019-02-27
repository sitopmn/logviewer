using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.Interfaces
{
    /// <summary>
    /// Interface for a log item
    /// </summary>
    public interface ILogItem 
    {
        /// <summary>
        /// Gets the source file or archive name
        /// </summary>
        string File { get; }

        /// <summary>
        /// Gets the source archive member name
        /// </summary>
        string Member { get; }

        /// <summary>
        /// Gets the position of the log item in the source file
        /// </summary>
        long Position { get; }

        /// <summary>
        /// Gets the line of the log item in the source file
        /// </summary>
        int Line { get; }

        /// <summary>
        /// Gets the fields of the log item
        /// </summary>
        Dictionary<string, object> Fields { get; }

        /// <summary>
        /// Gets the message of the log item
        /// </summary>
        string Message { get; }
    }
}
