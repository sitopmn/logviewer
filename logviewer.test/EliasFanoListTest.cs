using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using logviewer.query;
using logviewer.query.Index;
using logviewer.core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace logviewer.test
{
    [TestClass]
    public class EliasFanoListTest
    {
        private static readonly Random _random = new Random(500);

        private static readonly List<uint> _postingList = new List<uint>()
            {
                0,4316,4663,5002,5341,5685,6027,587162,587342,587522,
                587702,587882,588062,588242,588422,588602,588782,588962,589142,589322,
                589502,589682,589862,590042,590222,590402,590582,590762,590942,591122,
                591302,591482,591662,591842,592022,592202,592382,592562,592742,592922,
                593102,593282,593462,593642,593822,594002,594182,594362,594542,594722,
                594902,595082,595262,595442,595622,595802,595982,596162,596342,596522,
                596702,596882,597062,597242,597422,597602,597782,597962,598142,598322,
                598502,598682,598862,599042,599222,599402,599582,599762,599942,600122,
                600302,600482,600662,600842,601022,601202,601382,601562,601742,601923,
                602104,602285,602466,602647,602828,603009,603190,603371,603552,603733,
                603914,604095,604276,604457,604638,604819,963294,963474,963654,989143,
                989323,1015548,1015728,1064847,1065027,1065207,25070248,25070429,
            };

        // generates a sorted list of n integers with no duplicates within range
        private static List<uint> GeneratePostingList(int n, int range)
        {
            if ((n < 1) || (n > range) || (range < 1)) Console.WriteLine("n within 1...range and range>0!");

            List<uint> postingList = new List<uint>(n);

            // hashset fits in RAM && enough gaps (n*1.1<range)
            if ((n <= 10000000) && (n * 1.1 < range))
            {
                // fast for sparse lists, in dense lists many loops because difficult for random to hit remaining gaps, hashset required (RAM), sorting required 
                HashSet<uint> hs = new HashSet<uint>();
                while (hs.Count < n)
                {
                    uint docID = (uint)_random.Next(1, range);

                    // make sure docid are unique! 
                    // strictly positive delta, no zero allowed (we dont allow a zero for the docid because then the delta for the first docid in a posting list could be zero)
                    if (hs.Add(docID)) postingList.Add(docID);
                }
                postingList.Sort();
            }
            else
            {
                // slow for sparse lists as it loops through whole range, fast for dense lists, no hashset required, no sorting required
                for (uint i = 1; i <= range; i++)
                {
                    // derived from: if ( rnd.Next(range)<n) postingList.Add(i);
                    // adjusting probabilities so that exact number n is generated
                    if (_random.Next(range - (int)i) < (n - postingList.Count))
                    {
                        postingList.Add(i);
                    }
                }
            }

            return postingList;
        }

        [TestMethod]
        public void CompressionTest()
        {
            int indexedPages = 1000000000;

            // may be increased to 1,000,000,000 (>2 GB) if: >=16 GB RAM, 64 bit Windows, .NET version >= 4.5,  <gcAllowVeryLargeObjects> in config file, Project / Properties / Buld / Prefer 32-bit disabled!
            // http://stackoverflow.com/questions/25225249/maxsize-of-array-in-net-framework-4-5
            int maximumPostingListLength = 100000;

            for (int postingListLength = 10; postingListLength <= maximumPostingListLength; postingListLength *= 10)
            {
                // posting list creation
                Trace.WriteLine($"Create posting list with {postingListLength} items...");
                List<uint> postingList1 = GeneratePostingList(postingListLength, indexedPages);

                // compression
                Trace.Write("Compress posting list");
                var sw = Stopwatch.StartNew();
                var compressedBuffer1 = new EliasFanoList((uint)indexedPages, maximumPostingListLength, postingList1);
                Trace.WriteLine($" in {sw.ElapsedMilliseconds}ms...");

                // decompression
                Trace.Write("Decompress posting list");
                sw.Restart();
                var postingList10 = compressedBuffer1.ToList();
                Trace.WriteLine($" in {sw.ElapsedMilliseconds}ms...");

                // verification
                Trace.WriteLine("Verify posting list...");
                Assert.AreEqual(postingList1.Count, postingList10.Count);
                for (int i = 0; i < postingList1.Count; i++) Assert.AreEqual(postingList1[i], postingList10[i]);
            }
        }

        [TestMethod]
        public void CanCompressLists()
        {
            var lists = new List<List<uint>>()
            {
                new List<uint>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 },
                new List<uint>() { 87, 1009, 1117, 1221, 8065, 13647, 13954, 14057, 18688,  26675, 27183, 27966, 28069, 28426 },
                new List<uint>() { 12080479, 12082919, 12082936, 12093975, 49157819, 49164551 },
            };

            foreach (var list in lists)
            {
                var compressed = new EliasFanoList(list);
                Assert.IsTrue(list.SequenceEqual(compressed));
            }
        }

        [TestMethod]
        public void EnumeratorReturnsCorrectResults()
        {
            // compression
            var compressedBuffer1 = new EliasFanoList(_postingList.Last(), _postingList.Count, _postingList);
            
            // decompression
            var postingList10 = compressedBuffer1.ToList();

            // verification
            Assert.AreEqual(_postingList.Count, postingList10.Count);
            for (int i = 0; i < _postingList.Count; i++) Assert.AreEqual(_postingList[i], postingList10[i]);
        }

        [TestMethod]
        public void RandomAccessReturnsCorrectResults()
        {
            // compression
            var compressedBuffer1 = new EliasFanoList(_postingList.Last(), _postingList.Count, _postingList);

            // decompression & verification
            Assert.AreEqual(_postingList.Count, compressedBuffer1.Count);
            for (int i = 0; i < _postingList.Count; i++) Assert.AreEqual(_postingList[i], compressedBuffer1[i]);
        }

        [TestMethod]
        public void EnumeratorSkipsBeyondBoundary()
        {
            // compression
            var compressedBuffer1 = new EliasFanoList(_postingList.Last(), _postingList.Count, _postingList);

            // get enumerator and skip
            var enumerator = compressedBuffer1.GetEnumerator() as ISkippingEnumerator<uint>;
            Assert.AreEqual(true, enumerator.SkipToBoundary(595621));
            Assert.AreEqual(595622U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(595802U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(595982U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(596162U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(596342U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(596522U, enumerator.Current);
        }

        [TestMethod]
        public void EnumeratorSkipsToBoundary()
        {
            // compression
            var compressedBuffer1 = new EliasFanoList(_postingList.Last(), _postingList.Count, _postingList);

            // get enumerator and skip
            var enumerator = compressedBuffer1.GetEnumerator() as ISkippingEnumerator<uint>;
            Assert.AreEqual(true, enumerator.SkipToBoundary(963294));
            Assert.AreEqual(963294U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(963474U, enumerator.Current);
        }

        [TestMethod]
        public void EnumeratorSkipsToBoundaryTwice()
        {
            // compression
            var compressedBuffer1 = new EliasFanoList(_postingList.Last(), _postingList.Count, _postingList);

            // get enumerator and skip
            var enumerator = compressedBuffer1.GetEnumerator() as ISkippingEnumerator<uint>;
            Assert.AreEqual(true, enumerator.SkipToBoundary(963294));
            Assert.AreEqual(963294U, enumerator.Current);
            Assert.AreEqual(true, enumerator.SkipToBoundary(963294));
            Assert.AreEqual(963474U, enumerator.Current);
        }

        [TestMethod]
        public void EnumeratorSkipsBeyondLastItem()
        {
            // compression
            var compressedBuffer1 = new EliasFanoList(_postingList.Last(), _postingList.Count, _postingList);

            // get enumerator and skip
            var enumerator = compressedBuffer1.GetEnumerator() as ISkippingEnumerator<uint>;
            Assert.AreEqual(false, enumerator.SkipToBoundary(25070430));
        }

        [TestMethod]
        public void EnumeratorSkipsToLastItem()
        {
            // compression
            var compressedBuffer1 = new EliasFanoList(_postingList.Last(), _postingList.Count, _postingList);

            // get enumerator and skip
            var enumerator = compressedBuffer1.GetEnumerator() as ISkippingEnumerator<uint>;
            Assert.AreEqual(true, enumerator.SkipToBoundary(25070429));
            Assert.AreEqual(25070429U, enumerator.Current);
        }

        [TestMethod]
        public void EnumeratorSkipsBeyondLastBucket()
        {
            // compression
            var compressedBuffer1 = new EliasFanoList(_postingList.Last(), _postingList.Count, _postingList);

            // get enumerator and skip
            var enumerator = compressedBuffer1.GetEnumerator() as ISkippingEnumerator<uint>;
            Assert.AreEqual(false, enumerator.SkipToBoundary(50000000));
        }
         
        [TestMethod]
        public void EnumeratorSkipsByIndex()
        {
            // compression
            var compressedBuffer1 = new EliasFanoList(_postingList.Last(), _postingList.Count, _postingList);

            // get enumerator and skip
            var enumerator = compressedBuffer1.GetEnumerator() as ISkippingEnumerator<uint>;
            Assert.AreEqual(true, enumerator.SkipToIndex(10));
            Assert.AreEqual(587702U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(587882U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(588062U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(588242U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(588422U, enumerator.Current);
        }

        [TestMethod]
        public void EnumeratorSkipsToLargeIndex()
        {
            // compression
            var data = Enumerable.Range(0, 4096).Select(i => (uint)i).ToList();
            var compressedBuffer1 = new EliasFanoList(data);

            // get enumerator and skip
            var enumerator = compressedBuffer1.GetEnumerator() as ISkippingEnumerator<uint>;
            Assert.AreEqual(true, enumerator.SkipToIndex(3000));
            Assert.AreEqual(3000U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(3001U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(3002U, enumerator.Current);
            Assert.AreEqual(true, enumerator.SkipToIndex(3003));
            Assert.AreEqual(3003U, enumerator.Current);
        }

        [TestMethod]
        public void EnumeratorSkipsToFirstPointerIndex()
        {
            // compression
            var data = Enumerable.Range(0, 4096).Select(i => (uint)i).ToList();
            var compressedBuffer1 = new EliasFanoList(data);

            // get enumerator and skip
            var enumerator = compressedBuffer1.GetEnumerator() as ISkippingEnumerator<uint>;
            Assert.AreEqual(true, enumerator.SkipToIndex(1024));
            Assert.AreEqual(1024U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(1025U, enumerator.Current);
        }

        [TestMethod]
        public void EnumeratorSkipsToSecondPointerIndex()
        {
            // compression
            var data = Enumerable.Range(0, 4096).Select(i => (uint)i).ToList();
            var compressedBuffer1 = new EliasFanoList(data);

            // get enumerator and skip
            var enumerator = compressedBuffer1.GetEnumerator() as ISkippingEnumerator<uint>;
            Assert.AreEqual(true, enumerator.SkipToIndex(2048));
            Assert.AreEqual(2048U, enumerator.Current);
            Assert.AreEqual(true, enumerator.MoveNext());
            Assert.AreEqual(2049U, enumerator.Current);
        }

    }
}
