using logviewer.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    /// <summary>
    /// Implementation of a reader parsing a reduced subset of JSON (arrays are intentionally not supported as they are difficult to represent here)
    /// </summary>
    internal class JsonItemReader : JsonReader<ILogItem>
    {
        /// <summary>
        /// Builder for the message string
        /// </summary>
        private readonly StringBuilder _message = new StringBuilder();

        /// <summary>
        /// Builder for field values
        /// </summary>
        private readonly StringBuilder _value = new StringBuilder();

        /// <summary>
        /// Builder for property names
        /// </summary>
        private readonly StringBuilder _property = new StringBuilder();

        /// <summary>
        /// List of parent property names in the object hierarchy of the current document
        /// </summary>
        private readonly List<string> _hierarchy = new List<string>();

        /// <summary>
        /// Dictionary with field values
        /// </summary>
        private readonly Dictionary<string, string> _fields = new Dictionary<string, string>();

        /// <summary>
        /// Position of the item
        /// </summary>
        private long _position;

        /// <summary>
        /// Number of nested arrays
        /// </summary>
        private int _arrayLevel;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonReader{T}"/> class
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="encoding">Encoding of the source stream</param>
        /// <param name="file">File providing the source stream</param>
        /// <param name="member">Archive member providing the source stream</param>
        public JsonItemReader(Stream stream, Encoding encoding, string file, string member) 
            : base(stream, encoding, file, member)
        {
        }

        #region read documents

        /// <summary>
        /// The reader encountered a new document
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <param name="position">Position within the source file</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnDocumentStart(ILogItem[] buffer, int offset, long position)
        {
            _message.Clear();
            _fields.Clear();
            _hierarchy.Clear();
            _property.Clear();
            _value.Clear();
            _position = position;
            _arrayLevel = 0;
            return offset;
        }

        /// <summary>
        /// The reader read a character within a document
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <param name="position">Position within the source file</param>
        /// <param name="c">The current character</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnDocumentCharacter(ILogItem[] buffer, int offset, char c)
        {
            _message.Append(c);
            return offset;
        }

        /// <summary>
        /// The reader completed a document
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnDocumentEnd(ILogItem[] buffer, int offset)
        {
            var item = new LogItem(_message.ToString(), File, Member, _position, 0);

            foreach (var f in _fields)
            {
                item.Fields[f.Key] = f.Value;
            }

            buffer[offset++] = item;
            return offset;
        }

        #endregion

        #region read property names

        /// <summary>
        /// The reader encountered a property name
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnPropertyStart(ILogItem[] buffer, int offset)
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
        protected override int OnPropertyCharacter(ILogItem[] buffer, int offset, char c)
        {
            _property.Append(c);
            return offset;
        }
        
        /// <summary>
        /// The reader encountered an object value
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnObjectStart(ILogItem[] buffer, int offset)
        {
            _hierarchy.Add(_property.ToString());
            return offset;
        }

        /// <summary>
        /// The reader encountered the end of an object value
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnObjectEnd(ILogItem[] buffer, int offset)
        {
            _hierarchy.RemoveAt(_hierarchy.Count - 1);
            return offset;
        }

        /// <summary>
        /// The reader encountered the start of an array
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnArrayStart(ILogItem[] buffer, int offset)
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
        protected override int OnArrayEnd(ILogItem[] buffer, int offset)
        {
            _arrayLevel -= 1;
            return offset;
        }

        #endregion

        #region read property values

        /// <summary>
        /// The reader encountered a string value
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnValueStart(ILogItem[] buffer, int offset)
        {
            _value.Clear();
            return offset;
        }

        /// <summary>
        /// The reader encountered a literal character
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <param name="c">The current character</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnValueCharacter(ILogItem[] buffer, int offset, char c)
        {
            _value.Append(c);
            return offset;
        }

        /// <summary>
        /// The reader encountered a string character
        /// </summary>
        /// <param name="buffer">Buffer for storing tokens</param>
        /// <param name="offset">Offset of the next token to store into the buffer</param>
        /// <param name="c">The current character</param>
        /// <returns>Offset to store the next token into the buffer</returns>
        protected override int OnValueEnd(ILogItem[] buffer, int offset)
        {
            if (_arrayLevel == 0)
            {
                _fields[CreateFieldName()] = _value.ToString();
            }

            return offset;
        }

        #endregion

        /// <summary>
        /// Creates a name for the current field
        /// </summary>
        /// <returns>Name of the field</returns>
        private string CreateFieldName()
        {
            return string.Join(".", _hierarchy.Concat(new[] { _property.ToString() }));
        }
    }
}
