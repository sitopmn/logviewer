using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using logviewer.query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace logviewer.test
{
    [TestClass]
    public class VirtualListTest
    {
        private readonly TestList _list = new TestList(14000000);

        private readonly List<LoadEventArgs> _loads = new List<LoadEventArgs>();

        private readonly List<NotifyCollectionChangedEventArgs> _changes = new List<NotifyCollectionChangedEventArgs>();

        [TestInitialize]
        public void Initialize()
        {
            _list.Loaded += (s, e) => _loads.Add(e);
            _list.CollectionChanged += (s, e) => _changes.Add(e);
            _loads.Clear();
            _changes.Clear();
        }

        [TestMethod]
        public void CountReturnsCorrectValue()
        {
            Assert.AreEqual(14000000, _list.Count);
        }

        [TestMethod]
        public void IndexerReturnsCorrectData()
        {
            Assert.AreEqual(500, _list[500]);
            Assert.AreEqual(1500, _list[1500]);
        }

        [TestMethod]
        public void IndexerLoadsData()
        {
            var a = _list[500];
            Assert.AreEqual(1, _loads.Count);
            Assert.AreEqual(0, _loads[0].Index);

            var b = _list[1500];
            Assert.AreEqual(2, _loads.Count);
            Assert.AreEqual(1024, _loads[1].Index);
        }

        [TestMethod]
        public void AddUpdatesCount()
        {
            _list.AddValue(99999999);
            Assert.AreEqual(14000001, _list.Count);
        }

        [TestMethod]
        public void IndexerReturnsAddedValue()
        {
            _list.AddValue(1111);
            Assert.AreEqual(1111, _list[14000000]);

            _list.AddValue(2222);
            Assert.AreEqual(2222, _list[14000001]);
        }

        [TestMethod]
        public void AddDoesNotLoadBuffer()
        {
            _list.AddValue(99999999);
            Assert.AreEqual(0, _loads.Count);
        }

        [TestMethod]
        public void AddFiresCollectionChanged()
        {
            _list.AddValue(99999999);
            Assert.AreEqual(1, _changes.Count);
            Assert.AreEqual(NotifyCollectionChangedAction.Add, _changes[0].Action);
            Assert.AreEqual(1, _changes[0].NewItems.Count);
            Assert.AreEqual(99999999, _changes[0].NewItems[0]);
            Assert.AreEqual(14000000, _changes[0].NewStartingIndex);
        }

        [TestMethod]
        public void UpdateDoesNotLoadBuffer()
        {
            _list.UpdateValue(7000, 1111);
            Assert.AreEqual(0, _loads.Count);
        }

        [TestMethod]
        public void UpdateReturnsCorrectValue()
        {
            var a = _list[7000];
            _list.UpdateValue(7000, 1111);
            Assert.AreEqual(1111, _list[7000]);
        }

        [TestMethod]
        public void UpdateFiresCollectionChanged()
        {
            var a = _list[7000];
            _list.UpdateValue(7000, 1111);
            Assert.AreEqual(1, _changes.Count);
            Assert.AreEqual(NotifyCollectionChangedAction.Replace, _changes[0].Action);
            Assert.AreEqual(7000, _changes[0].NewStartingIndex);
            Assert.AreEqual(1111, _changes[0].NewItems[0]);
        }

        [TestMethod]
        public void ClearResetsCount()
        {
            _list.ClearValues();
            Assert.AreEqual(0, _list.Count);
        }

        [TestMethod]
        public void ClearFiresCollectionChanged()
        {
            _list.ClearValues();
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, _changes[0].Action);
        }

        [TestMethod]
        public void BufferedResetReturnsCorrectCount()
        {
            _list.AddBufferedValues(Enumerable.Range(0, 10000));
            Assert.AreEqual(10000, _list.Count);
        }

        [TestMethod]
        public void BufferedResetReturnsCorrectValues()
        {
            _list.AddBufferedValues(Enumerable.Range(0, 10000));
            Assert.IsTrue(_list.Select((v, i) => v == i).All(v => v));
        }

        [TestMethod]
        public void BufferedResetFiresCollectionChanged()
        {
            _list.AddBufferedValues(Enumerable.Range(0, 10000));
            Assert.AreEqual(1, _changes.Count);
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, _changes[0].Action);
        }

        [TestMethod]
        public void EnumeratorReturnsCorrectValues()
        {
            Assert.IsTrue(_list.Select((v, i) => v == i).Take(5000).All(v => v));
        }

        [TestMethod]
        public void IndexOfReturnsCorrectIndex()
        {
            Assert.AreEqual(7654321, _list.IndexOf(7654321));
        }

        private class TestList : VirtualList<int>
        {
            private readonly Dictionary<int, int> _addedValues = new Dictionary<int, int>();

            public TestList(int count)
            {
                Reset(count);
            }

            public event EventHandler<LoadEventArgs> Loaded;

            public void AddBufferedValues(IEnumerable<int> values)
            {
                base.Reset(values);
            }

            public void AddValue(int value)
            {
                _addedValues[Count] = value;
                base.Add(value);
            }

            public void UpdateValue(int index, int value)
            {
                this[index] = value;
            }

            public void ClearValues()
            {
                base.Reset(0);
            }

            public override int IndexOf(int item)
            {
                return item;
            }

            protected override void Load(int index, int[] data)
            {
                Loaded?.Invoke(this, new LoadEventArgs(index));

                for (var i = 0; i < data.Length; i++)
                {
                    if (_addedValues.ContainsKey(index + i))
                    {
                        data[i] = _addedValues[index + i];
                    }
                    else if (index + i < Count)
                    {
                        data[i] = index + i;
                    }
                    else
                    {
                        data[i] = 0;
                    }
                }
            }
        }

        private class LoadEventArgs : EventArgs
        {
            public readonly int Index;

            public LoadEventArgs(int index)
            {
                Index = index;
            }
        }
    }
}
