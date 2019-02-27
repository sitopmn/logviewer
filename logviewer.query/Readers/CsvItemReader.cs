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
    internal class CsvItemReader : LogReader<ILogItem>
    {
        private readonly IList<string> _columns;

        public CsvItemReader(Stream stream, string file, string member, IList<string> columns)
            : base(stream, file, member)
        {
            _columns = columns;
        }

        public override ILogItem Read()
        {
            ILogItem result = new LogItem(string.Empty, File, Member, Position, Index);
            var inColumn = false;
            var columnIndex = 0;
            while (!EndOfStream)
            {
                var c = ReadChar();
                if (c == '\r' && PeekChar() == '\n')
                {
                    ReadChar();
                    break;
                }
                else if (c == '\n' && PeekChar() == '\r')
                {
                    ReadChar();
                    break;
                }
                else if (c == '\n')
                {
                    break;
                }
                else if (c == '\r')
                {
                    break;
                }
                else if (c == ';' || c == '\t' || c == ',')
                {
                    result.Fields[_columns[columnIndex]] = MarkEnd(-1);
                    inColumn = false;
                    columnIndex += 1;
                }
                else if (!inColumn)
                {
                    MarkBegin();
                    inColumn = true;
                }
            }

            return result;
        }
    }
}
