using logviewer.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    internal class LineTokenReader : LogReader<Token>
    {
        /// <summary>
        /// StringBuilder for capturing tokens
        /// </summary>
        private readonly StringBuilder _token = new StringBuilder();

        /// <summary>
        /// Position of the first character of the captured token in the input stream
        /// </summary>
        private long _tokenPosition = 0;

        /// <summary>
        /// State for reading buffer
        /// </summary>
        private int _state = -1;

        /// <summary>
        /// Previous character
        /// </summary>
        private char _previous;

        /// <summary>
        /// True if the previous character was a letter
        /// </summary>
        private bool _previousIsLetter;
       
        /// <summary>
        /// Initializes a new instance of the <see cref="LineTokenReader"/> class
        /// </summary>
        /// <param name="stream">Stream providing the source data</param>
        /// <param name="encoding">Encoding of the log file</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member if the file is an archive</param>
        public LineTokenReader(Stream stream, Encoding encoding, string file, string member)
            : base(stream, encoding, file, member)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineTokenReader"/> class.
        /// </summary>
        /// <param name="line">String to tokenize</param>
        public LineTokenReader(string line)
            : base(new MemoryStream(Encoding.Default.GetBytes(line)), Encoding.Default, string.Empty, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineTokenReader"/> class.
        /// </summary>
        /// <param name="line">String to tokenize</param>
        public LineTokenReader(string line, string file, string member)
            : base(new MemoryStream(Encoding.Default.GetBytes(line)), Encoding.Default, file, member)
        {
        }

        /// <summary>
        /// Reads a single index token
        /// </summary>
        /// <returns>The token or default(Token) of no token could be read</returns>
        public override Token Read()
        {
            var buffer = new Token[1];
            if (Read(buffer, 0, 1) == 0)
            {
                return default(Token);
            }
            else
            {
                return buffer[0];
            }
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
            var total = count;

            if (_state < 0)
            {
                _state = 0;

                // return the token for the first line
                buffer[offset++] = new Token(ETokenType.Item, File, Member, Position);
                count -= 1;
            }

            while (count > 0)
            {
                var r = ReadChar();
                if (r < 0) break;
                var c = (char)r;
                var isLetter = char.IsLetter(c);
                var isDigit = char.IsDigit(c);

                switch (_state)
                {
                    case 0:
                        if (c == '\n' || c == '\r')
                        {
                            _state = c;
                        }
                        else if (isLetter || isDigit)
                        {
                            _state = 1;
                            _token.Clear();
                            _token.Append(c);
                            _tokenPosition = Position;
                        }
                        break;

                    // letter or digits
                    case 1:
                        if (c == '\n' || c == '\r') // split on newline
                        {
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = new Token(ETokenType.Characters, _token.ToString(), File, Member, _tokenPosition);
                                count -= 1;
                            }

                            _state = c;
                        }
                        else if (!isLetter && !isDigit) // stop on non letter or digit
                        {
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = new Token(ETokenType.Characters, _token.ToString(), File, Member, _tokenPosition);
                                count -= 1;
                            }

                            _state = 0;
                        }
                        else if (char.IsUpper(c) && char.IsLower(_previous)) // split on change from lower case to upper case
                        {
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = new Token(ETokenType.Characters, _token.ToString(), File, Member, _tokenPosition);
                                count -= 1;
                            }

                            _token.Clear();
                            _token.Append(c);
                            _tokenPosition = Position;
                        }
                        else if (isLetter != _previousIsLetter) // split on change to or from letters
                        {
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = new Token(ETokenType.Characters, _token.ToString(), File, Member, _tokenPosition);
                                count -= 1;
                            }

                            _token.Clear();
                            _token.Append(c);
                            _tokenPosition = Position;
                        }
                        else
                        {
                            _token.Append(c);
                        }
                        break;

                    // first character of line break is \n
                    case 10:
                        if (c == '\r')
                        {
                            _state = 14;
                        }
                        else if (c == '\n')
                        {
                            buffer[offset++] = new Token(ETokenType.Item, File, Member, Position);
                            count -= 1;
                            _state = c;
                        }
                        else if (isLetter || isDigit)
                        {
                            buffer[offset++] = new Token(ETokenType.Item, File, Member, Position);
                            count -= 1;
                            _state = 1;
                            _token.Clear();
                            _token.Append(c);
                            _tokenPosition = Position;
                        }
                        else
                        {
                            buffer[offset++] = new Token(ETokenType.Item, File, Member, Position);
                            count -= 1;
                            _state = 0;
                        }
                        break;

                    // first character of line break is \r
                    case 13:
                        if (c == '\n')
                        {
                            _state = 14;
                        }
                        else if (c == '\r')
                        {
                            buffer[offset++] = new Token(ETokenType.Item, File, Member, Position);
                            count -= 1;
                            _state = c;
                        }
                        else if (isLetter || isDigit)
                        {
                            buffer[offset++] = new Token(ETokenType.Item, File, Member, Position);
                            count -= 1;
                            _state = 1;
                            _token.Clear();
                            _token.Append(c);
                            _tokenPosition = Position;
                        }
                        else
                        {
                            buffer[offset++] = new Token(ETokenType.Item, File, Member, Position);
                            count -= 1;
                            _state = 0;
                        }
                        break;

                    // second character of line break
                    case 14:
                        buffer[offset++] = new Token(ETokenType.Item, File, Member, Position);
                        count -= 1;

                        if (c == '\r' || c == '\n')
                        {
                            _state = c;
                        }
                        else if (isLetter || isDigit)
                        {
                            _state = 1;
                            _token.Clear();
                            _token.Append(c);
                            _tokenPosition = Position;
                        }
                        else
                        {
                            _state = 0;
                        }
                        break;
                }

                _previous = c;
                _previousIsLetter = isLetter;
            }

            if (count > 0 && EndOfStream && _token.Length > 0 && _state == 1)
            {
                buffer[offset++] = new Token(ETokenType.Characters, _token.ToString(), File, Member, _tokenPosition);
                count -= 1;
                _token.Clear();
            }

            return total - count;
        }
    }
}
