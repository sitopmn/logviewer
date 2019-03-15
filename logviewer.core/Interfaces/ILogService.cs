using logviewer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.core.Interfaces
{
    /// <summary>
    /// Interface of the log management service
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// Fires when a log was loaded
        /// </summary>
        event EventHandler Loaded;

        /// <summary>
        /// Gets the loaded log
        /// </summary>
        ILog Log { get; }

        /// <summary>
        /// Gets a list of supported formats
        /// </summary>
        IEnumerable<string> Formats { get; }

        /// <summary>
        /// Detect the format of the log source
        /// </summary>
        /// <param name="source">Source to check</param>
        /// <returns>Name of the format or <see cref="string.Empty"/> if it couldn't be determined</returns>
        string Detect(string[] source);

        /// <summary>
        /// Loads a log from the given source with the given format
        /// </summary>
        /// <param name="source">Source to load the log from</param>
        /// <param name="format">Format name of the log</param>
        /// <param name="progress">Action to report progress while loading the log</param>
        /// <param name="cancellation">Token for cancelling the operation</param>
        void Load(string[] source, string format, Action<double> progress, CancellationToken cancellation);


    }
}
