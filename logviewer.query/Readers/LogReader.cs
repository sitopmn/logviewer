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
        /// Array of decoded characters
        /// </summary>
        private readonly char[] _decoded = new char[1024];

        /// <summary>
        /// Encoding if the input text
        /// </summary>
        private readonly Encoding _encoding;

        /// <summary>
        /// Decoder for decoding input text
        /// </summary>
        private readonly Decoder _decoder;
        
        /// <summary>
        /// Number of bytes in <see cref="_undecoded"/>
        /// </summary>
        private int _undecodedBytes;

        /// <summary>
        /// Number of characters in <see cref="_decoded"/>
        /// </summary>
        private int _decodedChars;
        
        /// <summary>
        /// Index within <see cref="_undecoded"/> of the next byte to decode
        /// </summary>
        private int _decodedIndex;
        
        /// <summary>
        /// Position of the next byte to read in the input stream
        /// </summary>
        private long _streamPosition;

        /// <summary>
        /// Current position within the input stream
        /// </summary>
        private long _currentPosition;
        
        #endregion

        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseReader"/> class
        /// </summary>
        /// <param name="stream">Stream providing the source data</param>
        /// <param name="encoding">Encoding of the source data</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member if the file is an archive</param>
        protected LogReader(Stream stream, Encoding encoding, string file, string member)
        {
            _encoding = encoding;
            _decoder = _encoding.GetDecoder();
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
        public bool EndOfStream => !EnsureBuffer();

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
        public long Position => _currentPosition;

        /// <summary>
        /// Gets the index of the current item
        /// </summary>
        public int Index { get; protected set; }

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
            // calculate the target position 
            if (origin == SeekOrigin.Current)
            {
                offset = _currentPosition + offset;
            }
            else if (origin == SeekOrigin.End)
            {
                throw new NotSupportedException();
            }

            if (offset < _currentPosition) 
            {
                throw new InvalidOperationException("Cannot reverse seek");
            }
            else if (offset <= _streamPosition)
            {
                // target offset is somewhere within the buffers
                while (_currentPosition < offset)
                {
                    _currentPosition += _encoding.GetByteCount(_decoded, _decodedIndex++, 1);
                }
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

                _currentPosition = _streamPosition;
                _decodedChars = 0;
                _decodedIndex = 0;
                _undecodedBytes = 0;
            }
            
            Index = index;
            return offset;
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
        public abstract int Read(T[] buffer, int offset, int count);

        /// <summary>
        /// Reads all elements
        /// </summary>
        /// <returns>Enumerable returning all elements in the source</returns>
        public virtual IEnumerable<T> ReadAll()
        {
            var comparer = EqualityComparer<T>.Default;
            var def = default(T);
            while (!EndOfStream)
            {
                var item = Read();
                if (comparer.Equals(item, def))
                {
                    break;
                }
                else
                {
                    yield return item;
                }
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
            if (!EnsureBuffer())
            {
                return -1;
            }

            return _decoded[_decodedIndex];
        }

        /// <summary>
        /// Reads a single character
        /// </summary>
        /// <returns>A character or -1 on EOF</returns>
        protected int ReadChar()
        {
            if (!EnsureBuffer())
            {
                return -1;
            }

            var c = _decoded[_decodedIndex];
            _currentPosition += _encoding.GetByteCount(_decoded, _decodedIndex, 1);
            _decodedIndex += 1;
            return c;
        }

        #endregion

        #region private methods
        
        /// <summary>
        /// Reads data from the underlying stream, decodes them and saves them to <see cref="_decoded"/>
        /// </summary>
        private bool EnsureBuffer()
        {
            // check if the decoded buffer is completely consumed
            if (_decodedIndex < _decodedChars)
            {
                return true;
            }

            // read bytes from the input stream
            var bytesRead = _stream.Read(_undecoded, _undecodedBytes, _undecoded.Length - _undecodedBytes);
            if (bytesRead == 0)
            {
                return false;
            }
            
            _undecodedBytes += bytesRead;
            _streamPosition += bytesRead;

            // decode the undecoded buffer
            var bytesUsed = 0;
            var completed = false;
            _decoder.Convert(_undecoded, 0, _undecodedBytes, _decoded, 0, _decoded.Length, false, out bytesUsed, out _decodedChars, out completed);
            _undecodedBytes -= bytesUsed;
            _decodedIndex = 0;

            // copy undecoded bytes to the start of the buffer
            if (!completed)
            {
                Array.Copy(_undecoded, bytesUsed, _undecoded, 0, _undecodedBytes - bytesUsed);
            }
            
            return true;
        }
        
        #endregion
    }
}
