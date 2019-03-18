using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    /// <summary>
    /// Reader for tokenizing json formatted files
    /// </summary>
    internal class JsonTokenReader : JsonReader<Token>
    {
        /// <summary>
        /// Builder for assemblying token strings
        /// </summary>
        private readonly StringBuilder _token = new StringBuilder();

        /// <summary>
        /// Builder for assemblying property names
        /// </summary>
        private readonly StringBuilder _property = new StringBuilder();

        /// <summary>
        /// List of parent property names in the object hierarchy of the current document
        /// </summary>
        private readonly List<string> _hierarchy = new List<string>();

        /// <summary>
        /// Starting position of the current token
        /// </summary>
        private long _tokenPosition;

        /// <summary>
        /// The last character encountered in the current document
        /// </summary>
        private char _previous;

        /// <summary>
        /// Number of nested arrays
        /// </summary>
        private int _arrayLevel;

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
        /// The reader encountered a new document
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <param name="position">Position within the source file</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnDocumentStart(Token[] buffer, int offset, long position)
        {
            _token.Clear();
            _property.Clear();
            _hierarchy.Clear();
            _previous = '\0';
            _arrayLevel = 0;
            buffer[offset++] = new Token() { Type = ETokenType.Item, File = File, Member = Member, Position = position };
            return offset;
        }

        #region read fulltext tokens

        /// <summary>
        /// The reader read a character within a document
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <param name="position">Position within the source file</param>
        /// <param name="c">The current character</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnDocumentCharacter(Token[] buffer, int offset, char c)
        {
            if (_arrayLevel == 0)
            {
                var currentIsLetter = char.IsLetter(c);
                var currentIsDigit = char.IsDigit(c);
                var currentIsUpper = char.IsUpper(c);
                var previousIsLetter = char.IsLetter(_previous);
                var previousIsDigit = char.IsDigit(_previous);
                var previousIsUpper = char.IsUpper(_previous);

                if (_token.Length > 0 && (currentIsLetter != previousIsLetter || currentIsDigit != previousIsDigit || (currentIsUpper && !previousIsUpper)))
                {
                    buffer[offset++] = new Token() { Type = ETokenType.Characters, Data = _token.ToString(), File = File, Member = Member, Position = _tokenPosition };
                    _token.Clear();
                }

                if (currentIsLetter || currentIsDigit)
                {
                    if (_token.Length == 0)
                    {
                        _tokenPosition = Position;
                    }

                    _token.Append(c);
                }

                _previous = c;
            }

            return offset;
        }

        /// <summary>
        /// The reader encountered the start of an array
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnArrayStart(Token[] buffer, int offset)
        {
            _arrayLevel += 1;
            return offset;
        }

        /// <summary>
        /// The reader encountered the end of an array
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnArrayEnd(Token[] buffer, int offset)
        {
            _arrayLevel -= 1;
            return offset;
        }

        /// <summary>
        /// The reader completed a document
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnDocumentEnd(Token[] buffer, int offset)
        {
            if (_token.Length > 0)
            {
                buffer[offset++] = new Token() { Type = ETokenType.Characters, Data = _token.ToString(), File = File, Member = Member, Position = _tokenPosition };
                _token.Clear();
            }

            return offset;
        }

        #endregion

        #region read field names

        /// <summary>
        /// The reader encountered a property name
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnPropertyStart(Token[] buffer, int offset)
        {
            _property.Clear();
            return offset;
        }

        /// <summary>
        /// The reader encountered a property name character
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <param name="c">The current character</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnPropertyCharacter(Token[] buffer, int offset, char c)
        {
            _property.Append(c);
            return offset;
        }

        /// <summary>
        /// The reader encountered a literal value
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnValueStart(Token[] buffer, int offset)
        {
            if (_arrayLevel == 0)
            {
                buffer[offset++] = new Token() { Type = ETokenType.Field, Data = CreateFieldName() };
            }

            return offset;
        }
        
        /// <summary>
        /// The reader encountered an object value
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnObjectStart(Token[] buffer, int offset)
        {
            if (_arrayLevel == 0)
            {
                _hierarchy.Add(_property.ToString());
            }

            return offset;
        }

        /// <summary>
        /// The reader encountered the end of an object value
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnObjectEnd(Token[] buffer, int offset)
        {
            if (_arrayLevel == 0)
            {
                _hierarchy.RemoveAt(_hierarchy.Count - 1);
            }

            return offset;
        }

        /// <summary>
        /// Creates a name for the current field
        /// </summary>
        /// <returns>Name of the field</returns>
        private string CreateFieldName()
        {
            return string.Join(".", _hierarchy.Concat(new[] { _property.ToString() }));
        }

        #endregion
    }
}
