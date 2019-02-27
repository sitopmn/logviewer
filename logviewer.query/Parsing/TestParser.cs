using logviewer.query.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Parsing
{
    internal class TestParser : IParser
    {
        public bool TryParse(string input, out Dictionary<string, object> fields)
        {
            fields = new Dictionary<string, object>()
            {
                { "A", 1 },
                { "B", "2" },
                { "C", 3.1 },
            };
            return true;
        }
    }
}
