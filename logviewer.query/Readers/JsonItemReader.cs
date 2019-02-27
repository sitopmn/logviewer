using logviewer.Interfaces;
using logviewer.query.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    internal class JsonReader : LogReader<ILogItem>
    {
        public JsonReader(Stream stream, string file, string member)
            : base(stream, file, member)
        {
        }

        public override ILogItem Read()
        {
            throw new NotImplementedException();
        }
    }
}
