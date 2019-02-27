using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using logviewer.Interfaces;
using logviewer.query.Readers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace logviewer.test.Readers
{
    [TestClass]
    public class LineTokenReaderTest
    {
        [TestMethod]
        public void SingleTokenReturned()
        {
            var tokens = ReadTokens("FATAL").ToArray();
            Assert.AreEqual(2, tokens.Length);
            Assert.AreEqual(ETokenType.Line, tokens[0].Type);
            Assert.AreEqual(ETokenType.Characters, tokens[1].Type);
            Assert.AreEqual("FATAL", tokens[1].Data);
        }

        [TestMethod]
        public void MultiTokenTerminatesCorrectly()
        {
            var tokens = ReadTokens("{time:time} [*ROLL_CHANGING -> ROLL_COMPLETED").ToArray();
            Assert.AreEqual(7, tokens.Length);
            Assert.AreEqual(ETokenType.Line, tokens[0].Type);

            Assert.AreEqual(ETokenType.Characters, tokens[1].Type);
            Assert.AreEqual("time", tokens[1].Data);

            Assert.AreEqual(ETokenType.Characters, tokens[2].Type);
            Assert.AreEqual("time", tokens[2].Data);

            Assert.AreEqual(ETokenType.Characters, tokens[3].Type);
            Assert.AreEqual("ROLL", tokens[3].Data);

            Assert.AreEqual(ETokenType.Characters, tokens[4].Type);
            Assert.AreEqual("CHANGING", tokens[4].Data);

            Assert.AreEqual(ETokenType.Characters, tokens[5].Type);
            Assert.AreEqual("ROLL", tokens[5].Data);

            Assert.AreEqual(ETokenType.Characters, tokens[6].Type);
            Assert.AreEqual("COMPLETED", tokens[6].Data);
        }

        private IEnumerable<Token> ReadTokens(string line)
        {
            var reader = new LineTokenReader(new MemoryStream(Encoding.Default.GetBytes(line)), string.Empty, string.Empty);
            while (!reader.EndOfStream)
            {
                yield return reader.Read();
            }
        }
    }
}
