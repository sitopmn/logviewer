using logviewer.query.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace logviewer.test
{
    [TestClass]
    public class PatternTest
    {
        [TestMethod]
        public void MatchSuccessful()
        {
            var filter = new Pattern("{timestamp} [*Speed/Value = {speed}");
            Assert.IsTrue(filter.IsMatch("2017-10-22 21:55:44,100 [4508] INFO [DataHandler]: WriteVariable Speed/Value = 0.02002258"));
        }

        [TestMethod]
        public void CaptureNamesCorrect()
        {
            var filter = new Pattern("{timestamp} [*Speed/Value = {speed}");
            Assert.AreEqual("timestamp", filter.Captures[0]);
            Assert.AreEqual("speed", filter.Captures[1]);
        }

        [TestMethod]
        public void CaptureValuesSuccessful()
        {
            var filter = new Pattern("{timestamp} [*Speed/Value = {speed}");
            var match = filter.Match("2017-10-22 21:55:44,100 [4508] INFO [DataHandler]: WriteVariable Speed/Value = 0.02002258");
            Assert.IsNotNull(match);
            Assert.AreEqual("2017-10-22 21:55:44,100", match.Captures[0].Value);
            Assert.AreEqual("0.02002258", match.Captures[1].Value);
        }

        [TestMethod]
        public void MatchFails()
        {
            var filter = new Pattern("{timestamp} [*Speed/Value = {speed}");
            Assert.IsFalse(filter.IsMatch("2017-10-22 21:54:34,515 [3664] INFO [DataHandler]: WriteVariable Position/Value = 1"));
        }

        [TestMethod]
        public void ConstantMatchSuccessful()
        {
            var filter = new Pattern("StateMachine");
            Assert.IsTrue(filter.IsMatch("2017-10-22 21:54:37,607 [3664] INFO [StateMachine]: State: STOPPED -> DISABLED"));
        }

        [TestMethod]
        public void ConstantMatchFails()
        {
            var filter = new Pattern("abcdef");
            Assert.IsFalse(filter.IsMatch("dgsghlsfkjgslkdfg"));
        }

        [TestMethod]
        public void ConstantMatchAtStartSuccessful()
        {
            var filter = new Pattern("abcdef");
            Assert.IsTrue(filter.IsMatch("abcdefghi"));
        }

        [TestMethod]
        public void ConstantMatchAtEndSuccessful()
        {
            var filter = new Pattern("abcdef");
            Assert.IsTrue(filter.IsMatch("0123abcdef"));
        }

        [TestMethod]
        public void EscapeSequenceSuccessful()
        {
            var filter = new Pattern(@"abc\*def\{ghi}");
            Assert.IsTrue(filter.IsMatch("abc*def{ghi}"));
        }

        [TestMethod]
        public void WildcardAtEndSuccessful()
        {
            var filter = new Pattern("abc*");
            Assert.IsTrue(filter.IsMatch("abcdef"));
        }

        [TestMethod]
        public void WildcardAtStartSuccessful()
        {
            var filter = new Pattern("*def");
            Assert.IsTrue(filter.IsMatch("abcdef"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WildcardAfterCaptureFails()
        {
            var filter = new Pattern("abv{test}*def");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CaptureAfterWildcardFails()
        {
            var filter = new Pattern("abc*{test}def");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void OpenCaptureFails()
        {
            var filter = new Pattern("abc{testdef");
        }

        [TestMethod]
        public void QuestionMarkAtStartSuccessful()
        {
            var filter = new Pattern("?bcdef");
            Assert.IsTrue(filter.IsMatch("abcdef"));
        }

        [TestMethod]
        public void QuestionMarkAtEndSuccessful()
        {
            var filter = new Pattern("abcde?");
            Assert.IsTrue(filter.IsMatch("abcdef"));
        }

        [TestMethod]
        public void QuestionMarkBeforeWildcardSuccessful()
        {
            var filter = new Pattern("ab?*");
            Assert.IsTrue(filter.IsMatch("abcdef"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void QuestionMarkAfterWildcardFails()
        {
            var filter = new Pattern("ab*?f");
        }

        [TestMethod]
        public void BenchmarkCapture()
        {
            var pattern = new Pattern("{timestamp} [*Speed/Value = {speed}");
            var regex = new Regex(@"(?<timestamp>.+) \[.+Speed/Value = (?<speed>.+)$");

            Benchmark(pattern, regex, true, "2017-10-22 21:55:44,100 [4508] INFO [DataHandler]: WriteVariable Speed/Value = 0.02002258");
            Benchmark(pattern, regex, false, "2017-10-22 21:54:34,515 [3664] INFO [DataHandler]: WriteVariable Position/Value = 1");
        }

        [TestMethod]
        public void BenchmarkMatch()
        {
            var pattern = new Pattern("Speed/Value");
            var regex = new Regex(@"Speed/Value");

            Benchmark(pattern, regex, true, "2017-10-22 21:55:44,100 [4508] INFO [DataHandler]: WriteVariable Speed/Value = 0.02002258");
            Benchmark(pattern, regex, false, "2017-10-22 21:54:34,515 [3664] INFO [DataHandler]: WriteVariable Position/Value = 1");
        }

        private void Benchmark(Pattern pattern, Regex regex, bool success, string data)
        {
            var sw = new Stopwatch();
            var result = success ? "successful" : "failed";
            var count = 10000;

            sw.Start();
            foreach (var l in Enumerable.Range(0, count).Select(i => data))
            {
                Assert.AreEqual(success, pattern.IsMatch(l));
            }
            sw.Stop();

            Debug.WriteLine($"{count} {result} pattern matches completed in {sw.ElapsedMilliseconds}ms");
            sw.Reset();

            sw.Start();
            foreach (var l in Enumerable.Range(0, count).Select(i => data))
            {
                Assert.AreEqual(success, regex.IsMatch(l));
            }
            sw.Stop();
            Debug.WriteLine($"{count} {result} regex matches completed in {sw.ElapsedMilliseconds}ms");
            sw.Reset();
        }
    }
}
