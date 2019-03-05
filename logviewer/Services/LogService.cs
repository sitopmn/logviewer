using logviewer.core.Interfaces;
using logviewer.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.Services
{
    /// <summary>
    /// Implementation of the log management service
    /// </summary>
    [Export(typeof(ILogService))]
    internal class LogService : ILogService
    {
        /// <summary>
        /// The actual log wrapped by the log service
        /// </summary>
        private readonly ILog _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogService"/> class
        /// </summary>
        /// <param name="log">The log instance wrapped by the log service instance</param>
        [ImportingConstructor]
        public LogService(ILog log)
        {
            _log = log;
        }

        /// <summary>
        /// Gets the loaded log
        /// </summary>
        public ILog Log => _log.Files.Length > 0 ? _log : null;

        /// <summary>
        /// Gets a list of supported formats
        /// </summary>
        public IEnumerable<string> Formats
        {
            get;
            private set;
        }

        /// <summary>
        /// Fires when a log was loaded
        /// </summary>
        public event EventHandler Loaded;

        /// <summary>
        /// Loads a log from the given source. The format is auto-detected
        /// </summary>
        /// <param name="source">Source to load the log from</param>
        /// <param name="progress">Action to report progress while loading the log</param>
        /// <param name="cancellation">Token for cancelling the operation</param>
        public void Load(string[] source, Action<double> progress, CancellationToken cancellation)
        {
            var format = DetectLogFormat(source);
            if (!string.IsNullOrEmpty(format))
            {
                throw new NotSupportedException("The log format could not be detected automatically");
            }

            Load(source, format, progress, cancellation);
        }
        
        /// <summary>
        /// Loads a log from the given source with the given format
        /// </summary>
        /// <param name="source">Source to load the log from</param>
        /// <param name="format">Format name of the log</param>
        /// <param name="progress">Action to report progress while loading the log</param>
        /// <param name="cancellation">Token for cancelling the operation</param>
        public void Load(string[] source, string format, Action<double> progress, CancellationToken cancellation)
        {
            // load the log
            _log.Load(source, progress, cancellation);

            // fire the loaded event
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Tries to detect the log format
        /// </summary>
        /// <param name="source">Source of the log which format should be detected</param>
        /// <returns>Name of the format or <see cref="string.Empty"/> if it couldn't be detected</returns>
        private string DetectLogFormat(string[] source)
        {
            return string.Empty;
        }
    }
}
