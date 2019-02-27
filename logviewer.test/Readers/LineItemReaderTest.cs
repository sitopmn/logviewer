using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using logviewer.query;
using logviewer.query.Readers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace logviewer.test.Readers
{
    [TestClass]
    public class LineItemReaderTest
    {
        [TestMethod]
        public void ReadItemReturnsCorrectLinesWithCrLfEnding()
        {
            var lines = Enumerable.Range(0, 5000).Select(i => $"Line {i}").ToList();
            var reader = CreateReader(lines, "\r\n");
            Assert.IsTrue(lines.All(l => reader.Read().Message == l));
        }

        [TestMethod]
        public void ReadItemReturnsCorrectLinesWithLfCrEnding()
        {
            var lines = Enumerable.Range(0, 5000).Select(i => $"Line {i}").ToList();
            var reader = CreateReader(lines, "\n\r");
            Assert.IsTrue(lines.All(l => reader.Read().Message == l));
        }

        [TestMethod]
        public void ReadItemReturnsCorrectLinesWithLfEnding()
        {
            var lines = Enumerable.Range(0, 5000).Select(i => $"Line {i}").ToList();
            var reader = CreateReader(lines, "\n");
            Assert.IsTrue(lines.All(l => reader.Read().Message == l));
        }

        [TestMethod]
        public void ReadItemReturnsCorrectLinesWithMixedEndings()
        {
            var lines = Enumerable.Range(0, 10).Select(i => $"Line {i}\r\n").Concat(Enumerable.Range(10, 10).Select(i => $"Line {i}\n\r")).Concat(Enumerable.Range(20, 10).Select(i => $"Line {i}\n")).ToList();
            var reader = CreateReader(lines, "");
            Assert.IsTrue(lines.All(l => reader.Read().Message == l.TrimEnd()));
        }

        [TestMethod]
        public void ReadItemReturnsCorrectLineNumbers()
        {
            var lines = Enumerable.Range(0, 1000).Select(i => $"Line {i}").Concat(Enumerable.Range(0, 1000).Select(i => $"Line {i}\n\r")).Concat(Enumerable.Range(0, 1000).Select(i => $"Line {i}\n")).ToList();
            var reader = CreateReader(lines);
            Assert.IsTrue(lines.Select((l, i) => i).All(i => reader.Read().Line == i));
        }

        [TestMethod]
        public void ReadItemReturnsCorrectLinePositions()
        {
            var lines = Enumerable.Range(0, 5000).Select(i => $"Line {i}").ToList();
            var reader = CreateReader(lines, "\r\n");
            var position = 0;
            foreach (var line in lines)
            {
                var item = reader.Read();
                Assert.AreEqual(position, item.Position);
                position += line.Length + 2;
            }
        }

        [TestMethod]
        public void ReadItemReturnsCorrectLinesAfterSeek()
        {
            var lines = Enumerable.Range(0, 5000).Select(i => $"Line {i}").ToList();
            var offset = lines.Take(1000).Sum(l => l.Length + 2);
            var reader = CreateReader(lines, "\r\n");
            reader.Read();
            reader.Seek(offset, 1000, SeekOrigin.Begin);
            Assert.IsTrue(lines.Skip(1000).All(l => reader.Read().Message == l));
        }

        [TestMethod]
        public void ReadItemReturnsCorrectLineNumbersAfterSeek()
        {
            var lines = Enumerable.Range(0, 5000).Select(i => $"Line {i}").ToList();
            var offset = lines.Take(1000).Sum(l => l.Length + 2);
            var reader = CreateReader(lines, "\r\n");
            reader.Read();
            reader.Seek(offset, 1000, SeekOrigin.Begin);
            Assert.IsTrue(lines.Skip(1000).Select((l, i) => i + 1000).All(i => reader.Read().Line == i));
        }

        [TestMethod]
        public void ReadItemReturnsCorrectLinePositionsAfterSeek()
        {
            var lines = Enumerable.Range(0, 5000).Select(i => $"Line {i}").ToList();
            var reader = CreateReader(lines, "\r\n");
            var position = lines.Take(1000).Sum(l => l.Length + 2);
            reader.Read();
            reader.Seek(position, 1000, SeekOrigin.Begin);
            foreach (var line in lines.Skip(1000))
            {
                var item = reader.Read();
                Assert.AreEqual(position, item.Position);
                position += line.Length + 2;
            }
        }

        [TestMethod]
        public void ReadItemReturnsCorrectFile()
        {
            var reader = CreateReader(new[] { "Test" });
            Assert.AreEqual("file", reader.Read().File);
        }

        [TestMethod]
        public void ReadItemReturnsCorrectMember()
        {
            var reader = CreateReader(new[] { "Test" });
            Assert.AreEqual("member", reader.Read().Member);
        }

        [TestMethod]
        public void ReadItemReturnsSingleLineCorrectly()
        {
            var reader = CreateReader(new[] { "Test" });
            Assert.AreEqual("Test", reader.Read().Message);
            Assert.AreEqual(null, reader.Read());
        }

        [TestMethod]
        public void ReadItemReturnsEmptyInputCorrectly()
        {
            var reader = CreateReader(Enumerable.Empty<string>());
            Assert.AreEqual(null, reader.Read());
        }

        [TestMethod]
        public void ReadItemReturnsEmptyLineCorrectly()
        {
            var reader = CreateReader(new[] { "", "" });
            Assert.AreEqual("", reader.Read().Message);
            Assert.AreEqual(null, reader.Read());
        }

        [TestMethod]
        public void ReadItemReturnsDoubleEmptyLineCorrectly()
        {
            var reader = CreateReader(new[] { "", "", "" });
            Assert.AreEqual("", reader.Read().Message);
            Assert.AreEqual("", reader.Read().Message);
            Assert.AreEqual(null, reader.Read());
        }

        [TestMethod]
        public void ReadItemReturnsInterleavedEmptyLineCorrectly()
        {
            var reader = CreateReader(new[] { "Foo", "", "Bar" });
            Assert.AreEqual("Foo", reader.Read().Message);
            Assert.AreEqual("", reader.Read().Message);
            Assert.AreEqual("Bar", reader.Read().Message);
            Assert.AreEqual(null, reader.Read());
        }

        [TestMethod]
        public void Benchmark()
        {
            var rnd = new Random(123);
            var lines = Enumerable.Range(0, 10000).Select(i => new string(Enumerable.Repeat('X', rnd.Next() % 2048).ToArray())).ToList();
            var data = string.Join("\n", lines);

            var reader1 = CreateReader(lines, "\n");
            var sw = Stopwatch.StartNew();
            while (!reader1.EndOfStream)
            {
                var item = reader1.Read();
            }
            Trace.WriteLine($"LineReader completed reading {lines.Count} in {sw.ElapsedMilliseconds} ms");
            
            var reader3 = new StreamReader(new MemoryStream(Encoding.Default.GetBytes(data)), Encoding.Default);
            sw.Restart();
            while (!reader3.EndOfStream)
            {
                var line = reader3.ReadLine();
            }
            Trace.WriteLine($"StreamReader completed reading {lines.Count} in {sw.ElapsedMilliseconds} ms");
        }

        private LineItemReader CreateReader(IEnumerable<string> lines, string newline = "\r\n")
        {
            return new LineItemReader(new MemoryStream(Encoding.Default.GetBytes(string.Join(newline, lines))), "file", "member");
        }
    }
}
