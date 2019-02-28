using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using logviewer.Interfaces;
using logviewer.query;
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
            Assert.AreEqual(ETokenType.Item, tokens[0].Type);
            Assert.AreEqual(ETokenType.Characters, tokens[1].Type);
            Assert.AreEqual("FATAL", tokens[1].Data);
        }

        [TestMethod]
        public void MultiTokenTerminatesCorrectly()
        {
            var tokens = ReadTokens("{time:time} [*ROLL_CHANGING -> ROLL_COMPLETED").ToArray();
            Assert.AreEqual(7, tokens.Length);
            Assert.AreEqual(0, tokens[0].Position);
            Assert.AreEqual(ETokenType.Item, tokens[0].Type);

            Assert.AreEqual(ETokenType.Characters, tokens[1].Type);
            Assert.AreEqual(1, tokens[1].Position);
            Assert.AreEqual("time", tokens[1].Data);

            Assert.AreEqual(ETokenType.Characters, tokens[2].Type);
            Assert.AreEqual(6, tokens[2].Position);
            Assert.AreEqual("time", tokens[2].Data);

            Assert.AreEqual(ETokenType.Characters, tokens[3].Type);
            Assert.AreEqual(14, tokens[3].Position);
            Assert.AreEqual("ROLL", tokens[3].Data);

            Assert.AreEqual(ETokenType.Characters, tokens[4].Type);
            Assert.AreEqual(19, tokens[4].Position);
            Assert.AreEqual("CHANGING", tokens[4].Data);

            Assert.AreEqual(ETokenType.Characters, tokens[5].Type);
            Assert.AreEqual(31, tokens[5].Position);
            Assert.AreEqual("ROLL", tokens[5].Data);

            Assert.AreEqual(ETokenType.Characters, tokens[6].Type);
            Assert.AreEqual(36, tokens[6].Position);
            Assert.AreEqual("COMPLETED", tokens[6].Data);
        }

        [TestMethod]
        public void TwoCrLfLinesTokenizedCorrectly()
        {
            var tokens = ReadTokens("Hello\r\nWorld").ToArray();
            Assert.AreEqual(4, tokens.Length);

            Assert.AreEqual(0, tokens[0].Position);
            Assert.AreEqual(ETokenType.Item, tokens[0].Type);

            Assert.AreEqual(ETokenType.Characters, tokens[1].Type);
            Assert.AreEqual(0, tokens[1].Position);
            Assert.AreEqual("Hello", tokens[1].Data);

            Assert.AreEqual(7, tokens[2].Position);
            Assert.AreEqual(ETokenType.Item, tokens[2].Type);

            Assert.AreEqual(ETokenType.Characters, tokens[3].Type);
            Assert.AreEqual(7, tokens[3].Position);
            Assert.AreEqual("World", tokens[3].Data);
        }

        [TestMethod]
        public void TwoLinesTokenizedCorrectly()
        {
            var tokens = ReadTokens("Hello\rWorld").ToArray();
            Assert.AreEqual(4, tokens.Length);

            Assert.AreEqual(0, tokens[0].Position);
            Assert.AreEqual(ETokenType.Item, tokens[0].Type);

            Assert.AreEqual(ETokenType.Characters, tokens[1].Type);
            Assert.AreEqual(0, tokens[1].Position);
            Assert.AreEqual("Hello", tokens[1].Data);

            Assert.AreEqual(6, tokens[2].Position);
            Assert.AreEqual(ETokenType.Item, tokens[2].Type);

            Assert.AreEqual(ETokenType.Characters, tokens[3].Type);
            Assert.AreEqual(6, tokens[3].Position);
            Assert.AreEqual("World", tokens[3].Data);
        }

        private IEnumerable<Token> ReadTokens(string line)
        {
            return new LineTokenReader(new MemoryStream(Encoding.Default.GetBytes(line)), Encoding.Default, string.Empty, string.Empty).ReadAll();
        }
    }
}
