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
        private readonly IReadOnlyList<ILogFactory> _factories;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogService"/> class
        /// </summary>
        /// <param name="factories">Enumerable of factories for creating logs</param>
        [ImportingConstructor]
        public LogService([ImportMany] IEnumerable<ILogFactory> factories)
        {
            _factories = factories.ToList();
        }

        /// <summary>
        /// Gets the loaded log
        /// </summary>
        public ILog Log
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a list of supported formats
        /// </summary>
        public IEnumerable<string> Formats => _factories.Select(f => f.Name);

        /// <summary>
        /// Fires when a log was loaded
        /// </summary>
        public event EventHandler Loaded;
        
        /// <summary>
        /// Loads a log from the given source with the given format
        /// </summary>
        /// <param name="source">Source to load the log from</param>
        /// <param name="format">Format name of the log</param>
        /// <param name="progress">Action to report progress while loading the log</param>
        /// <param name="cancellation">Token for cancelling the operation</param>
        public void Load(string[] source, string format, Action<double> progress, CancellationToken cancellation)
        {
            // get the factory for the given format
            var factory = _factories.FirstOrDefault(f => f.Name == format);
            if (factory == null)
            {
                throw new ArgumentException("The given log format is not known");
            }
            
            // load the log
            Log = factory.Create(source, progress, cancellation);

            // fire the loaded event
            Loaded?.Invoke(this, EventArgs.Empty);
        }
    }
}
