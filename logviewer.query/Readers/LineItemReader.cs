﻿#define MARK
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
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member if the file is an archive</param>
        public LineItemReader(Stream stream, string file, string member)
            : base(stream, file, member)
        {
        }
        
        /// <summary>
        /// Reads a log item
        /// </summary>
        /// <returns>Log item read</returns>
        public override ILogItem Read()
        {
            ILogItem result = null;
            var isMarked = false;
            while (!EndOfStream)
            {
                var c = ReadChar();
                if (c == '\r')
                {
                    result = new LogItem(isMarked ? MarkEnd(-1) : string.Empty, File, Member, MarkPosition(), Index++);

                    if (PeekChar() == '\n')
                    {
                        ReadChar();
                    }

                    break;
                }
                else if (c == '\n')
                {
                    result = new LogItem(isMarked ? MarkEnd(-1) : string.Empty, File, Member, MarkPosition(), Index++);

                    if (PeekChar() == '\r')
                    {
                        ReadChar();
                    }
                    
                    break;
                }
                else if (!isMarked)
                {
                    MarkBegin();
                    isMarked = true;
                }
            }

            if (MarkLength() > 0 && result == null)
            {
                result = new LogItem(MarkEnd(), File, Member, MarkPosition(), Index++);
            }
            
            return result;
        }
    }
}
