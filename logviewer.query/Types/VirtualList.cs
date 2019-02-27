using logviewer.core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query
{
    /// <summary>
    /// Abstract implementation for a data virtualized list
    /// </summary>
    /// <typeparam name="T">Type of list items</typeparam>
    internal abstract class VirtualList<T> : NotificationObject, IReadOnlyList<T>, INotifyCollectionChanged
    {
        /// <summary>
        /// Size of a buffer block
        /// </summary>
        private const int BufferSize = 1024;
        
        /// <summary>
        /// Dictionary containing buffer blocks
        /// </summary>
        private Dictionary<int, Buffer> _buffers = new Dictionary<int, Buffer>();

        /// <summary>
        /// Indexer for accessing items by index
        /// </summary>
        /// <param name="index">Index of the item to access</param>
        /// <returns>Item at the given index</returns>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                var buffer = FindBuffer(index);
                return buffer.Data[index - buffer.Index];
            }

            protected set
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                var buffer = FindBuffer(index, false);
                if (buffer != null)
                {
                    buffer.Data[index - buffer.Index] = value;
                }

                RaisePropertyChanged("Item[]");
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, null, index));
            }
        }

        /// <summary>
        /// Gets the number of items in the list
        /// </summary>
        public int Count
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the maximum number of buffer blocks
        /// </summary>
        protected int BufferLimit
        {
            get;
            set;
        } = 1000;

        /// <summary>
        /// Notifies of changes to the collection
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Gets the index of the given item
        /// </summary>
        /// <param name="item">Item to find</param>
        /// <returns>Index of the item or -1 if it is not contained in the collection</returns>
        public virtual int IndexOf(T item)
        {
            foreach (var buffer in _buffers.Values)
            {
                var index = Array.IndexOf(buffer.Data, item);
                if (index >= 0)
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets an enumerator returning the collection items
        /// </summary>
        /// <returns>Enumerator returnin the collection items</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        /// <summary>
        /// Gets an enumerator returning the collection items
        /// </summary>
        /// <returns>Enumerator returnin the collection items</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        /// <summary>
        /// Resets the collection to the given number of items
        /// </summary>
        /// <param name="count">Number of items in the collection</param>
        protected void Reset(int count)
        {
            // restore the buffer limit
            if (BufferLimit == int.MaxValue)
            {
                BufferLimit = 1000;
            }

            Count = count;
            _buffers.Clear();
            RaisePropertyChanged(nameof(Count));
            RaisePropertyChanged("Item[]");
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Resets the collection and buffers the given items
        /// </summary>
        /// <remarks>Items are stored in buffer blocks and maximum buffer count is set to accomodate all items</remarks>
        /// <param name="items">Items to buffer for the collection</param>
        protected void Reset(IEnumerable<T> items)
        {
            // store all items in memory
            BufferLimit = int.MaxValue;

            // clear existing buffers
            _buffers.Clear();

            // add items to the buffers
            Count = 0;
            foreach (var item in items)
            {
                var buffer = FindBuffer(Count);
                buffer.Data[Count - buffer.Index] = item;
                Count += 1;
            }

            // raise change events
            RaisePropertyChanged(nameof(Count));
            RaisePropertyChanged("Item[]");
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Adds a range of items to the collection
        /// </summary>
        /// <param name="items">Enumerable returning the items to add</param>
        protected void Add(IEnumerable<T> items)
        {
            var startingIndex = Count;
            var index = startingIndex;
            var list = items.ToList();
            foreach (var item in list)
            {
                var buffer = FindBuffer(index, false);
                if (buffer != null)
                {
                    buffer.Data[index++ - buffer.Index] = item;
                }
            }

            Count += list.Count;
            RaisePropertyChanged(nameof(Count));
            RaisePropertyChanged("Item[]");
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list, startingIndex));
        }

        /// <summary>
        /// Adds an item to the collection
        /// </summary>
        /// <remarks>If the index is contained in a buffer block, the block is updated, otherwise just the change event is fired</remarks>
        /// <param name="item">Item to add</param>
        protected void Add(T item)
        {
            var buffer = FindBuffer(Count, false);
            if (buffer != null)
            {
                buffer.Data[Count - buffer.Index] = item;
            }

            Count += 1;
            RaisePropertyChanged(nameof(Count));
            RaisePropertyChanged("Item[]");
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, Count - 1));
        }

        /// <summary>
        /// Loads a buffer block with data
        /// </summary>
        /// <remarks>The index may be greater than the number of items in the collection</remarks>
        /// <param name="index">Index of the first item in the buffer block</param>
        /// <param name="data">Array to fill with the items of the buffer block</param>
        protected abstract void Load(int index, T[] data);

        /// <summary>
        /// Finds a buffer block for the given index. If no block is found a new block is loaded optionally.
        /// </summary>
        /// <param name="index">Index the buffer block should contain</param>
        /// <param name="load">True if a block is loaded if no existent block is found</param>
        /// <returns>A buffer block containing the given index or null</returns>
        private Buffer FindBuffer(int index, bool load = true)
        {
            // get the index of the block
            var bufferIndex = index - index % BufferSize;

            // find the block or load a new one
            Buffer buffer = null;
            if (!_buffers.TryGetValue(bufferIndex, out buffer) && load)
            {
                // if the block limit is reached, release the oldest blocks
                if (_buffers.Count > BufferLimit)
                {
                    foreach (var remove in _buffers.Values.OrderBy(b => b.Timestamp).Take(_buffers.Count - BufferLimit).Where(b => b.Index != bufferIndex).ToList())
                    {
                        _buffers.Remove(remove.Index);
                    }
                }

                // allocate and fill a new buffer block
                var data = new T[BufferSize];
                Load(bufferIndex, data);
                buffer = new Buffer(bufferIndex, data);
                _buffers[bufferIndex] = buffer;
            }

            // update the timestamp of the block
            if (buffer != null)
            {
                buffer.Timestamp = DateTime.Now;
            }
            
            return buffer;
        }

        /// <summary>
        /// Internal representation of a buffer block
        /// </summary>
        private class Buffer
        {
            /// <summary>
            /// Index of the block
            /// </summary>
            public readonly int Index;

            /// <summary>
            /// Array of items within the block
            /// </summary>
            public readonly T[] Data;

            /// <summary>
            /// Last access timestamp of the block
            /// </summary>
            public DateTime Timestamp;

            /// <summary>
            /// Initializes a new instance of the <see cref="Buffer"/> class.
            /// </summary>
            /// <param name="index">Index of the block</param>
            /// <param name="data">Array of items within the block</param>
            public Buffer(int index, T[] data)
            {
                Index = index;
                Data = data;
                Timestamp = DateTime.Now;
            }
        }
    }
}
