using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    /// <summary>
    /// A reader class providing helper methods for parsing the source
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class ParseReader<T> : LogReader<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseReader"/> class
        /// </summary>
        /// <param name="stream">Stream providing the source data</param>
        /// <param name="encoding">Encoding of the source data</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member if the file is an archive</param>
        protected ParseReader(Stream stream, Encoding encoding, string file, string member)
            : base(stream, encoding, file, member)
        {
        }

        /// <summary>
        /// Reads a literal string
        /// </summary>
        /// <param name="sequence">String to read</param>
        /// <returns>True if the string was read</returns>
        protected bool ReadLiteral(string sequence)
        {
            if (PeekChar() != sequence[0])
            {
                return false;
            }

            foreach (var c in sequence)
            {
                if (ReadChar() != c)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reads one of multiple characters
        /// </summary>
        /// <param name="chars">Array with alternative characters</param>
        /// <returns>True if one of the given characters was read</returns>
        protected bool ReadOne(params char[] chars)
        {
            var c = PeekChar();
            if (chars.Contains((char)c))
            {
                ReadChar();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Reads until one of the given characters is encountered
        /// </summary>
        /// <param name="delim">Charaters to delimit</param>
        /// <returns>True if one of the given characters was encountered</returns>
        protected bool ReadUntil(params char[] delim)
        {
            while (true)
            {
                var c = PeekChar();
                if (c < 0)
                {
                    return false;
                }
                else if (delim.Contains((char)c))
                {
                    ReadChar();
                    return true;
                }
                else
                {
                    ReadChar();
                }
            }
        }
    }
}
