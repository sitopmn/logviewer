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
    /// Factory for creating instances of <see cref="ILog"/>
    /// </summary>
    public interface ILogFactory
    {
        /// <summary>
        /// Gets the name of the log format
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Checks whether the sources are supported by the log format
        /// </summary>
        /// <param name="source">Sources to check</param>
        /// <returns>True if the source is supported</returns>
        bool IsSupported(string[] source);

        /// <summary>
        /// Creates a log from the given source
        /// </summary>
        /// <param name="source">Source of the log</param>
        /// <param name="progress">Action to report progress</param>
        /// <param name="cancellation">Token to cancel the operation</param>
        /// <returns>An instance of the log</returns>
        ILog Create(string[] source, Action<double> progress, CancellationToken cancellation);
    }
}
