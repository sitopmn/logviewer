using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    internal abstract class JsonReader<T> : LogReader<T>
    {
        /// <summary>
        /// States which process characters as document characters
        /// </summary>
        private readonly State[] _documentStates = new State[]
                {
                    State.KeyStart,
                    State.KeyCharacters,
                    State.KeyValueSeparator,
                    State.KeyEscape,
                    State.ValueStart,
                    State.ValueLiteral,
                    State.ValueString,
                    State.ValueStringEscape,
                    State.ValueEnd
                };

        /// <summary>
        /// Context stack of the parser
        /// </summary>
        private readonly Stack<Context> _context = new Stack<Context>();
        
        /// <summary>
        /// Current state of the parser
        /// </summary>
        private State _state = State.Root;

        /// <summary>
        /// Number of braces within the current document
        /// </summary>
        private int _braces = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonReader{T}"/> class
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="encoding">Encoding of the source stream</param>
        /// <param name="file">Name of the source file</param>
        /// <param name="member">Archive member if the source is an archive</param>
        protected JsonReader(Stream stream, Encoding encoding, string file, string member) 
            : base(stream, encoding, file, member)
        {
            _context.Push(Context.Root);
        }

        /// <summary>
        /// Reads a single item
        /// </summary>
        /// <returns>Item read from the source</returns>
        public override T Read()
        {
            var buffer = new T[1];
            if (Read(buffer, 0, 1) == 0)
            {
                return default(T);
            }
            else
            {
                return buffer[0];
            }
        }

        /// <summary>
        /// Reads multiple items into a buffer
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="offset">Offset of the first item in the target buffer</param>
        /// <param name="count">Number of items to read</param>
        /// <returns>Number of items read into the buffer</returns>
        public override int Read(T[] buffer, int offset, int count)
        {
            var index = offset;

            while (index < offset + count)
            {
                var r = ReadChar();
                if (r < 0) break;
                var c = (char)r;

                // count opening and closing braces
                if (c == '{')
                {
                    _braces += 1;
                }
                else if (c == '}')
                {
                    _braces -= 1;
                }

                // state machine to parse the input
                switch (_state)
                {
                    case State.Root:
                        if (c == '{')
                        {
                            _braces = 1;
                            _state = State.KeyStart;
                            _context.Push(Context.Object);
                            index = OnDocumentStart(buffer, index, Position);
                        }
                        else if (c == '/')
                        {
                            _state = State.Comment;
                        }
                        break;
                        
                    case State.KeyStart:
                        if (c == '"')
                        {
                            _state = State.KeyCharacters;
                            index = OnPropertyStart(buffer, index);
                        }
                        else if (!char.IsWhiteSpace(c))
                        {
                            _state = State.Error;
                        }
                        break;

                    case State.KeyCharacters:
                        if (c == '"')
                        {
                            _state = State.KeyValueSeparator;
                            index = OnPropertyEnd(buffer, index);
                        }
                        else if (c == '\\')
                        {
                            _state = State.KeyEscape;
                        }
                        else
                        {
                            index = OnPropertyCharacter(buffer, index, c);
                        }
                        break;

                    case State.KeyEscape:
                        _state = State.KeyCharacters;
                        index = OnPropertyCharacter(buffer, index, c);
                        break;

                    case State.KeyValueSeparator:
                        if (c == ':')
                        {
                            _state = State.ValueStart;
                        }
                        else if (!char.IsWhiteSpace(c))
                        {
                            _state = State.Error;
                        }
                        break;

                    case State.ValueStart:
                        if (c == '"')
                        {
                            _state = State.ValueString;
                            index = OnValueStart(buffer, index);
                        }
                        else if (c == '{')
                        {
                            _context.Push(Context.Object);
                            index = OnObjectStart(buffer, index);
                            _state = State.KeyStart;
                        }
                        else if (c == '[')
                        {
                            _context.Push(Context.Array);
                            index = OnArrayStart(buffer, index);
                            _state = State.ValueStart;
                        }
                        else if (!char.IsWhiteSpace(c))
                        {
                            _state = State.ValueLiteral;
                            index = OnValueStart(buffer, index);
                            index = OnValueCharacter(buffer, index, c);
                        }

                        if (_state != State.ValueStart && _context.Peek() == Context.Array)
                        {
                            index = OnArrayItem(buffer, index);
                        }
                        break;

                    case State.ValueString:
                        if (c == '"')
                        {
                            _state = State.ValueEnd;
                            index = OnValueEnd(buffer, index);
                        }
                        else if (c == '\\')
                        {
                            _state = State.ValueStringEscape;
                        }
                        else
                        {
                            index = OnValueCharacter(buffer, index, c);
                        }
                        break;

                    case State.ValueStringEscape:
                        _state = State.ValueString;
                        index = OnValueCharacter(buffer, index, c);
                        break;

                    case State.ValueLiteral:
                        if (char.IsWhiteSpace(c) || c == '}' || c == ']' || c == ',')
                        {
                            _state = State.ValueEnd;
                            index = OnValueEnd(buffer, index);
                            goto case State.ValueEnd;
                        }
                        else
                        {
                            index = OnValueCharacter(buffer, index, c);
                        }
                        break;

                    case State.ValueEnd:
                        if (_context.Peek() == Context.Object)
                        {
                            if (c == '}')
                            {
                                _context.Pop();
                                if (_context.Peek() == Context.Root)
                                {
                                    index = OnDocumentEnd(buffer, index);
                                    _state = State.Root;
                                }
                                else
                                {
                                    index = OnObjectEnd(buffer, index);
                                    _state = State.ValueEnd;
                                }
                            }
                            else if (c == ',')
                            {
                                _state = State.KeyStart;
                            }
                            else if (!char.IsWhiteSpace(c))
                            {
                                _state = State.Error;
                            }
                        }
                        else if (_context.Peek() == Context.Array)
                        {
                            if (c == ']')
                            {
                                _context.Pop();
                                index = OnArrayEnd(buffer, index);
                                _state = State.ValueEnd;
                            }
                            else if (c == ',')
                            {
                                index = OnArrayItem(buffer, index);
                                _state = State.ValueStart;
                            }
                            else if (!char.IsWhiteSpace(c))
                            {
                                _state = State.Error;
                            }
                        }
                        break;

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
                            _state = State.Error;
                        }
                        break;

                    case State.CommentLine:
                        if (c == '\r' || c == '\n')
                        {
                            if (_context.Peek() == Context.Root)
                            {
                                _state = State.Root;
                            }
                            else
                            {
                                _state = State.Error;
                            }
                        }
                        break;

                    case State.CommentBlock:
                        if (c == '*')
                        {
                            _state = State.CommentBlockEnd;
                        }
                        break;

                    case State.CommentBlockEnd:
                        if (c == '/')
                        {
                            if (_context.Peek() == Context.Root)
                            {
                                _state = State.Root;
                            }
                            else
                            {
                                _state = State.Error;
                            }
                        }
                        else
                        {
                            _state = State.CommentBlock;
                        }
                        break;

                    case State.Error:
                        if (_braces == 0)
                        {
                            index = OnDocumentEnd(buffer, index);
                            _state = State.Root;
                            _context.Clear();
                            _context.Push(Context.Root);
                        }
                        break;
                }
                // process all document characters
                if (Array.IndexOf(_documentStates, _state) >= 0)
                {
                    index = OnDocumentCharacter(buffer, index, c);
                }
            }

            return index - offset;
        }

        protected virtual int OnDocumentStart(T[] buffer, int offset, long position) => offset;
        protected virtual int OnDocumentCharacter(T[] buffer, int offset, char c) => offset;

        protected virtual int OnObjectStart(T[] buffer, int offset) => offset;

        protected virtual int OnPropertyStart(T[] buffer, int offset) => offset;
        protected virtual int OnPropertyCharacter(T[] buffer, int offset, char c) => offset;
        protected virtual int OnPropertyEnd(T[] buffer, int offset) => offset;

        protected virtual int OnArrayStart(T[] buffer, int offset) => offset;
        protected virtual int OnArrayItem(T[] buffer, int offset) => offset;
        protected virtual int OnArrayEnd(T[] buffer, int offset) => offset;

        protected virtual int OnValueStart(T[] buffer, int offset) => offset;
        protected virtual int OnValueCharacter(T[] buffer, int offset, char c) => offset;
        protected virtual int OnValueEnd(T[] buffer, int offset) => offset;
        
        protected virtual int OnObjectEnd(T[] buffer, int offset) => offset;

        protected virtual int OnDocumentEnd(T[] buffer, int offset) => offset;

        /// <summary>
        /// Enumeration of the states of the parser
        /// </summary>
        private enum State
        {
            Root,
            Comment,
            CommentLine,
            CommentBlock,
            CommentBlockEnd,
            Error,
            KeyStart,
            KeyCharacters,
            KeyValueSeparator,
            KeyEscape,
            ValueStart,
            ValueLiteral,
            ValueString,
            ValueStringEscape,
            ValueEnd
        }

        /// <summary>
        /// Enumeration of parser contexts
        /// </summary>
        private enum Context
        {
            Root,
            Object,
            Array,
        }
    }
}
