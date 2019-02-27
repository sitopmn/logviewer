using logviewer.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Types
{
    /// <summary>
    /// Reader for decoding a character stream into tokens
    /// </summary>
    internal class TokenReader : IDisposable
    {
        /// <summary>
        /// Reader for retrieving source character data
        /// </summary>
        private readonly TextReader _reader;

        /// <summary>
        /// Encoding for tracking token positions
        /// </summary>
        private readonly Encoding _encoding;

        /// <summary>
        /// Source file of the tokens
        /// </summary>
        private readonly string _file;

        /// <summary>
        /// Source member of the tokens
        /// </summary>
        private readonly string _member;

        /// <summary>
        /// Buffer for keeping character data
        /// </summary>
        private readonly char[] _buffer = new char[1024];

        /// <summary>
        /// Number of characters in <see cref="_buffer"/>
        /// </summary>
        private int _bufferCount = 0;

        /// <summary>
        /// Index of the current character in <see cref="_buffer"/>
        /// </summary>
        private int _bufferPointer = 0;

        /// <summary>
        /// Current state of the tokenizer
        /// </summary>
        private int _state = -1;

        /// <summary>
        /// Index of the first character of the current token in <see cref="_buffer"/>
        /// </summary>
        private int _tokenPointer = 0;

        /// <summary>
        /// Position of the token in the source file
        /// </summary>
        private long _tokenPosition = 0;

        /// <summary>
        /// Position of the current character in the source file
        /// </summary>
        private long _position = 0;

        /// <summary>
        /// Previously read character
        /// </summary>
        private char _previous;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenReader"/> class from a stream
        /// </summary>
        /// <param name="stream">Stream to process</param>
        /// <param name="file">File the tokens were read from</param>
        /// <param name="member">Member the tokens were read from</param>
        /// <param name="position">Position of the first input character in the file</param>
        /// <param name="encoding">Encoding of the source file</param>
        public TokenReader(Stream stream, string file, string member, long position, Encoding encoding)
            : this(new StreamReader(stream), file, member, position, encoding)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenReader"/> class from a string
        /// </summary>
        /// <param name="line">String to process</param>
        /// <param name="file">File the tokens were read from</param>
        /// <param name="member">Member the tokens were read from</param>
        /// <param name="position">Position of the first input character in the file</param>
        /// <param name="encoding">Encoding of the source file</param>
        public TokenReader(string line, string file, string member, long position, Encoding encoding)
            : this(new StringReader(line), file, member, position, encoding)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenReader"/> class from a reader
        /// </summary>
        /// <param name="reader">Reader to read the source characters</param>
        /// <param name="file">File the tokens were read from</param>
        /// <param name="member">Member the tokens were read from</param>
        /// <param name="position">Position of the first input character in the file</param>
        /// <param name="encoding">Encoding of the source file</param>
        public TokenReader(TextReader reader, string file, string member, long position, Encoding encoding)
        {
            _file = file;
            _member = member;
            _encoding = encoding;
            _reader = reader;
            _position = position;
        }

        /// <summary>
        /// Gets a value indicating no more tokens are available
        /// </summary>
        public bool EndOfStream => _bufferPointer == _bufferCount && _reader.Peek() < 0;

        /// <summary>
        /// Reads all tokens
        /// </summary>
        /// <returns>Enumerable returning all tokens</returns>
        public IEnumerable<Token> ReadAll()
        {
            var tokens = new Token[1];
            while (Read(tokens, 0, tokens.Length) == 1)
            {
                yield return tokens[0];
            }
        }

        /// <summary>
        /// Reads a number of tokens
        /// </summary>
        /// <param name="tokens">Array to write the tokens to</param>
        /// <param name="offset">Offset of the first token in the array</param>
        /// <param name="count">Number of tokens to read</param>
        /// <returns>Number of tokens read</returns>
        public int Read(Token[] tokens, int offset, int count)
        {
            var requested = count;

            if (_state < 0)
            {
                _state = 0;

                // return the token for the first line
                tokens[offset++] = CreateNewlineToken(_file, _member, _position);
                count -= 1;
            }

            while (count > 0 && (_bufferPointer < _bufferCount || _reader.Peek() >= 0))
            {
                if (_bufferPointer == _bufferCount)
                {
                    // copy the current token to the start of the buffer
                    Array.Copy(_buffer, _tokenPointer, _buffer, 0, _buffer.Length - _tokenPointer);
                    _bufferCount -= _tokenPointer;
                    _bufferPointer -= _tokenPointer;
                    _tokenPointer = 0;

                    // fill the remainder of the buffer
                    if (_buffer.Length - _bufferCount > 0)
                    {
                        _bufferCount += _reader.Read(_buffer, _bufferCount, _buffer.Length - _bufferCount);
                    }
                }

                // decode the buffer
                while (count > 0 && _bufferPointer < _bufferCount)
                {
                    var c = _buffer[_bufferPointer];
                    var isLetter = char.IsLetter(c);
                    var isDigit = char.IsDigit(c);
                    var isUpper = char.IsUpper(c);
                    var tokenLength = _bufferPointer - _tokenPointer;

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
                                _tokenPosition = _position;
                                _tokenPointer = _bufferPointer;
                            }
                            break;

                        // letter or digits
                        case 1:
                            if (c == '\n' || c == '\r') // split on newline
                            {
                                if (tokenLength > 0)
                                {
                                    tokens[offset++] = CreateCharacterToken(_buffer, _tokenPointer, tokenLength, _file, _member, _tokenPosition);
                                    count -= 1;
                                }

                                _state = c;
                            }
                            else if (isLetter && char.IsDigit(_previous)) // split on change to letters
                            {
                                if (tokenLength > 0)
                                {
                                    tokens[offset++] = CreateCharacterToken(_buffer, _tokenPointer, tokenLength, _file, _member, _tokenPosition);
                                    count -= 1;
                                }

                                _tokenPosition = _position;
                                _tokenPointer = _bufferPointer;
                            }
                            else if (isDigit && char.IsLetter(_previous)) // split on change to digits
                            {
                                if (tokenLength > 0)
                                {
                                    tokens[offset++] = CreateCharacterToken(_buffer, _tokenPointer, tokenLength, _file, _member, _tokenPosition);
                                    count -= 1;
                                }

                                _tokenPosition = _position;
                                _tokenPointer = _bufferPointer;
                            }
                            else if (isUpper && char.IsLower(_previous)) // split on camel case
                            {
                                if (tokenLength > 0)
                                {
                                    tokens[offset++] = CreateCharacterToken(_buffer, _tokenPointer, tokenLength, _file, _member, _tokenPosition);
                                    count -= 1;
                                }

                                _tokenPosition = _position;
                                _tokenPointer = _bufferPointer;
                            }
                            else if (!isLetter && !isDigit)
                            {
                                if (tokenLength > 0)
                                {
                                    tokens[offset++] = CreateCharacterToken(_buffer, _tokenPointer, tokenLength, _file, _member, _tokenPosition);
                                    count -= 1;
                                }

                                _state = 0;
                                _tokenPosition = _position;
                                _tokenPointer = _bufferPointer;
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
                                tokens[offset++] = CreateNewlineToken(_file, _member, _position);
                                count -= 1;
                                _state = c;
                            }
                            else if (isLetter || isDigit)
                            {
                                tokens[offset++] = CreateNewlineToken(_file, _member, _position);
                                count -= 1;
                                _state = 1;
                                _tokenPosition = _position;
                                _tokenPointer = _bufferPointer;
                            }
                            else
                            {
                                tokens[offset++] = CreateNewlineToken(_file, _member, _position);
                                count -= 1;
                                _state = 0;
                                _tokenPosition = _position;
                                _tokenPointer = _bufferPointer;
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
                                tokens[offset++] = CreateNewlineToken(_file, _member, _position);
                                count -= 1;
                                _state = c;
                            }
                            else if (isLetter || isDigit)
                            {
                                tokens[offset++] = CreateNewlineToken(_file, _member, _position);
                                count -= 1;
                                _state = 1;
                                _tokenPosition = _position;
                                _tokenPointer = _bufferPointer;
                            }
                            else
                            {
                                tokens[offset++] = CreateNewlineToken(_file, _member, _position);
                                count -= 1;
                                _state = 0;
                                _tokenPosition = _position;
                                _tokenPointer = _bufferPointer;
                            }
                            break;

                        // second character of line break
                        case 14:
                            tokens[offset++] = CreateNewlineToken(_file, _member, _position);
                            count -= 1;

                            if (c == '\r' || c == '\n')
                            {
                                _state = c;
                            }
                            else if (isLetter || isDigit)
                            {
                                _state = 1;
                                _tokenPosition = _position;
                                _tokenPointer = _bufferPointer;
                            }
                            else
                            {
                                _state = 0;
                                _tokenPosition = _position;
                                _tokenPointer = _bufferPointer;
                            }
                            break;
                    }

                    _position += GetByteCount(c);
                    _previous = c;
                    _bufferPointer += 1;
                }

                if (count > 0 && EndOfStream && _state == 1)
                {
                    tokens[offset++] = CreateCharacterToken(_buffer, _tokenPointer, _bufferCount - _tokenPointer, _file, _member, _tokenPosition);
                    count -= 1;
                }
            }

            return requested - count;
        }

        /// <summary>
        /// Disposes resources of the reader
        /// </summary>
        public void Dispose()
        {
            _reader.Dispose();
        }

        /// <summary>
        /// Gets the number of bytes for a character
        /// </summary>
        /// <param name="c">The character for which the size is calulated</param>
        /// <param name="encoding">The encoding used to calculate the size</param>
        /// <returns>Number of bytes the character occupies using the given encoding</returns>
        private unsafe long GetByteCount(char c)
        {
            return _encoding.GetByteCount(&c, 1);
        }

        /// <summary>
        /// Create a token from the given data
        /// </summary>
        /// <param name="data">The token data</param>
        /// <param name="file">The file the line was read from</param>
        /// <param name="member">The archive member the line was read from</param>
        /// <param name="position">The starting offset of the token</param>
        /// <returns>A token as specified</returns>
        private Token CreateCharacterToken(char[] data, int offset, int length, string file, string member, long position)
        {
            return new Token()
            {
                Type = ETokenType.Characters,
                Data = new string(data, offset, length),
                File = file,
                Member = member,
                Position = position,
                IsExact = true,
            };
        }

        /// <summary>
        /// Creates a newline token
        /// </summary>
        /// <param name="file">The file the line was read from</param>
        /// <param name="member">The archive member the line was read from</param>
        /// <param name="position">The starting offset of the token</param>
        /// <returns></returns>
        private static Token CreateNewlineToken(string file, string member, long position)
        {
            return new Token() { Type = ETokenType.Line, File = file, Member = member, Position = position, IsExact = true };
        }
    }
}
