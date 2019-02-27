using logviewer.query.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Parsing
{
    internal class CsvParser : IParser
    {
        private string[] _columns;

        public bool TryParse(string input, out Dictionary<string, object> fields)
        {
            fields = new Dictionary<string, object>();

            var parts = input.Split(';');
            if (_columns == null && parts.All(p => p.All(c => char.IsLetterOrDigit(c)) && !p.All(c => char.IsDigit(c))))
            {
                _columns = parts;
            }

            if (_columns != null && !parts.All(p => _columns.Contains(p)))
            {
                for (var i = 0; i < _columns.Length; i++)
                {
                    fields[_columns[i]] = parts[i];
                }

                return true;
            }

            return false;
        }
    }
}
