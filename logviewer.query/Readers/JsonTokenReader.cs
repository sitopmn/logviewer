using logviewer.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    internal class JsonTokenReader : LogReader<Token>
    {
        public JsonTokenReader(Stream stream, string file, string member) 
            : base(stream, file, member)
        {
        }

        public override Token Read()
        {
            throw new NotImplementedException();
        }
    }
}
