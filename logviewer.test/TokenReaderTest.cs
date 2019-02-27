using logviewer.Interfaces;
using logviewer.query.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

namespace logviewer.test
{
    [TestClass]
    public class TokenReaderTest
    {
        [TestMethod]
        public void SingleTokenReturned()
        {
            var tokens = new TokenReader("FATAL", string.Empty, string.Empty, 0, Encoding.Default).ReadAll().ToArray();
            Assert.AreEqual(2, tokens.Length);
            Assert.AreEqual(ETokenType.Line, tokens[0].Type);
            Assert.AreEqual(ETokenType.Characters, tokens[1].Type);
            Assert.AreEqual("FATAL", tokens[1].Data);
        }

        [TestMethod]
        public void MultiTokenTerminatesCorrectly()
        {
            var tokens = new TokenReader("{time:time} [*ROLL_CHANGING -> ROLL_COMPLETED", string.Empty, string.Empty, 0, Encoding.Default).ReadAll().ToArray();
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
    }
}
