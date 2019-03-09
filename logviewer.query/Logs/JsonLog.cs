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

namespace logviewer.query.Logs
{
    /// <summary>
    /// Log implementation for json formatted log files. 
    /// The index is created over the plain text but documents are parsed as json
    /// </summary>
    internal class JsonLog : BaseLog
    {
        public JsonLog(ISettings settings, InvertedIndex index, [ImportMany] IEnumerable<ILogIndexer> indexers) 
            : base(settings, index, indexers)
        {
        }

        protected override LogReader<ILogItem> CreateItemReader(Stream stream, string file, string member)
        {
            return new JsonItemReader(stream, Encoding.Default, file, member);
        }

        protected override LogReader<Token> CreateTokenReader(Stream stream, string file, string member)
        {
            throw new NotImplementedException();
        }
    }
}
