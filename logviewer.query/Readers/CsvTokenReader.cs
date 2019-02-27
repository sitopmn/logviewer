using logviewer.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    /// <summary>
    /// Reader for reading tokens out of an csv formatted file
    /// </summary>
    internal class CsvTokenReader : LineTokenReader
    {
        /// <summary>
        /// List if column names
        /// </summary>
        private readonly List<string> _columns = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvTokenReader"/> class.
        /// </summary>
        /// <param name="stream">Stream providing the source data</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member if the file is an archive</param>
        public CsvTokenReader(Stream stream, string file, string member)
            : base(stream, file, member)
        {
        }

        /// <summary>
        /// Reads index tokens into a buffer
        /// </summary>
        /// <param name="buffer">Buffer to save buffer into</param>
        /// <param name="offset">Offset of the first token in the buffer</param>
        /// <param name="count">Number of buffer to read</param>
        /// <returns>The number of buffer read</returns>
        public override int Read(Token[] buffer, int offset, int count)
        {
            // read the column names from the first line
            if (Position == 0)
            {
                var inColumn = false;
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
                        _columns.Add(MarkEnd(-1));
                        inColumn = false;
                    }
                    else if (!inColumn)
                    {
                        MarkBegin();
                        inColumn = true;
                    }
                }
            }

            return base.Read(buffer, offset, count);
        }
    }
}
