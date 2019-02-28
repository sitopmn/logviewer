using System;
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
                : base(new MemoryStream(Encoding.Default.GetBytes(data), false), file, member)
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
                throw new NotImplementedException();
            }
        }
    }
}
