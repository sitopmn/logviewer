using logviewer.core.Interfaces;
using logviewer.Interfaces;
using logviewer.query.Index;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.query.Logs
{
    /// <summary>
    /// Factory for creating text based logs
    /// </summary>
    [Export(typeof(ILogFactory))]
    internal class TextLogFactory : ILogFactory
    {
        /// <summary>
        /// Application settings
        /// </summary>
        private readonly ISettings _settings;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TextLogFactory"/> class
        /// </summary>
        /// <param name="settings">Application settings object</param>
        /// <param name="indexerFactories">List of factories for indexers</param>
        [ImportingConstructor]
        public TextLogFactory(ISettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Gets the name of the log format
        /// </summary>
        public string Name => "Plain Text";

        /// <summary>
        /// Checks whether the given source can be opened by the log
        /// </summary>
        /// <param name="source">Source to check</param>
        /// <returns>True if the source is supported, false otherwise</returns>
        public bool IsSupported(string[] source)
        {
            try
            {
                return source.All(s => File.Exists(s));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a log from the given source
        /// </summary>
        /// <param name="source">Source of the log</param>
        /// <param name="progress">Action to report progress</param>
        /// <param name="cancellation">Token to cancel the operation</param>
        /// <returns>Log create from the source</returns>
        public ILog Create(string[] source, Action<double> progress, CancellationToken cancellation)
        {
            var index = new InvertedIndex();
            var log = new TextLog(_settings, index, new[] { index });
            log.Load(source, progress, cancellation);
            return log;
        }
    }
}
