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
    /// <typeparam name="T"></typeparam>
    internal abstract class JsonReader<T> : LogReader<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonReader{T}"/> class
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="encoding">Encoding of the source stream</param>
        /// <param name="file">File providing the source stream</param>
        /// <param name="member">Archive member providing the source stream</param>
        protected JsonReader(Stream stream, Encoding encoding, string file, string member) 
            : base(stream, encoding, file, member)
        {
        }

        protected Document ReadDocument()
        {
            // read whitespace or comments
            while (ReadWhitespace() || ReadComment()) ;

            // read a separator character
            ReadOne(',', '\x1E');

            // read the document value
            return ReadObject();
        }

        /// <summary>
        /// Reads a single json object
        /// </summary>
        /// <returns>A <see cref="Document"/> containing the document data</returns>
        private Document ReadObject()
        {
            var doc = new Document();
            doc.Fields = new List<Field>();

            // read whitespace or comments
            while (ReadWhitespace() || ReadComment()) ;
            
            // store the starting position of the document
            doc.Position = Position;
            
            // read the opening bracket
            if (!ReadOne('{'))
            {
                return Document.Empty;
            }

            // read the key-value pairs
            while (true)
            {
                // read whitespace or comments
                while (ReadWhitespace() || ReadComment()) ;
                
                // read the property name
                var key = ReadString();
                if (key == null)
                {
                    break;
                }

                // read whitespace or comments
                while (ReadWhitespace() || ReadComment()) ;

                // read the separator
                if (!ReadOne(':'))
                {
                    return Document.Empty;
                }

                // read whitespace or comments
                while (ReadWhitespace() || ReadComment()) ;

                // read the property value
                var value = ReadValue();
                if (value is string strv)
                {
                    doc.Fields.Add(new Field() { Name = key, Value = strv, Type = 1 });
                }
                else if (value is Document docv)
                {
                    foreach (var f in docv.Fields)
                    {
                        doc.Fields.Add(new Field() { Name = $"{key}.{f.Name}", Value = f.Value, Type = f.Type });
                    }
                }
                else
                {
                    return Document.Empty;
                }
                
                // read whitespace or comments
                while (ReadWhitespace() || ReadComment()) ;

                // read the list delimiter
                if (!ReadOne(','))
                {
                    break;
                }
            }

            // read whitespace or comments
            while (ReadWhitespace() || ReadComment()) ;

            // read the closing bracket
            if (!ReadOne('}'))
            {
                return Document.Empty;
            }
            
            return doc;
        }

        /// <summary>
        /// Reads a value
        /// </summary>
        /// <returns>The value or null</returns>
        private object ReadValue()
        {
            object result;
            
            if ((result = ReadString()) != null)
            {
                return result;
            }

            if ((result = ReadNumber()) != null)
            {
                return result;
            }

            if (ReadArray())
            {
                return "[...]";
            }

            var doc = ReadObject();
            if (doc.Position >= 0)
            {
                return doc;
            }

            if (ReadLiteral("true"))
            {
                return "true";
            }

            if (ReadLiteral("false"))
            {
                return "false";
            }

            if (ReadLiteral("null"))
            {
                return "null";
            }

            return null;
        }

        /// <summary>
        /// Reads an array
        /// </summary>
        /// <returns>True if the array was read correctly</returns>
        private bool ReadArray()
        {
            var c = PeekChar();
            if (c != '[')
            {
                return false;
            }

            ReadChar();
            c = PeekChar();
            var level = 1;
            while (true)
            {
                if (c < 0)
                {
                    return false;
                }
                else if (c == ']')
                {
                    level -= 1;
                    if (level == 0)
                    {
                        return true;
                    }
                }
                else if (c == '[')
                {
                    level += 1;
                }

                c = ReadChar();
            }
        }
        
        /// <summary>
        /// Reads a comment
        /// </summary>
        /// <returns>True if the comment was read</returns>
        private bool ReadComment()
        {
            if (ReadOne('/'))
            {
                if (ReadOne('/'))
                {
                    ReadUntil('\r', '\n');
                    return true;
                }
                else if (ReadOne('*'))
                {
                    while (ReadUntil('*'))
                    {
                        if (ReadOne('/'))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Reads a string delimited by quotes
        /// </summary>
        /// <returns>The string value or null</returns>
        private string ReadString()
        {
            var builder = new StringBuilder();

            // check the opening quote
            var c = PeekChar();
            if (c != '"')
            {
                return null;
            }

            // read the opening quote
            ReadChar();

            // read the string characters up to the closing quote
            c = ReadChar();
            while (c != '"')
            {
                if (c == '\\')
                {
                    builder.Append((char)ReadChar());
                }
                else
                {
                    builder.Append((char)c);
                }

                c = ReadChar();
            }

            return builder.ToString();
        }

        /// <summary>
        /// Reads a number
        /// </summary>
        /// <returns>The number as string or null</returns>
        private string ReadNumber()
        {
            var builder = new StringBuilder();

            // read the leading minus sign
            var c = PeekChar();
            if (c == '-')
            {
                builder.Append((char)c);
                ReadChar();
                c = PeekChar();
            }

            // read the integer part
            if (!char.IsDigit((char)c))
            {
                return null;
            }
            while (char.IsDigit((char)c))
            {
                builder.Append((char)c);
                ReadChar();
                c = PeekChar();
            }

            // decimal point
            if (c == '.')
            {
                builder.Append((char)c);
                ReadChar();
                c = PeekChar();

                // read the fractional part
                if (!char.IsDigit((char)c))
                {
                    return null;
                }
                while (char.IsDigit((char)c))
                {
                    builder.Append((char)c);
                    ReadChar();
                    c = PeekChar();
                }
            }

            // read the exponential part
            if (c == 'e' || c == 'E')
            {
                builder.Append((char)c);
                ReadChar();
                c = PeekChar();

                if (c == '+' || c == '-')
                {
                    builder.Append((char)c);
                    ReadChar();
                    c = PeekChar();
                }

                // read the exponential part
                if (!char.IsDigit((char)c))
                {
                    return null;
                }
                while (char.IsDigit((char)c))
                {
                    builder.Append((char)c);
                    ReadChar();
                    c = PeekChar();
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Reads whitespace
        /// </summary>
        /// <returns>True if whitespace was read</returns>
        private bool ReadWhitespace()
        {
            if (char.IsWhiteSpace((char)PeekChar()))
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
        /// Reads a literal string
        /// </summary>
        /// <param name="sequence">String to read</param>
        /// <returns>True if the string was read</returns>
        private bool ReadLiteral(string sequence)
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
        private bool ReadOne(params char[] chars)
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
        private bool ReadUntil(params char[] delim)
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
                    return true;
                }
                else
                {
                    ReadChar();
                }
            }
        }

        /// <summary>
        /// Struct representing a document
        /// </summary>
        protected struct Document
        {
            /// <summary>
            /// Empty document
            /// </summary>
            public static readonly Document Empty = new Document() { Position = -1 };

            /// <summary>
            /// Starting position of the document within the source
            /// </summary>
            public long Position;

            /// <summary>
            /// List of fields within the document
            /// </summary>
            public List<Field> Fields;
        }

        /// <summary>
        /// Struct representing a field within a document
        /// </summary>
        protected struct Field
        {
            /// <summary>
            /// Empty field
            /// </summary>
            public static readonly Field Empty = new Field() { Type = 0 };

            /// <summary>
            /// Name of the field
            /// </summary>
            public string Name;

            /// <summary>
            /// Value of the field
            /// </summary>
            public string Value;

            /// <summary>
            /// Data type of the value
            /// </summary>
            public int Type;
        }
    }
}
