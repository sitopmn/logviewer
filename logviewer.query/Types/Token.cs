using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query
{
    /// <summary>
    /// Struct containing log token
    /// </summary>
    public struct Token
    {
        /// <summary>
        /// Type of the token
        /// </summary>
        public ETokenType Type;

        /// <summary>
        /// Data represented by the token
        /// </summary>
        public string Data;

        /// <summary>
        /// Name of the containing the token
        /// </summary>
        public string File;

        /// <summary>
        /// Name of the archive member containing the token
        /// </summary>
        public string Member;

        /// <summary>
        /// Position of the token within the source file or archive member
        /// </summary>
        public long Position;

        /// <summary>
        /// Indicates the position of the token relative to the next token is exact
        /// </summary>
        public bool IsExact;
        
        public Token(ETokenType type, string data, string file, string member, long position, bool isExact)
        {
            Type = type;
            Data = data;
            File = file;
            Member = member;
            Position = position;
            IsExact = isExact;
        }

        public Token(ETokenType type, string data, string file, string member, long position)
        {
            Type = type;
            Data = data;
            File = file;
            Member = member;
            Position = position;
            IsExact = false;
        }

        public Token(ETokenType type, string file, string member, long position)
        {
            Type = type;
            Data = string.Empty;
            File = file;
            Member = member;
            Position = position;
            IsExact = false;
        }
    }

    /// <summary>
    /// Token types
    /// </summary>
    public enum ETokenType
    {
        /// <summary>
        /// Invalid token
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// Marks the start of a new item
        /// </summary>
        Item = 1,

        /// <summary>
        /// Marks a field with the given data as a name
        /// </summary>
        Field = 2,

        /// <summary>
        /// Marks a token consisting of character data
        /// </summary>
        Characters = 3,
    }    
}
