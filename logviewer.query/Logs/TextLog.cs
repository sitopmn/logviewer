using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using logviewer.Interfaces;
using logviewer.query.Index;
using logviewer.query.Readers;

namespace logviewer.query
{
    /// <summary>
    /// Log implementation interpreting the sources files line by line as plain text
    /// </summary>
    internal class TextLog : BaseLog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextLog"/> class
        /// </summary>
        /// <param name="settings">Application settings</param>
        /// <param name="index">Index to use</param>
        /// <param name="indexers">List of indexers used to process tokens</param>
        public TextLog(ISettings settings, InvertedIndex index, IEnumerable<ILogIndexer> indexers)
            : base(settings, index, indexers)
        {
        }

        /// <summary>
        /// Factory method for creating log item readers
        /// </summary>
        /// <param name="stream">Input stream to read</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member</param>
        /// <returns>Reader returning log items</returns>
        protected override LogReader<ILogItem> CreateItemReader(Stream stream, string file, string member)
        {
            return new LineItemReader(stream, Encoding.Default, file, member);
        }

        /// <summary>
        /// Factory method for creating index token readers
        /// </summary>
        /// <param name="stream">Input stream to read</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member</param>
        /// <returns>Reader returning index tokens</returns>
        protected override LogReader<Token> CreateTokenReader(Stream stream, string file, string member)
        {
            return new LineTokenReader(stream, Encoding.Default, file, member);
        }
    }
}
