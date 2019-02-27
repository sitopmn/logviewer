using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using logviewer.query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace logviewer.test
{
    [TestClass]
    public class LogReaderTest
    {
        private readonly Encoding _encoding = Encoding.Default;

        [TestMethod]
        public void ReadLines()
        {
            var reader = Create("Hallo Welt\nFoo Bar");
            Assert.AreEqual("Hallo Welt", reader.ReadLine());
            Assert.AreEqual("Foo Bar", reader.ReadLine());
            Assert.AreEqual(null, reader.ReadLine());

            reader = Create("Hallo Welt\rFoo Bar");
            Assert.AreEqual("Hallo Welt", reader.ReadLine());
            Assert.AreEqual("Foo Bar", reader.ReadLine());
            Assert.AreEqual(null, reader.ReadLine());

            reader = Create("Hallo Welt\r\nFoo Bar");
            Assert.AreEqual("Hallo Welt", reader.ReadLine());
            Assert.AreEqual("Foo Bar", reader.ReadLine());
            Assert.AreEqual(null, reader.ReadLine());

            reader = Create("Hallo Welt\n\rFoo Bar");
            Assert.AreEqual("Hallo Welt", reader.ReadLine());
            Assert.AreEqual("Foo Bar", reader.ReadLine());
            Assert.AreEqual(null, reader.ReadLine());

            reader = Create("Hallo Welt\r\nFoo Bar\r\n");
            Assert.AreEqual("Hallo Welt", reader.ReadLine());
            Assert.AreEqual("Foo Bar", reader.ReadLine());
            Assert.AreEqual(null, reader.ReadLine());

            reader = Create("");
            Assert.AreEqual(null, reader.ReadLine());

            reader = Create("\r\n");
            Assert.AreEqual("", reader.ReadLine());
            Assert.AreEqual(null, reader.ReadLine());

            reader = Create("\r\n\r\n");
            Assert.AreEqual("", reader.ReadLine());
            Assert.AreEqual("", reader.ReadLine());
            Assert.AreEqual(null, reader.ReadLine());

            reader = Create("Foo\r\n\r\nBar");
            Assert.AreEqual("Foo", reader.ReadLine());
            Assert.AreEqual("", reader.ReadLine());
            Assert.AreEqual("Bar", reader.ReadLine());
            Assert.AreEqual(null, reader.ReadLine());
        }

        [TestMethod]
        public void ReadLineLongerThanBuffer()
        {
            var line1 = "HalloWelt";
            var line2 = new string(Enumerable.Repeat('X', 1200).ToArray());
            var line3 = "FooBar";
            var reader = Create(line1 + "\n" + line2 + "\n" + line3 + "\n");
            Assert.AreEqual(line1, reader.ReadLine());
            Assert.AreEqual(line2, reader.ReadLine());
            Assert.AreEqual(line3, reader.ReadLine());
            Assert.AreEqual(null, reader.ReadLine());
        }

        [TestMethod]
        public void GivesCorrectPositions()
        {
            var bytes = 0;
            var line1 = "HalloWelt";
            var line2 = new string(Enumerable.Repeat('X', 1200).ToArray());
            var line3 = "FooBar";
            var reader = Create(line1 + "\n" + line2 + "\r\n" + line3 + "\n");
            Assert.AreEqual(line1, reader.ReadLine(out bytes));
            Assert.AreEqual(line1.Length + 1, bytes);
            Assert.AreEqual(line2, reader.ReadLine(out bytes));
            Assert.AreEqual(line2.Length + 2, bytes);
            Assert.AreEqual(line3, reader.ReadLine(out bytes));
            Assert.AreEqual(line3.Length + 1, bytes);
            Assert.AreEqual(null, reader.ReadLine(out bytes));
        }

        [TestMethod]
        public void Benchmark()
        {
            var rnd = new Random(123);
            var lines = Enumerable.Range(0, 10000).Select(i => new string(Enumerable.Repeat('X', rnd.Next() % 2048).ToArray())).ToList();
            var data = string.Join("\n", lines);

            var reader1 = new CountingReader(new MemoryStream(_encoding.GetBytes(data)));
            var bytes = 0;
            var i1 = 0;
            var sw = Stopwatch.StartNew();
            while (!reader1.EndOfStream)
            {
                var line = reader1.ReadLine(out bytes);
                Assert.AreEqual(lines[i1++], line);
            }
            Trace.WriteLine($"LogReader completed reading {lines.Count} in {sw.ElapsedMilliseconds} ms");

            var reader2 = new StreamReader(new MemoryStream(_encoding.GetBytes(data)), _encoding);
            var i2 = 0;
            sw.Restart();
            while (!reader2.EndOfStream)
            {
                var line = reader2.ReadLine();
                Assert.AreEqual(lines[i2++], line);
            }
            Trace.WriteLine($"StreamReader completed reading {lines.Count} in {sw.ElapsedMilliseconds} ms");
            Assert.AreEqual(i2, i1);
        }

        private CountingReader Create(string data)
        {
            return new CountingReader(new MemoryStream(_encoding.GetBytes(data)));
        }
    }
}
