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
        private readonly Stack<State> _history = new Stack<State>();

        private int _level = 0;
        
        private State _state = State.Document;

        protected JsonReader(Stream stream, Encoding encoding, string file, string member) 
            : base(stream, encoding, file, member)
        {
        }

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

        public override int Read(T[] buffer, int offset, int count)
        {
            var end = offset + count;
            var index = offset;
            while (index < end)
            {
                var r = ReadChar();
                if (r < 0)
                {
                    break;
                }

                var c = (char)r;
                switch (_state)
                {
                    // expect the start of an item
                    case State.Document:
                        if (c == '{')
                        {
                            _level += 1;
                            _history.Push(State.Document);
                            _state = State.KeyStart;
                            index = OnDocumentStart(buffer, index, Position);
                        }
                        else if (c == '/')
                        {
                            _history.Push(State.Document);
                            _state = State.Comment;
                        }
                        break;

                    // expect the start of a key
                    case State.KeyStart:
                        if (c == '"')
                        {
                            _state = State.Key;
                            index = OnPropertyStart(buffer, index);
                        }
                        else if (c == '/')
                        {
                            _state = State.Comment;
                            _history.Push(State.KeyStart);
                        }
                        else if (c == '}')
                        {
                            goto case State.ValueEnd;
                        }
                        else if (!char.IsWhiteSpace((char)c))
                        {
                            _state = State.Skip;
                            goto case State.Skip;
                        }
                        break;

                    // characters in a key
                    case State.Key:
                        if (c == '"')
                        {
                            _state = State.KeyValueSeparator;
                            index = OnPropertyEnd(buffer, index);
                        }
                        else
                        {
                            index = OnPropertyCharacter(buffer, index, c);
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
                            _history.Push(State.KeyValueSeparator);
                            _state = State.Comment;
                        }
                        else if (!char.IsWhiteSpace((char)c))
                        {
                            _state = State.Skip;
                            goto case State.Skip;
                        }
                        break;

                    case State.ValueStart:
                        if (c == '{')
                        {
                            _level += 1;
                            index = OnObjectStart(buffer, index);
                            _history.Push(State.ValueEnd);
                            _state = State.KeyStart;
                        }
                        else if (c == '}')
                        {
                            goto case State.ValueEnd;
                        }
                        else if (c == '[')
                        {
                            _state = State.Skip;
                            goto case State.Skip;
                        }
                        else if (c == '"')
                        {
                            index = OnValueStart(buffer, index);
                            _state = State.String;
                        }
                        else if (c == '/')
                        {
                            _history.Push(State.ValueStart);
                            _state = State.Comment;
                        }
                        else if (char.IsLetter(c) || char.IsDigit(c))
                        {
                            index = OnValueStart(buffer, index);
                            index = OnValueCharacter(buffer, index, c);
                            _state = State.Literal;
                        }
                        else if (!char.IsWhiteSpace((char)c))
                        {
                            index = OnValueStart(buffer, index);
                            index = OnValueCharacter(buffer, index, c);
                            _state = State.Literal;
                        }
                        break;

                    case State.Literal:
                        if (char.IsWhiteSpace((char)c) || c == ',' || c == '}')
                        {
                            index = OnValueEnd(buffer, index);
                            _state = State.ValueEnd;
                            goto case State.ValueEnd;
                        }
                        else
                        {
                            index = OnValueCharacter(buffer, index, c);
                        }
                        break;

                    case State.String:
                        if (c == '"')
                        {
                            _state = State.ValueEnd;
                            index = OnValueEnd(buffer, index);
                        }
                        else if (c == '\\')
                        {
                            _state = State.StringEscape;
                        }
                        else
                        {
                            index = OnValueCharacter(buffer, index, c);
                        }
                        break;

                    case State.StringEscape:
                        index = OnValueCharacter(buffer, index, c);
                        _state = State.String;
                        break;

                    case State.ValueEnd:
                        if (c == ',')
                        {
                            _state = State.KeyStart;
                        }
                        else if (c == '}')
                        {
                            _level -= 1;
                            _state = _history.Pop();
                            if (_level == 0)
                            {
                                index = OnDocumentCharacter(buffer, index, c);
                                index = OnDocumentEnd(buffer, index);
                            }
                            else
                            {
                                index = OnObjectEnd(buffer, index);
                            }
                        }
                        else if (c == '/')
                        {
                            _history.Push(State.ValueEnd);
                            _state = State.Comment;
                        }
                        else if (!char.IsWhiteSpace((char)c))
                        {
                            _state = State.Skip;
                            goto case State.Skip;
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
                            goto case State.Skip;
                        }
                        break;

                    // the rest of the line is a comment
                    case State.CommentLine:
                        if (c == '\n' || c == '\r')
                        {
                            _state = _history.Pop();
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
                            _state = _history.Pop();
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
                            if (_level > 0)
                            {
                                _level -= 1;
                            }

                            if (_level == 0)
                            {
                                _state = State.Document;
                                index = OnDocumentCharacter(buffer, index, c);
                                index = OnDocumentEnd(buffer, index);
                            }
                        }
                        else if (c == '{' && _level > 0)
                        {
                            _level += 1;
                        }
                        else if (c == '/')
                        {
                            _history.Push(State.Skip);
                            _state = State.Comment;
                        }
                        break;
                }

                if (_state == State.KeyStart ||
                    _state == State.Key ||
                    _state == State.KeyValueSeparator ||
                    _state == State.ValueStart ||
                    _state == State.String ||
                    _state == State.StringEscape ||
                    _state == State.Literal ||
                    _state == State.ValueEnd ||
                    _state == State.Skip)
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

        protected virtual int OnValueStart(T[] buffer, int offset) => offset;
        protected virtual int OnValueCharacter(T[] buffer, int offset, char c) => offset;
        protected virtual int OnValueEnd(T[] buffer, int offset) => offset;
        
        protected virtual int OnObjectEnd(T[] buffer, int offset) => offset;

        protected virtual int OnDocumentEnd(T[] buffer, int offset) => offset;

        private enum State
        {
            Document,
            KeyStart,
            Key,
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
