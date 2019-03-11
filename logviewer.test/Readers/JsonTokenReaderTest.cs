using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using logviewer.query;
using logviewer.query.Readers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace logviewer.test.Readers
{
    [TestClass]
    public class JsonTokenReaderTest
    {
        [TestMethod]
        public void ObjectWithStringValueRead()
        {
            var objects = ReadTokens("{ \"key\":\"value\" }").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 9, "value");
        }

        [TestMethod]
        public void ObjectWithNumericValueRead()
        {
            var objects = ReadTokens("{ \"key\":4711 }").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 8, "4711");
        }

        [TestMethod]
        public void ObjectWithTrueValueRead()
        {
            var objects = ReadTokens("{ \"key\":true }").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 8, "true");
        }

        [TestMethod]
        public void ObjectWithFalseValueRead()
        {
            var objects = ReadTokens("{ \"key\":false }").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 8, "false");
        }

        [TestMethod]
        public void ObjectWithNullValueRead()
        {
            var objects = ReadTokens("{ \"key\":null }").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 8, "null");
        }
        
        [TestMethod]
        public void NestedObjectsRead()
        {
            var objects = ReadTokens("{ \"key\":{ \"test\" : 1234, \"foo\":\"bar\", \"bang\": { \"test\":4321 } }, \"abc\":\"def\" }").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key.test");
            CheckToken(objects, 2, ETokenType.Characters, 19, "1234");
            CheckToken(objects, 3, ETokenType.Field, 0, "key.foo");
            CheckToken(objects, 4, ETokenType.Characters, 32, "bar");
            CheckToken(objects, 5, ETokenType.Field, 0, "key.bang.test");
            CheckToken(objects, 6, ETokenType.Characters, 55, "4321");
            CheckToken(objects, 7, ETokenType.Field, 0, "abc");
            CheckToken(objects, 8, ETokenType.Characters, 72, "def");
        }

        [TestMethod]
        public void ArrayInObjectSkipped()
        {
            var objects = ReadTokens("{ \"key\":1234, \"array\":[1, 2, 3, [ true, false], 4, 5 ] }").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 8, "1234");
        }

        [TestMethod]
        public void ObjectWithMultipleValuesRead()
        {
            var objects = ReadTokens("{ \"key1\":4711  , \"key2\":\"mygreatvalue\" }").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key1");
            CheckToken(objects, 2, ETokenType.Characters, 9, "4711");
            CheckToken(objects, 3, ETokenType.Field, 0, "key2");
            CheckToken(objects, 4, ETokenType.Characters, 25, "mygreatvalue");
        }
        
        [TestMethod]
        public void ConcatedatedObjectsRead()
        {
            var objects = ReadTokens("{ \"key\" : 1 }{ \"key\" : 2 }").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 10, "1");
            CheckToken(objects, 3, ETokenType.Item, 13);
            CheckToken(objects, 4, ETokenType.Field, 0, "key");
            CheckToken(objects, 5, ETokenType.Characters, 23, "2");
        }

        [TestMethod]
        public void WhitespaceSeparatedObjectsRead()
        {
            var objects = ReadTokens("{ \"key\" : 1 }\t{ \"key\" : 2 }").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 10, "1");
            CheckToken(objects, 3, ETokenType.Item, 14);
            CheckToken(objects, 4, ETokenType.Field, 0, "key");
            CheckToken(objects, 5, ETokenType.Characters, 24, "2");
        }

        [TestMethod]
        public void NewlineSeparatedObjectsRead()
        {
            var objects = ReadTokens("{ \"key\" : 1 }\r\n{ \"key\" : 2 }\r\n").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 10, "1");
            CheckToken(objects, 3, ETokenType.Item, 15);
            CheckToken(objects, 4, ETokenType.Field, 0, "key");
            CheckToken(objects, 5, ETokenType.Characters, 25, "2");
        }

        [TestMethod]
        public void CommaSeparatedObjectsRead()
        {
            var objects = ReadTokens("{ \"key\" : 1 },{ \"key\" : 2 },").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 10, "1");
            CheckToken(objects, 3, ETokenType.Item, 14);
            CheckToken(objects, 4, ETokenType.Field, 0, "key");
            CheckToken(objects, 5, ETokenType.Characters, 24, "2");
        }

        [TestMethod]
        public void RecordSeparatorSeparatedObjectsRead()
        {
            var objects = ReadTokens("\x1E{ \"key\" : 1 }\n\x1E{ \"key\" : 2 }\n").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 1);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 11, "1");
            CheckToken(objects, 3, ETokenType.Item, 16);
            CheckToken(objects, 4, ETokenType.Field, 0, "key");
            CheckToken(objects, 5, ETokenType.Characters, 26, "2");
        }

        [TestMethod]
        public void CommentsBetweenDocumentsSkipped()
        {
            var objects = ReadTokens("/* 1 */\n{ \"key\" : 1 }\n/* 2 */\n{ \"key\" : 2 }\n").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 8);
            CheckToken(objects, 1, ETokenType.Field, 0, "key");
            CheckToken(objects, 2, ETokenType.Characters, 18, "1");
            CheckToken(objects, 3, ETokenType.Item, 30);
            CheckToken(objects, 4, ETokenType.Field, 0, "key");
            CheckToken(objects, 5, ETokenType.Characters, 40, "2");
        }

        [TestMethod]
        public void DocumentReadCorrectlyAfterInvalidDocument()
        {
            var objects = ReadTokens("{ \"key\" false } { \"key\" : 1234 }").ToArray();
            CheckToken(objects, 0, ETokenType.Item, 0);
            CheckToken(objects, 1, ETokenType.Item, 16);
            CheckToken(objects, 2, ETokenType.Field, 0, "key");
            CheckToken(objects, 3, ETokenType.Characters, 26, "1234");
        }

        [TestMethod]
        public void Benchmark()
        {
            var data = string.Join("\n", Enumerable.Range(0, 10000).Select(i => "{ \"key\" : \"value\" }"));

            // test the reader
            var sw = Stopwatch.StartNew();
            ReadTokens(data).ToList();
            sw.Stop();
            Trace.WriteLine($"10000 JSON objects read using reader in {sw.ElapsedMilliseconds}ms");

            // against Newtonsoft.Json
            sw.Restart();
            ReadNewtonsoftTokens(data).ToList();
            sw.Stop();
            Trace.WriteLine($"10000 JSON objects read using Newtonsoft.Json in {sw.ElapsedMilliseconds}ms");
        }

        private void CheckToken(Token[] tokens, int index, ETokenType type, long position, string data)
        {
            Assert.IsTrue(index < tokens.Length, $"Less than {index+1} tokens returned");
            Assert.AreEqual(type, tokens[index].Type, $"Incorrect token type returned, expected {type} and got {tokens[index].Type}");
            Assert.AreEqual(data, tokens[index].Data, $"Incorrect token data returned, expected '{data}' and got '{tokens[index].Data}'");
            Assert.AreEqual(position, tokens[index].Position, $"Incorrect token position returned, expected {position} and got {tokens[index].Position}");
        }

        private void CheckToken(Token[] tokens, int index, ETokenType type, long position)
        {
            Assert.IsTrue(index < tokens.Length, $"Less than {index + 1} tokens returned");
            Assert.AreEqual(type, tokens[index].Type, $"Incorrect token type returned, expected {type} and got {tokens[index].Type}");
            Assert.AreEqual(position, tokens[index].Position, $"Incorrect token position returned, expected {position} and got {tokens[index].Position}");
        }

        private IEnumerable<Token> ReadTokens(string json)
        {
            var buffer = new Token[256];
            using (var reader = new JsonTokenReader(new MemoryStream(Encoding.Default.GetBytes(json)), Encoding.Default, string.Empty, string.Empty))
            {
                var count = reader.Read(buffer, 0, buffer.Length);
                return Enumerable.Range(0, count).Select(i => buffer[i]);
            }
        }

        private IEnumerable<Token> ReadNewtonsoftTokens(string json)
        {
            var levels = 0;
            using (var stringReader = new StreamReader(new MemoryStream(Encoding.Default.GetBytes(json))))
            using (var jsonReader = new Newtonsoft.Json.JsonTextReader(stringReader))
            {
                jsonReader.SupportMultipleContent = true;
                while (jsonReader.Read())
                {
                    switch (jsonReader.TokenType)
                    {
                        case Newtonsoft.Json.JsonToken.StartObject:
                            if (levels == 0)
                            {
                                yield return new Token() { Type = ETokenType.Item };
                            }

                            levels += 1;
                            break;

                        case Newtonsoft.Json.JsonToken.EndObject:
                            levels -= 1;
                            break;

                        case Newtonsoft.Json.JsonToken.PropertyName:
                            yield return new Token() { Type = ETokenType.Field, Data = jsonReader.Value.ToString() };
                            break;

                        case Newtonsoft.Json.JsonToken.Integer:
                        case Newtonsoft.Json.JsonToken.Float:
                        case Newtonsoft.Json.JsonToken.Date:
                        case Newtonsoft.Json.JsonToken.Bytes:
                        case Newtonsoft.Json.JsonToken.String:
                            yield return new Token() { Type = ETokenType.Characters, Data = jsonReader.Value.ToString() };
                            break;
                    }
                }
            }
        }
    }
}
