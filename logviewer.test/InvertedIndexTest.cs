using logviewer.Interfaces;
using logviewer.query.Index;
using logviewer.query.Readers;
using logviewer.query.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace logviewer.test
{
    [TestClass]
    public class InvertedIndexTest
    {
        /// <summary>
        /// Test data for the first document
        /// </summary>
        private readonly string[] _data1 =
        {
            "199.72.81.55 - - [01/Jul/1995:00:00:01 -0400] \"GET /history/apollo/ HTTP/1.0\" 200 6245",
            "unicomp6.unicomp.net - - [01/Jul/1995:00:00:06 -0400] \"GET /shuttle/countdown/ HTTP/1.0\" 200 3985",
            "199.120.110.21 - - [01/Jul/1995:00:00:09 -0400] \"GET /shuttle/missions/sts-73/mission-sts-73.html HTTP/1.0\" 200 4085",
            "burger.letters.com - - [01/Jul/1995:00:00:11 -0400] \"GET /shuttle/countdown/liftoff.html HTTP/1.0\" 304 0",
            "199.120.110.21 - - [01/Jul/1995:00:00:11 -0400] \"GET /shuttle/missions/sts-73/sts-73-patch-small.gif HTTP/1.0\" 200 4179",
            "burger.letters.com - - [01/Jul/1995:00:00:12 -0400] \"GET /images/NASA-logosmall.gif HTTP/1.0\" 304 0",
            "burger.letters.com - - [01/Jul/1995:00:00:12 -0400] \"GET /shuttle/countdown/video/livevideo.gif HTTP/1.0\" 200 0",
            "205.212.115.106 - - [01/Jul/1995:00:00:12 -0400] \"GET /shuttle/countdown/countdown.html HTTP/1.0\" 200 3985",
            "d104.aa.net - - [01/Jul/1995:00:00:13 -0400] \"GET /shuttle/countdown/ HTTP/1.0\" 200 3985",
            "129.94.144.152 - - [01/Jul/1995:00:00:13 -0400] \"GET / HTTP/1.0\" 200 7074",
            "unicomp6.unicomp.net - - [01/Jul/1995:00:00:14 -0400] \"GET /shuttle/countdown/count.gif HTTP/1.0\" 200 40310",
            "unicomp6.unicomp.net - - [01/Jul/1995:00:00:14 -0400] \"GET /images/NASA-logosmall.gif HTTP/1.0\" 200 786",
            "unicomp6.unicomp.net - - [01/Jul/1995:00:00:14 -0400] \"GET /images/KSC-logosmall.gif HTTP/1.0\" 200 1204",
            "d104.aa.net - - [01/Jul/1995:00:00:15 -0400] \"GET /shuttle/countdown/count.gif HTTP/1.0\" 200 40310",
            "d104.aa.net - - [01/Jul/1995:00:00:15 -0400] \"GET /images/NASA-logosmall.gif HTTP/1.0\" 200 786",
            "d104.aa.net - - [01/Jul/1995:00:00:15 -0400] \"GET /images/KSC-logosmall.gif HTTP/1.0\" 200 1204",
            "129.94.144.152 - - [01/Jul/1995:00:00:17 -0400] \"GET /images/ksclogo-medium.gif HTTP/1.0\" 304 0",
            "199.120.110.21 - - [01/Jul/1995:00:00:17 -0400] \"GET /images/launch-logo.gif HTTP/1.0\" 200 1713",
            "ppptky391.asahi-net.or.jp - - [01/Jul/1995:00:00:18 -0400] \"GET /facts/about_ksc.html HTTP/1.0\" 200 3977",
            "net-1-141.eden.com - - [01/Jul/1995:00:00:19 -0400] \"GET /shuttle/missions/sts-71/images/KSC-95EC-0916.jpg HTTP/1.0\" 200 34029",
            "ppptky391.asahi-net.or.jp - - [01/Jul/1995:00:00:19 -0400] \"GET /images/launchpalms-small.gif HTTP/1.0\" 200 11473",
        };

        /// <summary>
        /// Test data for the second document
        /// </summary>
        private readonly string[] _data2 =
        {
            "This is an appended test line"
        };

        /// <summary>
        /// Timestamp for testing
        /// </summary>
        private readonly DateTime _timestamp = DateTime.Now;

        /// <summary>
        /// Index under test
        /// </summary>
        private InvertedIndex _index;

        [TestInitialize]
        public void Initialize()
        {
            _index = new InvertedIndex();

            var data1 = string.Join("\r\n", _data1);
            Add("test1", string.Empty, Encoding.Default.GetByteCount(data1), _timestamp, TokenizeString("test1", string.Empty, data1));
        }

        [TestMethod]
        public void CorrectFilesReturned()
        {
            var files = _index.Files;
            Assert.AreEqual(1, files.Count);
            Assert.AreEqual("test1", files.ElementAt(0).Item1);
            Assert.AreEqual(string.Empty, files.ElementAt(0).Item2);
            Assert.AreEqual(_timestamp, files.ElementAt(0).Item3);
        }

        [TestMethod]
        public void CorrectItemCountReturned()
        {
            Assert.AreEqual(21, _index.Count);
        }

        [TestMethod]
        public void FileAddedIndexReturnsNewToken()
        {
            var data = string.Join("\r\n", _data1);
            var dataLength = Encoding.Default.GetByteCount(data);

            var data2 = string.Join("\r\n", _data2);
            var data2Length = Encoding.Default.GetByteCount(data2);

            var timestamp = _timestamp.AddSeconds(10);
            Add("test0", string.Empty, dataLength + data2Length, timestamp, TokenizeString("test0", string.Empty, data2));
            var result = _index.Search(new[] { new Token() { Data = "appended" } }).ToArray();
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0, result[0].Position);
        }

        [TestMethod]
        public void FileAddedIndexReturnsNewFile()
        {
            var data = string.Join("\r\n", _data1);
            var dataLength = Encoding.Default.GetByteCount(data);

            var data2 = string.Join("\r\n", _data2);
            var data2Length = Encoding.Default.GetByteCount(data2);

            var timestamp = _timestamp.AddSeconds(10);
            Add("test0", string.Empty, dataLength + data2Length, timestamp, TokenizeString("test0", string.Empty, data2));

            var files = _index.Files;
            Assert.AreEqual(2, files.Count);
            Assert.AreEqual("test1", files.ElementAt(0).Item1);
            Assert.AreEqual(string.Empty, files.ElementAt(0).Item2);
            Assert.AreEqual(_timestamp, files.ElementAt(0).Item3);
            Assert.AreEqual("test0", files.ElementAt(1).Item1);
            Assert.AreEqual(string.Empty, files.ElementAt(1).Item2);
            Assert.AreEqual(timestamp, files.ElementAt(1).Item3);
        }

        [TestMethod]
        public void FileAddedIndexReturnsNewCount()
        {
            var data = string.Join("\r\n", _data1);
            var dataLength = Encoding.Default.GetByteCount(data);

            var data2 = string.Join("\r\n", _data2);
            var data2Length = Encoding.Default.GetByteCount(data2);

            var timestamp = _timestamp.AddSeconds(10);
            Add("test0", string.Empty, dataLength + data2Length, timestamp, TokenizeString("test0", string.Empty, data2));
            Assert.AreEqual(22, _index.Count);
        }

        [TestMethod]
        public void FileRemovedIndexReturnsOldFiles()
        {
            var data = string.Join("\r\n", _data1);
            var dataLength = Encoding.Default.GetByteCount(data);

            var data2 = string.Join("\r\n", _data2);
            var data2Length = Encoding.Default.GetByteCount(data2);

            var timestamp = _timestamp.AddSeconds(10);
            Add("test0", string.Empty, dataLength + data2Length, timestamp, TokenizeString("test0", string.Empty, data2));
            Remove("test0", string.Empty);

            var files = _index.Files;
            Assert.AreEqual(1, files.Count);
            Assert.AreEqual("test1", files.ElementAt(0).Item1);
            Assert.AreEqual(string.Empty, files.ElementAt(0).Item2);
            Assert.AreEqual(_timestamp, files.ElementAt(0).Item3);
        }

        [TestMethod]
        public void FileRemovedIndexReturnsOldCount()
        {
            var data = string.Join("\r\n", _data1);
            var dataLength = Encoding.Default.GetByteCount(data);

            var data2 = string.Join("\r\n", _data2);
            var data2Length = Encoding.Default.GetByteCount(data2);

            var timestamp = _timestamp.AddSeconds(10);
            Add("test0", string.Empty, dataLength + data2Length, timestamp, TokenizeString("test0", string.Empty, data2));
            Remove("test0", string.Empty);
            Assert.AreEqual(21, _index.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AddingTokenInIndexedFileSegmentThrowsException()
        {
            Add("test1", string.Empty, 9999, _timestamp.AddSeconds(10), new[] { new Token() { Type = ETokenType.Line, Position = 120 }, new Token() { Type = ETokenType.Characters, Data = "asdf", Position = 123 } });
        }

        [TestMethod]
        public void AddingTokenInNonIndexedFileSegmentSucceeds()
        {
            var data = string.Join("\r\n", _data1);
            var dataLength = Encoding.Default.GetByteCount(data);

            Add("test1", string.Empty, 9999, _timestamp.AddSeconds(10), new[] { new Token() { Type = ETokenType.Line, Position = dataLength + 9 }, new Token() { Type = ETokenType.Characters, Data = "asdf", Position = dataLength + 10 } });
        }

        [TestMethod]
        public void AddingTokenInNonIndexedFileSegmentPositionIsReturned()
        {
            var data = string.Join("\r\n", _data1);
            var dataLength = Encoding.Default.GetByteCount(data);

            Add("test1", string.Empty, 9999, _timestamp.AddSeconds(10), new[] { new Token() { Type = ETokenType.Line, Position = dataLength + 9 }, new Token() { Type = ETokenType.Characters, Data = "asdf", Position = dataLength + 10 } });

            var result = _index.Search(new[] { new Token() { Type = ETokenType.Characters, Data = "asdf" } }).ToArray();
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(dataLength + 9, result[0].Position);
        }

        [TestMethod]
        public void SingleTokenSearchReturnsCorrectResults()
        {
            var phrase = new[] { new Token() { Type = ETokenType.Characters, Data = "countdown" } };

            // search using skip search
            var sw = Stopwatch.StartNew();
            var results = _index.Search(phrase).Select(i => i.Line).ToArray();
            Trace.WriteLine($"SkipSearch() completed in {sw.ElapsedTicks} ticks");
            sw.Restart();

            // search using naive search
            var reference = _data1.Select((l, i) => new { line = l, index = i }).Where(i => phrase.All(t => i.line.Contains(t.Data))).Select(i => i.index + 1).ToArray();
            Assert.AreEqual(reference.Length, results.Length);
            for (var i = 0; i < reference.Length; i++) Assert.AreEqual(reference[i], results[i]);
        }

        [TestMethod]
        public void MultipleTokenSearchReturnsCorrectResults()
        {
            var phrase = new[] { new Token() { Type = ETokenType.Characters, Data = "images" }, new Token() { Type = ETokenType.Characters, Data = "NASA" } };

            // search using skip search
            var sw = Stopwatch.StartNew();
            var results = _index.Search(phrase).Select(i => i.Line).ToArray();
            Trace.WriteLine($"SkipSearch() completed in {sw.ElapsedTicks} ticks");
            sw.Restart();
            
            // search using naive search
            var reference = _data1.Select((l, i) => new { line = l, index = i }).Where(i => phrase.All(t => i.line.Contains(t.Data))).Select(i => i.index + 1).ToArray();
            Assert.AreEqual(reference.Length, results.Length);
            for (var i = 0; i < reference.Length; i++) Assert.AreEqual(reference[i], results[i]);
        }

        private static IEnumerable<Token> TokenizeString(string file, string member, string data)
        {
            return new LineTokenReader(data, file, member).ReadAll();
        }

        private void Add(string file, string member, long length, DateTime timestamp, IEnumerable<Token> tokens)
        {
            var state = _index.Initialize(0, file, member, length, timestamp, true);
            var array = tokens.ToArray();
            _index.Update(state, array, array.Length);
            _index.Complete(state);
        }

        private void Remove(string file, string member)
        {
            var state = _index.Initialize(0, file, member, 0, DateTime.Now, false);
            _index.Complete(state);
        }
    }
}
