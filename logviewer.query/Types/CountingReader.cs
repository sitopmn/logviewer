using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.query
{
    /// <summary>
    /// A <see cref="StreamReader"/> like reader implementation which decodes bytes into a string and additionally returns the number of bytes consumed per line
    /// </summary>
    internal class CountingReader : TextReader
    {
        /// <summary>
        /// Stream providing the source data
        /// </summary>
        private readonly Stream _stream;
        
        /// <summary>
        /// Buffer for undecoded bytes
        /// </summary>
        private readonly byte[] _bytes = new byte[1024];

        /// <summary>
        /// Delegate for reporting progress
        /// </summary>
        private readonly Action<long> _progress;

        /// <summary>
        /// Token for cancelling the operation
        /// </summary>
        private readonly CancellationToken _cancellation;

        /// <summary>
        /// Encoding used on the input
        /// </summary>
        private Encoding _encoding;

        /// <summary>
        /// Decoder used for decoding bytes into characters
        /// </summary>
        private Decoder _decoder;

        /// <summary>
        /// Number of bytes in the buffer
        /// </summary>
        private int _bytesLength = 0;

        /// <summary>
        /// Next byte to process in the buffer. Rule: 0 &lt;= <see cref="_bytesPointer"/> &lt;= <see cref="_bytesLength"/>
        /// </summary>
        private int _bytesPointer = 0;

        /// <summary>
        /// Position of the stream
        /// </summary>
        private long _streamPosition = 0;

        /// <summary>
        /// Position of the reader (always less than the <see cref="_streamPosition"/>
        /// </summary>
        private long _readerPosition = 0;

        /// <summary>
        /// Last position reported as progress
        /// </summary>
        private long _lastReportedPosition = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="CountingReader"/> class
        /// </summary>
        /// <param name="stream">Stream providing the source data</param>
        public CountingReader(Stream stream)
            : this(stream, null, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CountingReader"/> class
        /// </summary>
        /// <param name="stream">Stream providing the source data</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> for cancelling the operation</param>
        public CountingReader(Stream stream, CancellationToken cancellationToken)
            : this(stream, null, cancellationToken)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CountingReader"/> class
        /// </summary>
        /// <param name="stream">Stream providing the source data</param>
        /// <param name="progress">Action to report progress</param>
        public CountingReader(Stream stream, Action<long> progress)
            : this(stream, progress, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CountingReader"/> class
        /// </summary>
        /// <param name="stream">Stream providing the source data</param>
        /// <param name="progress">Action to report progress</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> for cancelling the operation</param>
        public CountingReader(Stream stream, Action<long> progress, CancellationToken cancellationToken)
        {
            _stream = stream;
            _progress = progress;
            _cancellation = cancellationToken;
        }

        /// <summary>
        /// Gets a value indicating the stream contains no further data
        /// </summary>
        public bool EndOfStream
        {
            get
            {
                if (_decoder == null)
                {
                    if (FillBuffer() == 0)
                    {
                        return true;
                    }
                }

                if (_decoder.GetCharCount(_bytes, _bytesPointer, _bytesLength - _bytesPointer) > 0)
                {
                    return false;
                }

                return FillBuffer() == 0;
            }
        }

        /// <summary>
        /// Gets the encoding of the stream
        /// </summary>
        public Encoding CurrentEncoding
        {
            get
            {
                if (_decoder == null)
                {
                    if (FillBuffer() == 0)
                    {
                        return null;
                    }
                }

                return _encoding;
            }
        }
        
        /// <summary>
        /// Reads a single character from the stream without consuming it
        /// </summary>
        /// <returns>A character or -1</returns>
        public override int Peek()
        {
            if (_bytesPointer == _bytesLength && FillBuffer() == 0)
            {
                return -1;
            }

            var chars = new char[1];
            var bytesUsed = 0;
            var charsUsed = 0;
            var completed = false;
            _decoder.Convert(_bytes, _bytesPointer, _bytesLength - _bytesPointer, chars, 0, chars.Length, true, out bytesUsed, out charsUsed, out completed);

            if (charsUsed == 0)
            {
                return -1;
            }
            else
            {
                return chars[0];
            }
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (_bytesPointer == _bytesLength && FillBuffer() == 0)
            {
                return 0;
            }

            var bytesUsed = 0;
            var charsUsed = 0;
            var completed = false;
            _decoder.Convert(_bytes, _bytesPointer, _bytesLength - _bytesPointer, buffer, index, count, true, out bytesUsed, out charsUsed, out completed);
            _bytesPointer += bytesUsed;
            _readerPosition += bytesUsed;

            return charsUsed;
        }

        /// <summary>
        /// Reads a single character from the stream
        /// </summary>
        /// <returns>A character or -1</returns>
        public override int Read()
        {
            if (_bytesPointer == _bytesLength && FillBuffer() == 0)
            {
                return -1;
            }

            var chars = new char[1];
            var bytesUsed = 0;
            var charsUsed = 0;
            var completed = false;
            _decoder.Convert(_bytes, _bytesPointer, _bytesLength - _bytesPointer, chars, 0, chars.Length, true, out bytesUsed, out charsUsed, out completed);
            _bytesPointer += bytesUsed;
            _readerPosition += bytesUsed;
            
            if (charsUsed == 0)
            {
                return -1;
            }
            else
            {
                return chars[0];
            }
        }

        /// <summary>
        /// Reads a line from the stream
        /// </summary>
        /// <returns>A string containing the line data or null</returns>
        public override string ReadLine()
        {
            var temp = 0;
            return ReadLine(out temp);
        }

        /// <summary>
        /// Reads a line from the stream
        /// </summary>
        /// <param name="bytesRead">Number of bytes consumed</param>
        /// <returns>A string containing the line data or null</returns>
        public string ReadLine(out int bytesRead)
        {
            bytesRead = 0;

            // check for end of input
            if (EndOfStream)
            {
                return null;
            }

            var sb = new StringBuilder();
            var chars = new char[1024];
            var bytesUsed = 0;
            var charsUsed = 0;
            var completed = false;
            var end = -1;

            // find an end-of-line in the undecoded buffer            
            while (end < 0)
            {
                // check if we are at the end of the stream
                if (_bytesPointer == _bytesLength && FillBuffer() == 0)
                {
                    break;
                }

                // decode the complete buffer by default
                var decodeTo = _bytesLength;
                
                // try to find a line delimiter
                for (var i = _bytesPointer; i < _bytesLength; i++)
                {
                    var b = _bytes[i];
                    if (b == '\r' || b == '\n')
                    {
                        end = i;
                        break;
                    }
                }

                // read to the end of line if within the buffer
                if (end >= 0)
                {
                    decodeTo = end;
                }

                // decode all bytes up to the end of the buffer or up to the line delimiter
                _decoder.Convert(_bytes, _bytesPointer, decodeTo - _bytesPointer, chars, 0, chars.Length, true, out bytesUsed, out charsUsed, out completed);
                _bytesPointer += bytesUsed;
                bytesRead += bytesUsed;
                sb.Append(chars, 0, charsUsed);

                // read the line delimiter taking its length into account for position tracking
                if (end >= 0)
                {
                    // read the first line delimiter away
                    var a = Read();
                    bytesRead += 1;
                    _readerPosition -= 1;

                    // read the second delimiter if present
                    var b = Peek();
                    if (a == '\r' && b == '\n' || a == '\n' && b == '\r')
                    {
                        Read();
                        bytesRead += 1;
                        _readerPosition -= 1;
                    }
                }
            }

            _readerPosition += bytesRead;
            return sb.ToString();
        }

        /// <summary>
        /// Discards buffered data
        /// </summary>
        public void DiscardBufferedData()
        {
            if (_stream.CanSeek)
            {
                _streamPosition = _stream.Position;
            }

            _bytesPointer = _bytesLength = 0;
            _readerPosition = _streamPosition;
        }

        /// <summary>
        /// Seeks to the given position
        /// </summary>
        /// <param name="offset">Offset to seek to relative to the given origin</param>
        /// <param name="origin">Origin the offset refers to</param>
        /// <returns>Actual position of the stream after the seek operation</returns>
        public long Seek(long offset, SeekOrigin origin)
        {
            // seek to the desired position directly if the stream supports it, alternatively read to the given position
            if (_stream.CanSeek)
            {
                _streamPosition = _stream.Seek(offset, origin);

                // invalidate the buffer
                DiscardBufferedData();
            }
            else
            {
                // get the actual target position
                var target = 0L;
                if (origin == SeekOrigin.Begin)
                {
                    target = offset;
                }
                else if (origin == SeekOrigin.Current)
                {
                    target = _streamPosition + offset;
                }
                else
                {
                    throw new InvalidOperationException();
                }

                if (target < _streamPosition && target >= _readerPosition)
                {
                    // the target position is already contained in the buffer, so just skip forward
                    var bytesToConsume = target - _readerPosition;
                    _readerPosition += bytesToConsume;
                    _bytesPointer += (int)bytesToConsume;
                    if (_bytesPointer > _bytesLength)
                    {
                        throw new InvalidOperationException("Bytes pointer advanced beyond number of bytes in buffer");
                    }
                }
                else if (target >= _streamPosition)
                {
                    // read from the stream until the target position is reached
                    var buffer = new byte[4096];
                    while (_streamPosition < target)
                    {
                        _streamPosition += _stream.Read(buffer, 0, (int)Math.Min(target - _streamPosition, buffer.Length));
                    }

                    // invalidate the buffer
                    DiscardBufferedData();
                }
                else
                {
                    throw new InvalidOperationException("Cannot reverse seek on streams which do not support native seeking");
                }
            }

            // report progress
            ReportProgress();

            // return the new position of the reader
            return _readerPosition;
        }

        /// <summary>
        /// Fills the <see cref="_decoded"/> buffer with characters
        /// </summary>
        /// <returns>Number of characters decoded</returns>
        private int FillBuffer()
        {
            // copy remaining buffer contents to the front
            if (_bytesLength > 0)
            {
                var count = _bytesLength - _bytesPointer;
                Array.Copy(_bytes, _bytesPointer, _bytes, 0, count);
                _bytesPointer = 0;
                _bytesLength = count;
            }

            // read some data to decode into the buffer
            var read = _stream.Read(_bytes, _bytesLength, _bytes.Length - _bytesLength);
            _bytesLength += read;
            _streamPosition += read;
            
            // detect the encoding if not done yet
            if (_encoding == null)
            {
                DetectEncoding();
            }

            // report progress
            ReportProgress();

            return read;
        }

        /// <summary>
        /// Reports progress and checks the cancellation
        /// </summary>
        private void ReportProgress()
        {
            _cancellation.ThrowIfCancellationRequested();

            if (_progress != null)
            {
                var delta = _streamPosition - _lastReportedPosition;
                if (delta > 512 * 1024)
                {
                    _progress(delta);
                    _lastReportedPosition = _streamPosition;
                }
            }
        }

        /// <summary>
        /// Detects the encoding of the input
        /// </summary>
        private void DetectEncoding()
        {
            Encoding encoding = null;

            // try to find the encoding from byte order marks
            if (_bytesLength - _bytesPointer >= 2)
            {
                if (_bytes[_bytesPointer + 0] == 0xff && _bytes[_bytesPointer + 1] == 0xfe) encoding = Encoding.Unicode; //UTF-16LE
                if (_bytes[_bytesPointer + 0] == 0xfe && _bytes[_bytesPointer + 1] == 0xff) encoding = Encoding.BigEndianUnicode; //UTF-16BE
                if (encoding != null) _bytesPointer += 2;
            }
            if (_bytesLength - _bytesPointer >= 3)
            {
                if (_bytes[_bytesPointer + 0] == 0x2b && _bytes[_bytesPointer + 1] == 0x2f && _bytes[_bytesPointer + 2] == 0x76) encoding = Encoding.UTF7;
                if (_bytes[_bytesPointer + 0] == 0xef && _bytes[_bytesPointer + 1] == 0xbb && _bytes[_bytesPointer + 2] == 0xbf) encoding = Encoding.UTF8;
                if (encoding != null) _bytesPointer += 3;
            }
            if (_bytesLength - _bytesPointer >= 4)
            {
                if (_bytes[_bytesPointer + 0] == 0x00 && _bytes[_bytesPointer + 1] == 0x00 && _bytes[_bytesPointer + 2] == 0xfe && _bytes[_bytesPointer + 3] == 0xff) encoding = Encoding.UTF32;
                if (encoding != null) _bytesPointer += 4;
            }

            // no definitive encoding given, try some heuristics on the data in the buffer
            if (encoding == null)
            {
                if (_bytesPointer > 0)
                {
                    Array.Copy(_bytes, _bytesPointer, _bytes, 0, _bytesLength - _bytesPointer);
                    _bytesLength -= _bytesPointer;
                    _bytesPointer = 0;
                }

                IsTextUnicodeFlags output = IsTextUnicodeFlags.IS_TEXT_UNICODE_UNICODE_MASK;
                if (IsTextUnicode(_bytes, _bytesLength, ref output))
                {
                    Trace.WriteLine($"AutoDetected text: {output}");
                }
            }

            // nope, just use the default encoding
            if (encoding == null)
            {
                encoding = Encoding.Default;
            }

            _encoding = encoding;
            _decoder = encoding.GetDecoder();
        }

        [DllImport("Advapi32", SetLastError = false)]
        private static extern bool IsTextUnicode(byte[] buf, int len, ref IsTextUnicodeFlags opt);

        [Flags]
        private enum IsTextUnicodeFlags : int
        {
            IS_TEXT_UNICODE_ASCII16 = 0x0001,
            IS_TEXT_UNICODE_REVERSE_ASCII16 = 0x0010,

            IS_TEXT_UNICODE_STATISTICS = 0x0002,
            IS_TEXT_UNICODE_REVERSE_STATISTICS = 0x0020,

            IS_TEXT_UNICODE_CONTROLS = 0x0004,
            IS_TEXT_UNICODE_REVERSE_CONTROLS = 0x0040,

            IS_TEXT_UNICODE_SIGNATURE = 0x0008,
            IS_TEXT_UNICODE_REVERSE_SIGNATURE = 0x0080,

            IS_TEXT_UNICODE_ILLEGAL_CHARS = 0x0100,
            IS_TEXT_UNICODE_ODD_LENGTH = 0x0200,
            IS_TEXT_UNICODE_DBCS_LEADBYTE = 0x0400,
            IS_TEXT_UNICODE_NULL_BYTES = 0x1000,

            IS_TEXT_UNICODE_UNICODE_MASK = 0x000F,
            IS_TEXT_UNICODE_REVERSE_MASK = 0x00F0,
            IS_TEXT_UNICODE_NOT_UNICODE_MASK = 0x0F00,
            IS_TEXT_UNICODE_NOT_ASCII_MASK = 0xF000
        }
    }
}
