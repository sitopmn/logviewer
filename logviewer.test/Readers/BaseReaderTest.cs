using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using logviewer.Interfaces;
using logviewer.query.Readers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace logviewer.test.Readers
{
    [TestClass]
    public class BaseReaderTest
    {
        #region protected method tests

        [TestMethod]
        public void ReadCharReturnsCorrectCharacters()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            Assert.IsTrue(text.All(c => reader.ReadChar() == c));
        }
        
        [TestMethod]
        public void EndOfStreamIsReportedAtEndOfInput()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            for (var i = 0; i < text.Length; i++) reader.ReadChar();
            Assert.IsTrue(reader.EndOfStream);
        }

        [TestMethod]
        public void ReadCharReportsMinusOneAtEndOfInput()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            for (var i = 0; i < text.Length; i++) reader.ReadChar();
            Assert.AreEqual(-1, reader.ReadChar());
        }

        [TestMethod]
        public void PeekCharReturnCorrectCharacters()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            for (var i = 0; i < text.Length; i++)
            {
                var peeked = reader.PeekChar();
                var read = reader.ReadChar();
                Assert.AreEqual(read, peeked);
            }
        }

        [TestMethod]
        public void PeekCharReturnsMinusOneOnEndOfInput()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            for (var i = 0; i < text.Length; i++) reader.ReadChar();
            Assert.AreEqual(-1, reader.PeekChar());
        }

        [TestMethod]
        public void ReadCharAdvancesPosition()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            for (var i = 0; i < text.Length; i++)
            {
                Assert.AreEqual(i, reader.Position);
                reader.ReadChar();
            }
        }

        [TestMethod]
        public void ReaderReturnsCorrectFile()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            Assert.AreEqual("file", reader.File);
        }

        [TestMethod]
        public void ReaderReturnsCorrectMember()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            Assert.AreEqual("member", reader.Member);
        }
        
        #endregion

        #region public method tests

        [TestMethod]
        public void SeekToCorrectPosition()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            reader.Seek(2000, 2000, SeekOrigin.Begin);
            Assert.AreEqual(2000, reader.Position);
        }

        [TestMethod]
        public void SeekToCorrectIndex()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            reader.Seek(2000, 2000, SeekOrigin.Begin);
            Assert.AreEqual(2000, reader.Index);
        }

        [TestMethod]
        public void ReadCharAfterSeekReturnsCorrectCharacters()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            reader.ReadChar(); // read one character to fill the buffers
            reader.Seek(2000, 2000, SeekOrigin.Begin);
            Assert.IsTrue(text.Skip(2000).All(c => reader.ReadChar() == c));
        }

        [TestMethod]
        public void SeekIntoDecodedBufferReturnsCorrectCharacters()
        {
            var text = CreateText();
            var reader = new TestReader(text, "file", "member");
            reader.ReadChar(); // read one character to fill the buffers
            reader.Seek(500, 500, SeekOrigin.Begin);
            Assert.IsTrue(text.Skip(500).All(c => reader.ReadChar() == c));
        }

        [TestMethod]
        public void Benchmark()
        {
            var rnd = new Random();
            var text = new string(Enumerable.Range(0, 1000000).Select(i => (char)('A' + rnd.Next('Z' - 'A'))).ToArray());
            var bytes = Encoding.UTF8.GetBytes(text);

            var sw = Stopwatch.StartNew();
            for (var n = 0; n < 10; n++)
            {
                var decoder = Encoding.UTF8.GetDecoder();
                var encoder = Encoding.UTF8.GetEncoder();
                var result = new StringBuilder();
                var buffer = new char[1024];
                var bytePointer = 0;
                while (true)
                {
                    var bytesUsed = 0;
                    var charsUsed = 0;
                    var completed = false;
                    decoder.Convert(bytes, bytePointer, bytes.Length - bytePointer, buffer, 0, buffer.Length, false, out bytesUsed, out charsUsed, out completed);
                    if (charsUsed == 0)
                    {
                        break;
                    }
                    else
                    {
                        bytePointer += bytesUsed;
                        result.Append(buffer, 0, charsUsed);
                    }

                    for (var i = 1; i < 30; i++)
                    {
                        encoder.GetByteCount(buffer, 0, buffer.Length * i / 31, false);
                    }
                }
            }
            sw.Stop();
            Trace.WriteLine($"Decode in blocks completed in {sw.ElapsedMilliseconds / 10} ms");

            sw.Restart();
            for (var n = 0; n < 10; n++)
            {
                var decoder = Encoding.UTF8.GetDecoder();
                var chars = new char[1];
                var bytePointer = 0;
                while (bytePointer < bytes.Length)
                {
                    var bytesUsed = 0;
                    var charsUsed = 0;
                    var completed = false;
                    decoder.Convert(bytes, bytePointer, bytes.Length - bytePointer, chars, 0, 1, false, out bytesUsed, out charsUsed, out completed);
                    bytePointer += bytesUsed;
                }
            }
            sw.Stop();
            Trace.WriteLine($"Decode in characters completed in {sw.ElapsedMilliseconds / 10} ms");

            sw.Reset();
            for (var n = 0; n < 10; n++)
            {
                var reader = new TestReader(text, "file", "member");
                sw.Start();
                reader.Read();
                sw.Stop();
            }
            Trace.WriteLine($"Reader completed in {sw.ElapsedMilliseconds / 10} ms");
        }

        #endregion

        private string CreateText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Hello World");
            for (var i = 0; i < 5000; i++) sb.Append(i % 10);
            sb.AppendLine("This is a test text");
            return sb.ToString();
        }

        private class TestReader : LogReader<string>
        {
            public TestReader(string data, string file, string member)
                : base(new MemoryStream(Encoding.Default.GetBytes(data), false), Encoding.Default, file, member)
            {
            }
            
            public new int ReadChar()
            {
                return base.ReadChar();
            }

            public new int PeekChar()
            {
                return base.PeekChar();
            }

            public override string Read()
            {
                var builder = new StringBuilder();
                while (!EndOfStream)
                {
                    builder.Append(base.ReadChar());
                }
                return builder.ToString();
            }
        }
    }
}
