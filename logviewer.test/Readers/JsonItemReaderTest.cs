using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class JsonItemReaderTest
    {
        [TestMethod]
        public void ObjectWithStringValueRead()
        {
            var objects = ReadObjects("{ \"key\":\"value\" }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("value", objects[0].Fields["key"]);
        }

        [TestMethod]
        public void ObjectWithNumericValueRead()
        {
            var objects = ReadObjects("{ \"key\":4711 }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("4711", objects[0].Fields["key"]);
        }

        [TestMethod]
        public void ObjectWithTrueValueRead()
        {
            var objects = ReadObjects("{ \"key\":true }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("true", objects[0].Fields["key"]);
        }

        [TestMethod]
        public void ObjectWithFalseValueRead()
        {
            var objects = ReadObjects("{ \"key\":false }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("false", objects[0].Fields["key"]);
        }

        [TestMethod]
        public void ObjectWithNullValueRead()
        {
            var objects = ReadObjects("{ \"key\":null }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("null", objects[0].Fields["key"]);
        }

        [TestMethod]
        public void NestedObjectsRead()
        {
            var objects = ReadObjects("{ \"key\":{ \"test\" : 1234, \"foo\":\"bar\", \"bang\": { \"test\":4321 } }, \"abc\":\"def\" }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(5, objects[0].Fields.Count);
            Assert.AreEqual("1234", objects[0].Fields["key.test"]);
            Assert.AreEqual("bar", objects[0].Fields["key.foo"]);
            Assert.AreEqual("4321", objects[0].Fields["key.bang.test"]);
            Assert.AreEqual("def", objects[0].Fields["abc"]);
        }

        [TestMethod]
        public void ArrayInObjectSkipped()
        {
            var objects = ReadObjects("{ \"key\":1234, \"array\":[1, 2, 3, [ true, false], 4, 5 ] }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(3, objects[0].Fields.Count);
            Assert.AreEqual("1234", objects[0].Fields["key"]);
            Assert.AreEqual("[...]", objects[0].Fields["array"]);
        }

        [TestMethod]
        public void ObjectWithMultipleValuesRead()
        {
            var objects = ReadObjects("{ \"key1\":4711  , \"key2\":\"mygreatvalue\" }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(3, objects[0].Fields.Count);
            Assert.AreEqual("4711", objects[0].Fields["key1"]);
            Assert.AreEqual("mygreatvalue", objects[0].Fields["key2"]);
        }

        [TestMethod]
        public void DocumentReadCorrectlyAfterInvalidDocument()
        {
            var objects = ReadObjects("{ \"key\" false } { \"key\" : 1234 }").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(1, objects[0].Fields.Count);

            Assert.AreEqual(2, objects[1].Fields.Count);
            Assert.AreEqual("1234", objects[1].Fields["key"]);
        }

        [TestMethod]
        public void MissingKeyValueSeparatorDetected()
        {
            var objects = ReadObjects("{ \"key\" false }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(1, objects[0].Fields.Count);
        }

        [TestMethod]
        public void GarbageBeforeKeyDetected()
        {
            var objects = ReadObjects("{ asdf \"key\" : false }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(1, objects[0].Fields.Count);
        }

        [TestMethod]
        public void GarbageAfterKeyDetected()
        {
            var objects = ReadObjects("{ \"key\" asdf : false }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(1, objects[0].Fields.Count);
        }

        [TestMethod]
        public void GarbageBeforeValueDetected()
        {
            var objects = ReadObjects("{ \"key\" : asdf false }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(1, objects[0].Fields.Count);
        }

        [TestMethod]
        public void GarbageAfterValueDetected()
        {
            var objects = ReadObjects("{ \"key\" : false asdf }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("false", objects[0].Fields["key"]);
        }

        [TestMethod]
        public void ConcatedatedObjectsRead()
        {
            var objects = ReadObjects("{ \"key\" : 1 }{ \"key\" : 2 }").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("1", objects[0].Fields["key"]);

            Assert.AreEqual(2, objects[1].Fields.Count);
            Assert.AreEqual("2", objects[1].Fields["key"]);
        }

        [TestMethod]
        public void WhitespaceSeparatedObjectsRead()
        {
            var objects = ReadObjects("{ \"key\" : 1 }\t{ \"key\" : 2 }").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("1", objects[0].Fields["key"]);

            Assert.AreEqual(2, objects[1].Fields.Count);
            Assert.AreEqual("2", objects[1].Fields["key"]);
        }

        [TestMethod]
        public void NewlineSeparatedObjectsRead()
        {
            var objects = ReadObjects("{ \"key\" : 1 }\r\n{ \"key\" : 2 }\r\n").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("1", objects[0].Fields["key"]);

            Assert.AreEqual(2, objects[1].Fields.Count);
            Assert.AreEqual("2", objects[1].Fields["key"]);
        }

        [TestMethod]
        public void CommaSeparatedObjectsRead()
        {
            var objects = ReadObjects("{ \"key\" : 1 },{ \"key\" : 2 },").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("1", objects[0].Fields["key"]);

            Assert.AreEqual(2, objects[1].Fields.Count);
            Assert.AreEqual("2", objects[1].Fields["key"]);
        }

        [TestMethod]
        public void RecordSeparatorSeparatedObjectsRead()
        {
            var objects = ReadObjects("\x1E{ \"key\" : 1 }\n\x1E{ \"key\" : 2 }\n").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("1", objects[0].Fields["key"]);

            Assert.AreEqual(2, objects[1].Fields.Count);
            Assert.AreEqual("2", objects[1].Fields["key"]);
        }

        [TestMethod]
        public void CommentsBetweenDocumentsSkipped()
        {
            var objects = ReadObjects("/* 1 */\n{ \"key\" : 1 }\n/* 2 */\n{ \"key\" : 2 }\n").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(2, objects[0].Fields.Count);
            Assert.AreEqual("1", objects[0].Fields["key"]);

            Assert.AreEqual(2, objects[1].Fields.Count);
            Assert.AreEqual("2", objects[1].Fields["key"]);
        }

        [TestMethod]
        public void Benchmark()
        {
            var data = string.Join("\n", Enumerable.Range(0, 10000).Select(i => "{ \"key\" : \"value\" }"));

            // test the reader
            var sw = Stopwatch.StartNew();
            ReadObjects(data).ToList();
            sw.Stop();
            Trace.WriteLine($"10000 JSON objects read using reader in {sw.ElapsedMilliseconds}ms");

            // against Newtonsoft.Json
            sw.Restart();
            ReadNewtonsoftTokens(data).ToList();
            sw.Stop();
            Trace.WriteLine($"10000 JSON objects read using Newtonsoft.Json in {sw.ElapsedMilliseconds}ms");
        }

        private IEnumerable<ILogItem> ReadObjects(string json)
        {
            return new JsonItemReader(new MemoryStream(Encoding.Default.GetBytes(json)), Encoding.Default, string.Empty, string.Empty).ReadAll();
        }

        private IEnumerable<ILogItem> ReadNewtonsoftTokens(string json)
        {
            var level = 0;
            var propertyName = new Stack<string>();
            ILogItem item = null;
            using (var stringReader = new StreamReader(new MemoryStream(Encoding.Default.GetBytes(json))))
            using (var jsonReader = new Newtonsoft.Json.JsonTextReader(stringReader))
            {
                jsonReader.SupportMultipleContent = true;
                while (jsonReader.Read())
                {
                    switch (jsonReader.TokenType)
                    {
                        case Newtonsoft.Json.JsonToken.StartObject:
                            if (level == 0)
                            {
                                item = new LogItem(string.Empty, string.Empty, string.Empty, 0, 0);
                            }

                            level += 1;
                            break;

                        case Newtonsoft.Json.JsonToken.EndObject:
                            level -= 1;
                            if (level == 0)
                            {
                                yield return item;
                            }
                            break;

                        case Newtonsoft.Json.JsonToken.PropertyName:
                            propertyName.Push(jsonReader.Value.ToString());
                            break;

                        case Newtonsoft.Json.JsonToken.Integer:
                        case Newtonsoft.Json.JsonToken.Float:
                        case Newtonsoft.Json.JsonToken.Date:
                        case Newtonsoft.Json.JsonToken.Bytes:
                        case Newtonsoft.Json.JsonToken.String:
                            item.Fields.Add(string.Join(".", propertyName), jsonReader.Value.ToString());
                            propertyName.Pop();
                            break;
                    }
                }
            }
        }
    }
}
