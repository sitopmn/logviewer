using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Interfaces
{
    internal interface IParser
    {
        bool TryParse(string input, out Dictionary<string, object> fields);
    }
}
