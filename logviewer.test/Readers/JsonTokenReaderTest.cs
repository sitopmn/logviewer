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

            Assert.AreEqual(3, objects.Length);

            Assert.AreEqual(ETokenType.Item, objects[0].Type);
            Assert.AreEqual(0, objects[0].Position);

            Assert.AreEqual(ETokenType.Characters, objects[1].Type);
            Assert.AreEqual("key", objects[1].Data);
            Assert.AreEqual(3, objects[1].Position);

            Assert.AreEqual(ETokenType.Characters, objects[2].Type);
            Assert.AreEqual("value", objects[2].Data);
            Assert.AreEqual(9, objects[2].Position);
        }

        [TestMethod]
        public void ObjectWithNumericValueRead()
        {
            var objects = ReadTokens("{ \"key\":4711 }").ToArray();

            Assert.AreEqual(3, objects.Length);
        }

        [TestMethod]
        public void ObjectWithTrueValueRead()
        {
            var objects = ReadTokens("{ \"key\":true }").ToArray();

            Assert.AreEqual(3, objects.Length);
        }

        [TestMethod]
        public void ObjectWithFalseValueRead()
        {
            var objects = ReadTokens("{ \"key\":false }").ToArray();

            Assert.AreEqual(3, objects.Length);
        }

        [TestMethod]
        public void ObjectWithNullValueRead()
        {
            var objects = ReadTokens("{ \"key\":null }").ToArray();

            Assert.AreEqual(3, objects.Length);
        }
        
        [TestMethod]
        public void NestedObjectsRead()
        {
            var objects = ReadTokens("{ \"key\":{ \"test\" : 1234, \"foo\":\"bar\", \"bang\": { \"test\":4321 } }, \"abc\":\"def\" }").ToArray();

            Assert.AreEqual(11, objects.Length);
        }

        [TestMethod]
        public void ArrayInObjectSkipped()
        {
            var objects = ReadTokens("{ \"key\":1234, \"array\":[1, 2, 3, [ true, false], 4, 5 ] }").ToArray();

            Assert.AreEqual(4, objects.Length);
        }

        [TestMethod]
        public void ObjectWithMultipleValuesRead()
        {
            var objects = ReadTokens("{ \"key1\":4711  , \"key2\":\"mygreatvalue\" }").ToArray();

            Assert.AreEqual(7, objects.Length);
        }
        
        [TestMethod]
        public void ConcatedatedObjectsRead()
        {
            var objects = ReadTokens("{ \"key\" : 1 }{ \"key\" : 2 }").ToArray();

            Assert.AreEqual(6, objects.Length);
        }

        [TestMethod]
        public void WhitespaceSeparatedObjectsRead()
        {
            var objects = ReadTokens("{ \"key\" : 1 }\t{ \"key\" : 2 }").ToArray();

            Assert.AreEqual(6, objects.Length);
        }

        [TestMethod]
        public void NewlineSeparatedObjectsRead()
        {
            var objects = ReadTokens("{ \"key\" : 1 }\r\n{ \"key\" : 2 }\r\n").ToArray();

            Assert.AreEqual(6, objects.Length);
        }

        [TestMethod]
        public void CommaSeparatedObjectsRead()
        {
            var objects = ReadTokens("{ \"key\" : 1 },{ \"key\" : 2 },").ToArray();

            Assert.AreEqual(6, objects.Length);
        }

        [TestMethod]
        public void RecordSeparatorSeparatedObjectsRead()
        {
            var objects = ReadTokens("\x1E{ \"key\" : 1 }\n\x1E{ \"key\" : 2 }\n").ToArray();

            Assert.AreEqual(6, objects.Length);
        }

        [TestMethod]
        public void CommentsBetweenDocumentsSkipped()
        {
            var objects = ReadTokens("/* 1 */\n{ \"key\" : 1 }\n/* 2 */\n{ \"key\" : 2 }\n").ToArray();

            Assert.AreEqual(6, objects.Length);
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

        private IEnumerable<Token> ReadTokens(string json)
        {
            return new JsonTokenReader(new MemoryStream(Encoding.Default.GetBytes(json)), Encoding.Default, string.Empty, string.Empty).ReadAll();
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
