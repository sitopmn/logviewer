using logviewer.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.query.Readers
{
    /// <summary>
    /// Base implementation for a log reader
    /// </summary>
    internal abstract class LogReader<T> : IDisposable
    {
        #region fields

        /// <summary>
        /// Stream providing the source data
        /// </summary>
        private readonly Stream _stream;

        /// <summary>
        /// Array of bytes to decode
        /// </summary>
        private readonly byte[] _undecoded = new byte[1024];

        /// <summary>
        /// StringBuilder for handling overflowing marks (larger than one buffer size)
        /// </summary>
        private readonly StringBuilder _markOverflow = new StringBuilder();

        /// <summary>
        /// Encoding if the input text
        /// </summary>
        private Encoding _encoding;

        /// <summary>
        /// Decoder for decoding input text
        /// </summary>
        private Decoder _decoder;

        /// <summary>
        /// Number of bytes in <see cref="_undecoded"/>
        /// </summary>
        private int _undecodedBytes;

        /// <summary>
        /// Current position within the input stream
        /// </summary>
        private long _streamPosition;

        /// <summary>
        /// Offset of the current byte in <see cref="_undecoded"/> in the input stream
        /// </summary>
        private long _currentPosition;

        /// <summary>
        /// Index within <see cref="_undecoded"/> of the next byte to decode
        /// </summary>
        private int _currentIndex;

        /// <summary>
        /// The current character
        /// </summary>
        private char _currentCharacter;

        /// <summary>
        /// Position of the beginning of the marked area
        /// </summary>
        private long _markBeginPosition = -1;

        #endregion

        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseReader"/> class
        /// </summary>
        /// <param name="stream">Stream providing the source data</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member if the file is an archive</param>
        protected LogReader(Stream stream, string file, string member)
        {
            _stream = stream;
            File = file;
            Member = member;
            Index = 0;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets a value indicating the stream contains no further data
        /// </summary>
        public bool EndOfStream
        {
            get
            {
                if (_currentIndex < _undecodedBytes)
                {
                    return false;
                }
                else
                {
                    EnsureBuffer();
                    return _currentIndex >= _undecodedBytes;
                }
            }
        }

        /// <summary>
        /// Gets the encoding of the stream
        /// </summary>
        public Encoding CurrentEncoding => _encoding;

        /// <summary>
        /// Gets the current file name
        /// </summary>
        public string File { get; }

        /// <summary>
        /// Gets the current member name
        /// </summary>
        public string Member { get; }

        /// <summary>
        /// Gets the current position of the reader
        /// </summary>
        public long Position { get; private set; }

        /// <summary>
        /// Gets the index of the current item
        /// </summary>
        public int Index { get; set; }

        #endregion

        #region public methods

        /// <summary>
        /// Seeks to the given position
        /// </summary>
        /// <param name="offset">Offset to seek to relative to the given origin</param>
        /// <param name="origin">Origin the offset refers to</param>
        /// <returns>Actual position of the stream after the seek operation</returns>
        public virtual long Seek(long offset, int index, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Current)
            {
                offset = _currentPosition + offset;
            }
            else if (origin == SeekOrigin.End)
            {
                throw new NotSupportedException();
            }

            // cancel mark on seek
            Unmark();
            
            if (offset < _currentPosition)
            {
                throw new InvalidOperationException("Cannot reverse seek");
            }
            else if (offset <= _streamPosition)
            {
                // target offset is somewhere within the buffers
                _currentIndex += (int)(offset - _currentPosition);
                Position = _currentPosition;
            }
            else
            {
                // target offset is beyond the buffers
                if (_stream.CanSeek)
                {
                    _streamPosition = _stream.Seek(offset, SeekOrigin.Begin);
                }
                else
                {
                    while (_streamPosition < offset)
                    {
                        var bytesRead = _stream.Read(_undecoded, 0, Math.Min(_undecoded.Length, (int)(offset - _streamPosition)));
                        _streamPosition += bytesRead;
                        if (bytesRead == 0)
                        {
                            throw new EndOfStreamException();
                        }
                    }
                }

                Position = _currentPosition = _streamPosition;
                _currentIndex = 0;
                _undecodedBytes = 0;
            }

            Index = index;
            return _currentPosition;
        }
        
        /// <summary>
        /// Reads an element of type <see cref="{T}"/>
        /// </summary>
        /// <returns>Read element or default(T)</returns>
        public abstract T Read();

        /// <summary>
        /// Reads elements of type <see cref="{T}"/> into a buffer
        /// </summary>
        /// <param name="buffer">Buffer to store the elements to</param>
        /// <param name="offset">Offset of the first element in the buffer</param>
        /// <param name="count">Number of elements to read</param>
        /// <returns>Number of elements actually read</returns>
        public virtual int Read(T[] buffer, int offset, int count)
        {
            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("offset + count must be smaller than buffer.Length");
            }

            var comparer = EqualityComparer<T>.Default;
            var def = default(T);
            for (var i = 0; i < count; i++)
            {
                var e = Read();
                if (comparer.Equals(e, def))
                {
                    return i;
                }
                else
                {
                    buffer[i + offset] = e;
                }
            }

            return count;
        }

        /// <summary>
        /// Reads all elements
        /// </summary>
        /// <returns>Enumerable returning all elements in the source</returns>
        public virtual IEnumerable<T> ReadAll()
        {
            while (!EndOfStream)
            {
                yield return Read();
            }
        }

        /// <summary>
        /// Closes the input stream
        /// </summary>
        public void Close()
        {
            _stream.Close();
        }

        /// <summary>
        /// Releases resources of the reader
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Reads a single character from the stream without consuming it
        /// </summary>
        /// <returns>A character or -1 on EOF</returns>
        protected int PeekChar()
        {
            var chars = new char[1];
            var bytesUsed = 0;
            var charsUsed = 0;
            var completed = false;

            if (_currentIndex == _undecodedBytes || _undecodedBytes == 0)
            {
                EnsureBuffer();
            }

            Position = _currentPosition;
            _decoder.Convert(_undecoded, _currentIndex, _undecodedBytes - _currentIndex, chars, 0, 1, false, out bytesUsed, out charsUsed, out completed);

            return chars[0];
        }

        /// <summary>
        /// Reads a single character
        /// </summary>
        /// <returns>A character or -1 on EOF</returns>
        protected int ReadChar()
        {
            var chars = new char[1];
            var bytesUsed = 0;
            var charsUsed = 0;
            var completed = false;

            if (_currentIndex == _undecodedBytes || _undecodedBytes == 0)
            {
                EnsureBuffer();
            }

            Position = _currentPosition;
            _decoder.Convert(_undecoded, _currentIndex, _undecodedBytes - _currentIndex, chars, 0, 1, false, out bytesUsed, out charsUsed, out completed);
            _currentPosition += bytesUsed;
            _currentIndex += bytesUsed;
            _currentCharacter = chars[0];

            if (_markBeginPosition >= 0)
            {
                _markOverflow.Append(_currentCharacter);
            }

            return _currentCharacter;
        }

        /// <summary>
        /// Mark the beginning of a string (including the current character) to extract
        /// </summary>
        /// <remarks>Prevents purging of the string from the decoded buffer, so string length is at max the buffer length</remarks>
        protected void MarkBegin()
        {
            _markOverflow.Clear();
            _markBeginPosition = Position;

            if (_currentCharacter != 0)
            {
                _markOverflow.Append(_currentCharacter);
            }
        }

        /// <summary>
        /// Mark the end of a string in the input (including the current character and an optional offset) and extract it
        /// </summary>
        /// <returns></returns>
        protected string MarkEnd(int offset = 0)
        {
            var result = string.Empty;

            if (_markBeginPosition < 0)
            {
                throw new InvalidOperationException("MarkEnd() called without previous call to MarkBegin()");
            }

            if (offset < 0)
            {
                _markOverflow.Remove(_markOverflow.Length + offset, Math.Abs(offset));
            }

            _markBeginPosition = -1;
            return _markOverflow.ToString();
        }

        /// <summary>
        /// Gets the starting position of the marked area
        /// </summary>
        /// <returns>Position of the marked area within the input stream</returns>
        protected long MarkPosition()
        {
            return _markBeginPosition;
        }

        /// <summary>
        /// Gets the length of the marked area
        /// </summary>
        /// <returns></returns>
        protected int MarkLength()
        {
            if (_markBeginPosition >= 0)
            {
                return _markOverflow.Length;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Cancels a marked string and allows it to be purged from the input buffer
        /// </summary>
        protected void Unmark()
        {
            _markBeginPosition = -1;
        }
        
        #endregion

        #region private methods
        
        /// <summary>
        /// Reads data from the underlying stream, decodes them and saves them to <see cref="_decoded"/>
        /// </summary>
        private void EnsureBuffer()
        {
            // skip if there are still characters in the buffer
            if (_currentIndex < _undecodedBytes)
            {
                return;
            }

            // copy undecoded bytes to the beginning of the buffer
            if (_currentIndex < _undecodedBytes)
            {
                Array.Copy(_undecoded, _currentIndex, _undecoded, 0, _undecodedBytes - _currentIndex);
            }

            // fill the undecoded buffer
            var bytesRead = _stream.Read(_undecoded, _undecodedBytes, _undecoded.Length - _undecodedBytes);
            _undecodedBytes = bytesRead;
            _streamPosition += bytesRead;
            _currentIndex = 0;

            // detect the encoding if not done yet
            if (CurrentEncoding == null)
            {
                DetectEncoding();
            }
        }

        /// <summary>
        /// Detects the encoding from undecoded bytes in <see cref="_undecoded"/>
        /// </summary>
        private void DetectEncoding()
        {
            _encoding = Encoding.Default;
            _decoder = CurrentEncoding.GetDecoder();
        }

        #endregion
    }
}
