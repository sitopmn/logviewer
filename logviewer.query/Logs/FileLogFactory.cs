using logviewer.core.Interfaces;
using logviewer.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.query.Logs
{
    /// <summary>
    /// Abstract base class for factories for file based logs
    /// </summary>
    internal abstract class FileLogFactory : ILogFactory
    {
        /// <summary>
        /// List of supported file extensions
        /// </summary>
        private readonly string[] _extensions;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogFactory"/> class
        /// </summary>
        /// <param name="name">Name of the log format</param>
        /// <param name="extensions">List of supported file extensions</param>
        protected FileLogFactory(string name, params string[] extensions)
        {
            _extensions = extensions;
            Name = name;
        }

        /// <summary>
        /// Gets the name of the log format
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a log from the given source
        /// </summary>
        /// <param name="source">Source of the log</param>
        /// <param name="progress">Action to report progress</param>
        /// <param name="cancellation">Token to cancel the operation</param>
        /// <returns>An instance of the log</returns>
        public abstract ILog Create(string[] source, Action<double> progress, CancellationToken cancellation);

        /// <summary>
        /// Checks whether the sources are supported by the log format
        /// </summary>
        /// <param name="sources">Sources to check</param>
        /// <returns>True if the source is supported</returns>
        public bool IsSupported(string[] sources)
        {
            var supported = true;
            try
            {

                foreach (var s in sources)
                {
                    if (File.Exists(s))
                    {
                        var extension = Path.GetExtension(s).ToLowerInvariant();
                        if (extension == ".zip")
                        {
                            using (var archive = new ZipArchive(new FileStream(s, FileMode.Open)))
                            {
                                if (archive.Entries.Any(e => Array.IndexOf(_extensions, Path.GetExtension(e.FullName).ToLowerInvariant()) < 0))
                                {
                                    supported = false;
                                }
                            }
                        }
                        else if (Array.IndexOf(_extensions, extension) < 0)
                        {
                            supported = false;
                        }
                    }
                    else
                    {
                        supported = false;
                    }
                }
            }
            catch
            {
                supported = false;
            }

            return supported;
        }
    }
}
