using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Types
{
    internal class StringBuffer : TextReader
    {
        private readonly List<char> _characters = new List<char>();

        public void Add(string data)
        {
            _characters.AddRange(data.ToCharArray());
        }

        public override int Read()
        {
            var c = _characters[0];
            _characters.RemoveAt(0);
            return c;
        }
    }
}
