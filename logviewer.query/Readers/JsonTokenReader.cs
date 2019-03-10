using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    internal class JsonTokenReader : ParseReader<Token>
    {
        private readonly StringBuilder _token = new StringBuilder();

        private State _state = State.Item;

        private int _objectLevel = 0;

        private int _arrayLevel = 0;

        private long _tokenPosition = 0;

        private char _previous;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTokenReader"/> class
        /// </summary>
        /// <param name="stream">Stream providing the source data</param>
        /// <param name="encoding">Encoding of the log file</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member if the file is an archive</param>
        public JsonTokenReader(Stream stream, Encoding encoding, string file, string member)
            : base(stream, encoding, file, member)
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
            
            while (count > 0 && !EndOfStream)
            {
                var position = Position;
                var c = (char)ReadChar();
                var isLetter = char.IsLetter(c);
                var isDigit = char.IsDigit(c);
                var isUpper = char.IsUpper(c);

                switch (_state)
                {
                    case State.Item:
                        if (c == '{' )
                        {
                            _objectLevel += 1;
                            if (_objectLevel == 1)
                            {
                                buffer[offset++] = CreateItemToken(position);
                                count -= 1;
                            }
                        }
                        else if (c == '}')
                        {
                            _objectLevel -= 1;
                        }
                        else if (c == '[')
                        {
                            _arrayLevel = 1;
                            _state = State.Array;
                        }
                        else if (c == '/')
                        {
                            _state = State.Comment;
                        }
                        else if (c == '"')
                        {
                            _state = State.String;
                        }
                        else if (isLetter || isDigit)
                        {
                            _state = State.Token;
                            _token.Clear();
                            _token.Append((char)c);
                            _tokenPosition = position;

                        }
                        break;

                    // start of a comment
                    case State.Comment:
                        if (c == '/')
                        {
                            _state = State.CommentLine;
                        }
                        else if (c == '*')
                        {
                            _state = State.CommentBlock;
                        }
                        break;
                    
                    // end of a single line comment
                    case State.CommentLine:
                        if (c == '\n')
                        {
                            _state = State.Item;
                        }
                        break;

                    // first end-delimiter character of a block comment
                    case State.CommentBlock:
                        if (c == '*')
                        {
                            _state = State.CommentBlockEnd;
                        }
                        break;

                    // last end-delimiter character of a block comment
                    case State.CommentBlockEnd:
                        if (c == '/')
                        {
                            _state = State.Item;
                        }
                        break;

                    // skip array contents while indexing
                    case State.Array:
                        if (c == '[')
                        {
                            _arrayLevel += 1;
                        }
                        else if (c == ']')
                        {
                            _arrayLevel -= 1;
                            if (_arrayLevel == 0)
                            {
                                _state = State.Item;
                            }
                        }
                        break;

                    // non-token-characters within a string
                    case State.String:
                        if (c == '"')
                        {
                            _state = State.Item;
                        }
                        else if (isLetter || isDigit)
                        {
                            _state = State.StringToken;
                            _token.Clear();
                            _token.Append((char)c);
                            _tokenPosition = position;
                        }
                        break;

                    // letter or digits within a string
                    case State.StringToken:
                        if (c == '"')
                        {
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = CreateCharacterToken(_token.ToString(), _tokenPosition);
                                count -= 1;
                            }

                            _state = State.Item;
                        }
                        else if (!isLetter && !isDigit)
                        {
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = CreateCharacterToken(_token.ToString(), _tokenPosition);
                                count -= 1;
                            }

                            _state = State.String;
                        }
                        else
                        {
                            goto case State.Token;
                        }
                        break;

                    // letter or digits
                    case State.Token:
                        if (isLetter && char.IsDigit(_previous)) // split on change to letters
                        {
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = CreateCharacterToken(_token.ToString(), _tokenPosition);
                                count -= 1;
                            }

                            _token.Clear();
                            _token.Append((char)c);
                            _tokenPosition = position;
                        }
                        else if (isDigit && char.IsLetter(_previous)) // split on change to digits
                        {
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = CreateCharacterToken(_token.ToString(), _tokenPosition);
                                count -= 1;
                            }

                            _token.Clear();
                            _token.Append((char)c);
                            _tokenPosition = position;
                        }
                        else if (isUpper && char.IsLower(_previous)) // split on camel case
                        {
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = CreateCharacterToken(_token.ToString(), _tokenPosition);
                                count -= 1;
                            }

                            _token.Clear();
                            _token.Append((char)c);
                            _tokenPosition = position;
                        }
                        else if (!isLetter && !isDigit)
                        {
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = CreateCharacterToken(_token.ToString(), _tokenPosition);
                                count -= 1;
                            }

                            _state = State.Item;
                        }
                        else
                        {
                            _token.Append((char)c);
                        }
                        break;
                }

                _previous = c;
            }

            if (count > 0 && EndOfStream && _token.Length > 0 && (_state == State.Token || _state == State.StringToken))
            {
                buffer[offset++] = CreateCharacterToken(_token.ToString(), _tokenPosition);
                count -= 1;
            }

            return total - count;
        }

        /// <summary>
        /// Create a token from the given data
        /// </summary>
        /// <param name="data">The token data</param>
        /// <param name="file">The file the line was read from</param>
        /// <param name="member">The archive member the line was read from</param>
        /// <param name="position">The starting offset of the token</param>
        /// <returns>A token as specified</returns>
        private Token CreateCharacterToken(string token, long position)
        {
            return new Token()
            {
                Type = ETokenType.Characters,
                Data = token,
                File = File,
                Member = Member,
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
        private Token CreateItemToken(long position)
        {
            return new Token() { Type = ETokenType.Item, File = File, Member = Member, Position = position, IsExact = true };
        }

        private enum State
        {
            Item,
            String,
            Token,
            Array,
            CommentLine,
            CommentBlock,
            Comment,
            CommentBlockEnd,
            StringToken,
        }
    }
}
