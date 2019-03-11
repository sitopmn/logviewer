using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    internal class JsonTokenReader : LogReader<Token>
    {
        private readonly StringBuilder _token = new StringBuilder();

        private readonly Stack<State> _stateHistory = new Stack<State>();

        private readonly Stack<string> _hierarchy = new Stack<string>();

        private readonly StringBuilder _propertyName = new StringBuilder();

        private State _state = State.Item;
        
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
                    // expect the start of an item
                    case State.Item:
                        if (c == '{')
                        {
                            _stateHistory.Push(State.Item);
                            _state = State.KeyStart;
                            _hierarchy.Clear();
                            buffer[offset++] = CreateItemToken(position);
                            count--;
                        }
                        else if (c == '/')
                        {
                            _stateHistory.Push(State.Item);
                            _state = State.Comment;
                        }
                        else if (!char.IsWhiteSpace((char)c) && c != ',' && c != '\x1E')
                        {
                            _state = State.Skip;
                        }
                        break;

                    // expect the start of a key
                    case State.KeyStart:
                        _propertyName.Clear();
                        if (c == '"')
                        {
                            _state = State.Key;
                            _token.Clear();
                            _propertyName.Clear();
                        }
                        else if (c == '/')
                        {
                            _state = State.Comment;
                            _stateHistory.Push(State.KeyStart);
                        }
                        else if (c == '}')
                        {
                            _state = _stateHistory.Pop();
                            _hierarchy.Pop();
                        }
                        else if (!char.IsWhiteSpace((char)c))
                        {
                            _state = State.Skip;
                        }
                        break;

                    // characters in a key
                    case State.Key:
                        if (c == '"')
                        {
                            _state = State.KeyValueSeparator;
                        }
                        else
                        {
                            _propertyName.Append((char)c);
                        }
                        break;
                        
                    // expect a separator between a key and a value
                    case State.KeyValueSeparator:
                        if (c == ':')
                        {
                            _state = State.ValueStart;
                        }
                        else if (c == '/')
                        {
                            _stateHistory.Push(State.KeyValueSeparator);
                            _state = State.Comment;
                        }
                        else if (!char.IsWhiteSpace((char)c))
                        {
                            _state = State.Skip;
                        }
                        break;

                    case State.ValueStart:
                        if (c == '{')
                        {
                            _hierarchy.Push(_propertyName.ToString());
                            _stateHistory.Push(State.ValueEnd);
                            _state = State.KeyStart;
                        }
                        else if (c == '}')
                        {
                            goto case State.ValueEnd;
                        }
                        else if (c == '[')
                        {
                            _hierarchy.Push(string.Empty);
                            _state = State.Skip;
                        }
                        else if (c == '"')
                        {
                            buffer[offset++] = CreateFieldToken(string.Join(".", _hierarchy.Reverse().Concat(new[] { _propertyName.ToString() })));
                            count -= 1;
                            _state = State.String;
                        }
                        else if (c == '/')
                        {
                            _stateHistory.Push(State.ValueStart);
                            _state = State.Comment;
                        }
                        else if (isLetter || isDigit)
                        {
                            buffer[offset++] = CreateFieldToken(string.Join(".", _hierarchy.Reverse().Concat(new[] { _propertyName.ToString() })));
                            count -= 1;
                            _state = State.Literal;
                            _token.Clear();
                            _token.Append((char)c);
                            _tokenPosition = position;
                        }
                        else if (!char.IsWhiteSpace((char)c))
                        {
                            buffer[offset++] = CreateFieldToken(string.Join(".", _hierarchy.Reverse().Concat(new[] { _propertyName.ToString() })));
                            count -= 1;
                            _state = State.Literal;
                        }
                        break;

                    case State.Literal:
                        if (char.IsWhiteSpace((char)c) || c == ',' || c == '}')
                        {
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = CreateCharacterToken(_token.ToString(), _tokenPosition);
                                count -= 1;
                                _token.Clear();
                            }

                            goto case State.ValueEnd;
                        }
                        else if ((isDigit || isLetter) && ((char.IsLetter(_previous) == isLetter && char.IsUpper(_previous) == isUpper) || _token.Length == 0))
                        {
                            if (_token.Length == 0)
                            {
                                _tokenPosition = position;
                            }

                            _token.Append((char)c);
                        }
                        else
                        {
                            buffer[offset++] = CreateCharacterToken(_token.ToString(), _tokenPosition);
                            count -= 1;
                            _token.Clear();
                        }
                        break;

                    case State.String:
                        if (c == '"')
                        {
                            _state = State.ValueEnd;
                            if (_token.Length > 0)
                            {
                                buffer[offset++] = CreateCharacterToken(_token.ToString(), _tokenPosition);
                                count -= 1;
                                _token.Clear();
                            }
                        }
                        else if (c == '\\')
                        {
                            _state = State.StringEscape;
                        }
                        else if ((isDigit || isLetter) && ((char.IsLetter(_previous) == isLetter && char.IsUpper(_previous) == isUpper) || _token.Length == 0))
                        {
                            if (_token.Length == 0)
                            {
                                _tokenPosition = position;
                            }

                            _token.Append((char)c);
                        }
                        else
                        {
                            buffer[offset++] = CreateCharacterToken(_token.ToString(), _tokenPosition);
                            count -= 1;
                            _token.Clear();
                        }
                        break;

                    case State.StringEscape:
                        _token.Append((char)c);
                        _state = State.String;
                        break;

                    case State.ValueEnd:
                        if (c == ',')
                        {
                            _state = State.KeyStart;
                        }
                        else if (c == '}')
                        {
                            _state = _stateHistory.Pop();
                            if (_hierarchy.Count > 0)
                            {
                                _hierarchy.Pop();
                            }
                        }
                        else if (c == '/')
                        {
                            _stateHistory.Push(State.ValueEnd);
                            _state = State.Comment;
                        }
                        else if (!char.IsWhiteSpace((char)c))
                        {
                            _state = State.Skip;
                        }
                        break;
                        
                    // first character of a comment seen
                    case State.Comment:
                        if (c == '/')
                        {
                            _state = State.CommentLine;
                        }
                        else if (c == '*')
                        {
                            _state = State.CommentBlock;
                        }
                        else
                        {
                            _state = State.Skip;
                        }
                        break;

                    // the rest of the line is a comment
                    case State.CommentLine:
                        if (c == '\n' || c == '\r')
                        {
                            _state = _stateHistory.Pop();
                        }
                        break;

                    // first termination character of the block comment
                    case State.CommentBlock:
                        if (c == '*')
                        {
                            _state = State.CommentBlockEnd;
                        }
                        break;

                    // second termination character of the block comment
                    case State.CommentBlockEnd:
                        if (c == '/')
                        {
                            _state = _stateHistory.Pop();
                        }
                        else
                        {
                            _state = State.CommentBlock;
                        }
                        break;

                    // skip out of the current item
                    case State.Skip:
                        if (c == '}')
                        {
                            if (_hierarchy.Count > 0)
                            {
                                _hierarchy.Pop();
                            }

                            if (_hierarchy.Count == 0)
                            {
                                _state = State.Item;
                            }
                        }
                        else if (c == '{' && _hierarchy.Count > 0)
                        {
                            _hierarchy.Push(string.Empty);
                        }
                        else if (c == '/')
                        {
                            _stateHistory.Push(State.Skip);
                            _state = State.Comment;
                        }
                        break;
                }

                _previous = c;
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
        /// Create a token for a field
        /// </summary>
        /// <param name="name">Name of the field</param>
        /// <returns>Token describing the field</returns>
        private Token CreateFieldToken(string name)
        {
            return new Token()
            {
                Type = ETokenType.Field,
                Data = name
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
            KeyStart,
            Key,
            KeyToken,
            KeyValueSeparator,
            ValueStart,
            String,
            StringEscape,
            Literal,
            ValueEnd,
            Skip,
            Comment,
            CommentLine,
            CommentBlock,
            CommentBlockEnd,
        }
    }
}
