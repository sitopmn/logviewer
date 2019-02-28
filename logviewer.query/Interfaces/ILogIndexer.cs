using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query
{
    /// <summary>
    /// Interface for processing tokens while indexing
    /// </summary>
    public interface ILogIndexer
    {
        /// <summary>
        /// Called before the first file of a log is indexed
        /// </summary>
        void Initialize();

        /// <summary>
        /// Initializes the indexer for a file to index
        /// </summary>
        /// <param name="fileIndex">Index of the file</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member</param>
        /// <param name="length">Length of the file in bytes</param>
        /// <param name="timestamp">Tiemstamp of the file</param>
        /// <param name="append">True if the indexing is performed on appeded input data</param>
        /// <returns>State of the indexer</returns>
        object Initialize(int fileIndex, string file, string member, long length, DateTime timestamp, bool append);

        /// <summary>
        /// Updates the indexer state with a token
        /// </summary>
        /// <param name="state">State of the indexer</param>
        /// <param name="tokens">Tokens to update the state with</param>
        /// <param name="count">Number of tokens to process</param>
        void Update(object state, Token[] tokens, int count);

        /// <summary>
        /// Completes indexing for the given state
        /// </summary>
        /// <param name="state">State of the indexer</param>
        void Complete(object state);

        /// <summary>
        /// Completes indexing the entire log
        /// </summary>
        /// <remarks>May be called multiple times without calling <see cref="Initialize"/></remarks>
        void Complete();
    }
}
