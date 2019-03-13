#define MARK
using logviewer.Interfaces;
using logviewer.query.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    /// <summary>
    /// Reader implementation for line oriented log files
    /// </summary>
    internal class LineItemReader : LogReader<ILogItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineItemReader"/> class
        /// </summary>
        /// <param name="stream">Stream providing the source data</param>
        /// <param name="encoding">Encoding of the log file</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member if the file is an archive</param>
        public LineItemReader(Stream stream, Encoding encoding, string file, string member)
            : base(stream, encoding, file, member)
        {
        }

        /// <summary>
        /// Reads multiple log items into a buffer
        /// </summary>
        /// <param name="buffer">Buffer to store the elements to</param>
        /// <param name="offset">Offset of the first element in the buffer</param>
        /// <param name="count">Number of elements to read</param>
        /// <returns>Number of elements actually read</returns>
        public override int Read(ILogItem[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var item = Read();
                if (item == null)
                {
                    return i;
                }
                else
                {
                    buffer[i + offset] = item;
                }
            }

            return count;
        }

        /// <summary>
        /// Reads a log item
        /// </summary>
        /// <returns>Log item read</returns>
        public override ILogItem Read()
        {
            ILogItem result = null;
            var line = new StringBuilder();
            var linePosition = Position;
            while (true)
            {
                var c = ReadChar();
                if (c < 0)
                {
                    break;
                }
                else if (c == '\r')
                {
                    result = new LogItem(line.ToString(), File, Member, linePosition, Index++);

                    if (PeekChar() == '\n')
                    {
                        ReadChar();
                    }

                    line.Clear();
                    linePosition = Position;
                    break;
                }
                else if (c == '\n')
                {
                    result = new LogItem(line.ToString(), File, Member,linePosition, Index++);

                    if (PeekChar() == '\r')
                    {
                        ReadChar();
                    }

                    line.Clear();
                    linePosition = Position;
                    break;
                }
                else
                {
                    line.Append((char)c);
                }
            }

            if (line.Length > 0 && result == null)
            {
                result = new LogItem(line.ToString(), File, Member, linePosition, Index++);
            }
            
            return result;
        }
    }
}
