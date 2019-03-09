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
    public class JsonReaderTest
    {
        [TestMethod]
        public void ObjectWithStringValueRead()
        {
            var objects = ReadObjects("{ \"key\":\"value\" }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(1, objects[0].Count);
            Assert.AreEqual("value", objects[0]["key"]);
        }

        [TestMethod]
        public void ObjectWithNumericValueRead()
        {
            var objects = ReadObjects("{ \"key\":4711 }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(1, objects[0].Count);
            Assert.AreEqual("4711", objects[0]["key"]);
        }

        [TestMethod]
        public void ObjectWithTrueValueRead()
        {
            var objects = ReadObjects("{ \"key\":true }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(1, objects[0].Count);
            Assert.AreEqual("true", objects[0]["key"]);
        }

        [TestMethod]
        public void ObjectWithFalseValueRead()
        {
            var objects = ReadObjects("{ \"key\":false }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(1, objects[0].Count);
            Assert.AreEqual("false", objects[0]["key"]);
        }

        [TestMethod]
        public void ObjectWithNullValueRead()
        {
            var objects = ReadObjects("{ \"key\":null }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(1, objects[0].Count);
            Assert.AreEqual("null", objects[0]["key"]);
        }
        
        [TestMethod]
        public void RecursiveObjectsRead()
        {
            var objects = ReadObjects("{ \"key\":{ \"test\" : 1234, \"foo\":\"bar\", \"bang\": { \"test\":4321 } }, \"abc\":\"def\" }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(4, objects[0].Count);
            Assert.AreEqual("1234", objects[0]["key.test"]);
            Assert.AreEqual("bar", objects[0]["key.foo"]);
            Assert.AreEqual("4321", objects[0]["key.bang.test"]);
            Assert.AreEqual("def", objects[0]["abc"]);
        }

        [TestMethod]
        public void ArrayInObjectSkipped()
        {
            var objects = ReadObjects("{ \"key\":1234, \"array\":[1, 2, 3, [ true, false], 4, 5 ] }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(2, objects[0].Count);
            Assert.AreEqual("1234", objects[0]["key"]);
            Assert.AreEqual("[...]", objects[0]["array"]);
        }

        [TestMethod]
        public void ObjectWithMultipleValuesRead()
        {
            var objects = ReadObjects("{ \"key1\":4711  , \"key2\":\"mygreatvalue\" }").ToArray();

            Assert.AreEqual(1, objects.Length);

            Assert.AreEqual(2, objects[0].Count);
            Assert.AreEqual("4711", objects[0]["key1"]);
            Assert.AreEqual("mygreatvalue", objects[0]["key2"]);
        }
        
        [TestMethod]
        public void MissingKeyValueSeparatorDetected()
        {
            var objects = ReadObjects("{ \"key\" false }").ToArray();

            Assert.AreEqual(0, objects.Length);
        }

        [TestMethod]
        public void GarbageBeforeKeyDetected()
        {
            var objects = ReadObjects("{ asdf \"key\" : false }").ToArray();

            Assert.AreEqual(0, objects.Length);
        }

        [TestMethod]
        public void GarbageAfterKeyDetected()
        {
            var objects = ReadObjects("{ \"key\" asdf : false }").ToArray();

            Assert.AreEqual(0, objects.Length);
        }

        [TestMethod]
        public void GarbageBeforeValueDetected()
        {
            var objects = ReadObjects("{ \"key\" : asdf false }").ToArray();

            Assert.AreEqual(0, objects.Length);
        }

        [TestMethod]
        public void GarbageAfterValueDetected()
        {
            var objects = ReadObjects("{ \"key\" : false asdf }").ToArray();

            Assert.AreEqual(0, objects.Length);
        }

        [TestMethod]
        public void ConcatedatedObjectsRead()
        {
            var objects = ReadObjects("{ \"key\" : 1 }{ \"key\" : 2 }").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(1, objects[0].Count);
            Assert.AreEqual("1", objects[0]["key"]);

            Assert.AreEqual(1, objects[1].Count);
            Assert.AreEqual("2", objects[1]["key"]);
        }

        [TestMethod]
        public void WhitespaceSeparatedObjectsRead()
        {
            var objects = ReadObjects("{ \"key\" : 1 }\t{ \"key\" : 2 }").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(1, objects[0].Count);
            Assert.AreEqual("1", objects[0]["key"]);

            Assert.AreEqual(1, objects[1].Count);
            Assert.AreEqual("2", objects[1]["key"]);
        }

        [TestMethod]
        public void NewlineSeparatedObjectsRead()
        {
            var objects = ReadObjects("{ \"key\" : 1 }\r\n{ \"key\" : 2 }\r\n").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(1, objects[0].Count);
            Assert.AreEqual("1", objects[0]["key"]);

            Assert.AreEqual(1, objects[1].Count);
            Assert.AreEqual("2", objects[1]["key"]);
        }

        [TestMethod]
        public void CommaSeparatedObjectsRead()
        {
            var objects = ReadObjects("{ \"key\" : 1 },{ \"key\" : 2 },").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(1, objects[0].Count);
            Assert.AreEqual("1", objects[0]["key"]);

            Assert.AreEqual(1, objects[1].Count);
            Assert.AreEqual("2", objects[1]["key"]);
        }

        [TestMethod]
        public void RecordSeparatorSeparatedObjectsRead()
        {
            var objects = ReadObjects("\x1E{ \"key\" : 1 }\n\x1E{ \"key\" : 2 }\n").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(1, objects[0].Count);
            Assert.AreEqual("1", objects[0]["key"]);

            Assert.AreEqual(1, objects[1].Count);
            Assert.AreEqual("2", objects[1]["key"]);
        }

        [TestMethod]
        public void CommentsBetweenDocumentsSkipped()
        {
            var objects = ReadObjects("/* 1 */\n{ \"key\" : 1 }\n/* 2 */\n{ \"key\" : 2 }\n").ToArray();

            Assert.AreEqual(2, objects.Length);

            Assert.AreEqual(1, objects[0].Count);
            Assert.AreEqual("1", objects[0]["key"]);

            Assert.AreEqual(1, objects[1].Count);
            Assert.AreEqual("2", objects[1]["key"]);
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
            using (var stringReader = new StringReader(data))
            using (var jsonReader = new Newtonsoft.Json.JsonTextReader(stringReader))
            {
                var serializer = new Newtonsoft.Json.JsonSerializer();
                jsonReader.SupportMultipleContent = true;
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == Newtonsoft.Json.JsonToken.StartObject)
                    {
                        serializer.Deserialize<Dictionary<string, dynamic>>(jsonReader);
                    }
                }
            }
            sw.Stop();
            Trace.WriteLine($"10000 JSON objects read using Newtonsoft.Json in {sw.ElapsedMilliseconds}ms");
        }

        private IEnumerable<Dictionary<string, string>> ReadObjects(string json)
        {
            return new TestReader(json).ReadAll();
        }
        
        private class TestReader : JsonReader<Dictionary<string, string>>
        {
            public TestReader(string json) 
                : base(new MemoryStream(Encoding.Default.GetBytes(json)), Encoding.Default, string.Empty, string.Empty)
            {
            }

            public override IEnumerable<Dictionary<string, string>> ReadAll()
            {
                while (true)
                {
                    var doc = ReadDocument();
                    if (doc.Position < 0)
                    {
                        yield break;
                    }
                    else
                    {
                        yield return doc.Fields.ToDictionary(f => f.Name, f => f.Value);
                    }
                }
            }

            public override Dictionary<string, string> Read()
            {
                var doc = ReadDocument();
                if (doc.Position < 0)
                {
                    return null;
                }
                else
                {
                    return doc.Fields.ToDictionary(f => f.Name, f => f.Value);
                }
            }
        }
    }
}
