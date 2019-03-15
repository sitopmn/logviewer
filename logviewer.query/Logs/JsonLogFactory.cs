using logviewer.core.Interfaces;
using logviewer.Interfaces;
using logviewer.query.Index;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.query.Logs
{
    [Export(typeof(ILogFactory))]
    internal class JsonLogFactory : FileLogFactory
    {
        /// <summary>
        /// Application settings
        /// </summary>
        private readonly ISettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonLogFactory"/> class
        /// </summary>
        /// <param name="settings">Application settings object</param>
        /// <param name="indexerFactories">List of factories for indexers</param>
        [ImportingConstructor]
        public JsonLogFactory(ISettings settings)
            : base("Json Text", ".json")
        {
            _settings = settings;
        }
        
        
        // <summary>
        /// Creates a log from the given source
        /// </summary>
        /// <param name="source">Source of the log</param>
        /// <param name="progress">Action to report progress</param>
        /// <param name="cancellation">Token to cancel the operation</param>
        /// <returns>Log create from the source</returns>
        public override ILog Create(string[] source, Action<double> progress, CancellationToken cancellation)
        {
            var index = new InvertedIndex();
            var log = new JsonLog(_settings, index, new[] { index });
            log.Load(source, progress, cancellation);
            return log;
        }
    }
}
