#define ADD

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Index
{
    public interface ISkippingEnumerator<T> : IEnumerator<T>
    {
        /// <summary>
        /// Moves to the given index
        /// </summary>
        /// <param name="index">Index to move to</param>
        /// <returns>True if the move was successful</returns>
        bool SkipToIndex(int index);

        bool SkipToBoundary(uint lessThan);
    }

    /// <summary>
    /// Implementation of the integer list compression algorithm based on work by Elias & Fano et.al.
    /// </summary>
    /// <remarks>Based on http://vigna.di.unimi.it/ftp/papers/QuasiSuccinctIndices.pdf </remarks>
    [Serializable]
    internal class EliasFanoList : IReadOnlyList<uint>
    {
        /// <summary>
        /// Enumerator for access via the indexer
        /// </summary>
        private readonly EliasFanoEnumerator _indexEnumerator;

        /// <summary>
        /// Number of explicitely stored lower bits
        /// </summary>
        private int _lowerBitCount;

        /// <summary>
        /// Maximum item value to store
        /// </summary>
        private uint _universe;

        /// <summary>
        /// Number of negated unary reads which can be skipped with each entry in the skip pointer list
        /// </summary>
        private int _skipQuantum;

        /// <summary>
        /// Number of unary reads which can be skipped with each entry in the skip pointer list
        /// </summary>
        private int _indexQuantum;

        /// <summary>
        /// Number of items
        /// </summary>
        private int _count;

        /// <summary>
        /// Number of bits in <see cref="_upperBits"/>
        /// </summary>
        private uint _upperBitCount;

        /// <summary>
        /// Compressed data of the list
        /// </summary>
        private uint[] _lowerBits;

        /// <summary>
        /// Array with unary encoded upper bits
        /// </summary>
        private uint[] _upperBits;

        /// <summary>
        /// Array of upper bit indices after 'n' negated unary reads
        /// </summary>
        private uint[] _skipPointers;

        /// <summary>
        /// Number of pointers in <see cref="_skipPointers"/>
        /// </summary>
        private uint _skipPointerCount = 0;

        /// <summary>
        /// Array with pointers for skipping forward in the upper pointer array
        /// </summary>
        private uint[] _indexPointers;

        /// <summary>
        /// Number of pointers in <see cref="_indexPointers"/>
        /// </summary>
        private uint _indexPointerCount = 0;

        /// <summary>
        /// Last item which was added
        /// </summary>
        private uint _lastAddedItem = 0;

        /// <summary>
        /// Working counter for building the skipping pointers
        /// </summary>
        private uint _negatedUnaryReadCounter = 0;

        /// <summary>
        /// Working counter for building the indexing pointers
        /// </summary>
        private uint _unaryReadCounter = 0;

        /// <summary>
        /// Initializes a new instance of <see cref="EliasFanoList"/>
        /// </summary>
        /// <param name="universe">Maximum value to store</param>
        public EliasFanoList(uint universe)
            : this(universe, 1024)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="EliasFanoList"/>
        /// </summary>
        /// <param name="items">List of items to store</param>
        public EliasFanoList(IReadOnlyList<uint> items)
            : this(items.Count > 0 ? items[items.Count - 1] : 0, items.Count, items)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="EliasFanoList"/>
        /// </summary>
        /// <param name="universe">Maximum value to store</param>
        /// <param name="capacity">Maximum number of items to store</param>
        /// <param name="items">Enumerable returning the items to store</param>
        public EliasFanoList(uint universe, int capacity, IEnumerable<uint> items)
            : this(universe, capacity)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EliasFanoList"/>
        /// </summary>
        /// <param name="universe">Maximum value to store</param>
        /// <param name="capacity">Maximum number of items to store</param>
        public EliasFanoList(uint universe, int capacity)
        {
            // validate the universe
            if (universe == 0)
            {
                universe = 1;
            }

            if (capacity == 0)
            {
                capacity = 1;
            }

            // avoid negative log2
            if (universe <= capacity)
            {
                universe = (uint)capacity + 1;
            }

            _universe = universe;

            // calculate the lower bit count
            _lowerBitCount = (int)Math.Floor(Math.Log(universe / capacity, 2));
            if (_lowerBitCount > 32)
            {
                throw new ArgumentException("The given universe and capacity lead to a lower bit count > 32");
            }

            // allocate four more bytes in the data array to allow for working on 32 bit word size
            _count = 0;
            _upperBits = new uint[(int)Math.Ceiling((2 + Math.Ceiling(Math.Log(universe / (double)capacity, 2))) * capacity / 32) - (int)Math.Ceiling((capacity * (double)_lowerBitCount) / 32) + 1];
            _lowerBits = new uint[(int)Math.Ceiling((capacity * (double)_lowerBitCount) / 32)];

            // prepare the skip pointer list
            _skipQuantum = 32;
            _skipPointerCount = 1;
            _skipPointers = new uint[] { 0 };

            _indexQuantum = 1024;
            _indexPointerCount = 1;
            _indexPointers = new uint[] { 0 };

            // create the enumerator for the indexer
            _indexEnumerator = new EliasFanoEnumerator(this);
        }


        /// <summary>
        /// Gets the number of items
        /// </summary>
        public int Count => _count;
        
        /// <summary>
        /// Gets the item at the given index
        /// </summary>
        /// <remarks>To scan the list, do not use the indexer but rather use <see cref="GetEnumerator"/> as it is much more efficient</remarks>
        /// <param name="index">Index of the item to retrieve</param>
        /// <returns>Value at the given index</returns>
        public uint this[int index]
        {
            get
            {
                if (!_indexEnumerator.SkipToIndex(index))
                {
                    _indexEnumerator.Reset();
                    if (!_indexEnumerator.SkipToIndex(index))
                    {
                        throw new IndexOutOfRangeException();
                    }
                }

                return _indexEnumerator.Current;
            }
        }

        /// <summary>
        /// Adds an item to the list
        /// </summary>
        /// <param name="item">Item to add</param>
        public void Add(uint item)
        {
            if (item < _lastAddedItem)
            {
                throw new InvalidOperationException("Items must be added in monotonically increasing order");
            }

            if (item > _universe)
            {
                // extend the universe and recompress the list
                var temp = new EliasFanoList(_universe * 2, Count + 1, this.Concat(new[] { item }));
                _lowerBitCount = temp._lowerBitCount;
                _universe = temp._universe;
                _skipQuantum = temp._skipQuantum;
                _indexQuantum = temp._indexQuantum;
                _count = temp._count;
                _upperBitCount = temp._upperBitCount;
                _lowerBits = temp._lowerBits;
                _upperBits = temp._upperBits;
                _skipPointers = temp._skipPointers;
                _skipPointerCount = temp._skipPointerCount;
                _indexPointers = temp._indexPointers;
                _indexPointerCount = temp._indexPointerCount;
                _lastAddedItem = temp._lastAddedItem;
                _negatedUnaryReadCounter = temp._negatedUnaryReadCounter;
                _unaryReadCounter = temp._unaryReadCounter;
                _indexEnumerator.Reset();
            }
            else
            {
                // store the lower bits
                var lowerBitMask = (1 << _lowerBitCount) - 1;
                var lowerBits = item & lowerBitMask;
                var lowerBitCounter = 0;
                var lowerBitPointer = _lowerBitCount * _count;
                while (lowerBitCounter < _lowerBitCount)
                {
                    var byteOffset = (lowerBitPointer + lowerBitCounter) / 32;
                    var bitOffset = (lowerBitPointer + lowerBitCounter) % 32;

                    // resize the array if required
                    if (byteOffset >= _lowerBits.Length)
                    {
                        Array.Resize(ref _lowerBits, (int)Math.Ceiling(byteOffset * 1.5));
                    }

                    _lowerBits[byteOffset] |= (uint)(lowerBits << bitOffset);
                    lowerBitCounter += 32 - bitOffset;
                    lowerBits >>= 32 - bitOffset;
                }

                lowerBitPointer += _lowerBitCount;

                // get the upper bits to store and the difference to the previous upper bits
                var upperBits = item >> _lowerBitCount;
                var delta = upperBits - (_lastAddedItem >> _lowerBitCount);

                // build the negated unary skip list
                for (var i = 0U; i < delta; i++)
                {
                    _negatedUnaryReadCounter += 1;
                    if (_negatedUnaryReadCounter >= _skipQuantum)
                    {
                        // grow the skip pointer array if required
                        if (_skipPointerCount == _skipPointers.Length) Array.Resize(ref _skipPointers, (int)Math.Ceiling(_skipPointers.Length * 1.5));
                        _skipPointers[_skipPointerCount++] = _upperBitCount + i + 1;
                        _negatedUnaryReadCounter = 0;
                    }
                }

                // store the delta of the upper bits in unary encoding
                _upperBitCount += delta;
                if (_upperBitCount / 32 >= _upperBits.Length) Array.Resize(ref _upperBits, (int)Math.Ceiling(_upperBitCount * 1.5));
                _upperBits[_upperBitCount / 32] |= (uint)(1 << (int)(_upperBitCount % 32));
                _upperBitCount += 1;

                // build the skip list for direct indexing
                _unaryReadCounter += 1;
                if (_unaryReadCounter > _indexQuantum)
                {
                    if (_indexPointerCount == _indexPointers.Length) Array.Resize(ref _indexPointers, (int)Math.Ceiling(_indexPointers.Length * 1.5));
                    _indexPointers[_indexPointerCount++] = _upperBitCount;
                    _unaryReadCounter = 1;
                }

                _count += 1;
                _lastAddedItem = item;
            }
        }

        /// <summary>
        /// Returns an enumerator for the list
        /// </summary>
        /// <returns>Enumerator for the list</returns>
        public IEnumerator<uint> GetEnumerator()
        {
            return new EliasFanoEnumerator(this);
        }

        /// <summary>
        /// Returns an enumerator for the list
        /// </summary>
        /// <returns>Enumerator for the list</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Extracts the lower bits at the given pointer
        /// </summary>
        /// <param name="lowBitsPointer">Pointer to the bits</param>
        /// <returns>Value of the lower bits</returns>
        private uint GetLowBits(int lowBitsPointer)
        {
            uint lowBits = 0;
            var lowBitsCounter = 0;
            while (lowBitsCounter < _lowerBitCount)
            {
                var wordOffset = (lowBitsPointer + lowBitsCounter) / 32;
                var bitOffset = (lowBitsPointer + lowBitsCounter) % 32;
                lowBits |= (_lowerBits[wordOffset] >> bitOffset) << lowBitsCounter;
                lowBitsCounter += 32 - bitOffset;
            }

            return lowBits & (1U << _lowerBitCount) - 1;
        }

        /// <summary>
        /// Selects the position of the r'th set bit
        /// </summary>
        /// <remarks>See http://graphics.stanford.edu/~seander/bithacks.html#SelectPosFromMSBRank for more info</remarks>
        /// <param name="v">Word to select the bit in</param>
        /// <param name="r">Rank of the bit to select</param>
        /// <param name="pop">Number of bits set in <paramref name="value"/></param>
        /// <returns>Position of the selected bit</returns>
        private uint Select(uint value, uint n, out uint pop)
        {
            uint pop2 = (value & 0x55555555u) + ((value >> 1) & 0x55555555u);
            uint pop4 = (pop2 & 0x33333333u) + ((pop2 >> 2) & 0x33333333u);
            uint pop8 = (pop4 & 0x0f0f0f0fu) + ((pop4 >> 4) & 0x0f0f0f0fu);
            uint pop16 = (pop8 & 0x00ff00ffu) + ((pop8 >> 8) & 0x00ff00ffu);
            uint pop32 = (pop16 & 0x000000ffu) + ((pop16 >> 16) & 0x000000ffu);
            uint rank = 0;
            uint temp;

            // store the population count
            pop = pop32;

            // exit if a higher bit index is requested than available in the input
            if (n++ >= pop32)
                return 32;

            temp = pop16 & 0xffu;
            /* if (n > temp) { n -= temp; rank += 16; } */
            rank += ((temp - n) & 256) >> 4;
            n -= temp & ((temp - n) >> 8);

            temp = (pop8 >> (int)rank) & 0xffu;
            /* if (n > temp) { n -= temp; rank += 8; } */
            rank += ((temp - n) & 256) >> 5;
            n -= temp & ((temp - n) >> 8);

            temp = (pop4 >> (int)rank) & 0x0fu;
            /* if (n > temp) { n -= temp; rank += 4; } */
            rank += ((temp - n) & 256) >> 6;
            n -= temp & ((temp - n) >> 8);

            temp = (pop2 >> (int)rank) & 0x03u;
            /* if (n > temp) { n -= temp; rank += 2; } */
            rank += ((temp - n) & 256) >> 7;
            n -= temp & ((temp - n) >> 8);

            temp = (value >> (int)rank) & 0x01u;
            /* if (n > temp) rank += 1; */
            rank += ((temp - n) & 256) >> 8;

            return rank;
        }

        /// <summary>
        /// Enumerator implementation for scanning & skipping the compressed list
        /// </summary>
        private class EliasFanoEnumerator : ISkippingEnumerator<uint>
        {
            /// <summary>
            /// List which is scanned
            /// </summary>
            private readonly EliasFanoList _collection;
            
            /// <summary>
            /// Mask for the lower bits
            /// </summary>
            private readonly uint _lowerBitsMask;

            /// <summary>
            /// Index of the current item
            /// </summary>
            private int _index;

            /// <summary>
            /// Bit index of the lower bits for the next element
            /// </summary>
            private int _lowerBitsPointer;

            /// <summary>
            /// Buffer for data from the upper bits section
            /// </summary>
            private uint _upperBitsPointer;

            /// <summary>
            /// Gets the current item
            /// </summary>
            public uint Current { get; private set; }

            /// <summary>
            /// Gets the current item
            /// </summary>
            object IEnumerator.Current => Current;

            public int Index => _index;
            
            /// <summary>
            /// Initializes a new instance of the <see cref="EliasFanoEnumerator"/>
            /// </summary>
            /// <param name="collection">List to enumerate on</param>
            public EliasFanoEnumerator(EliasFanoList collection)
            {
                _collection = collection;
                _lowerBitsMask = (uint)(1 << _collection._lowerBitCount) - 1;
                Reset();
            }

            /// <summary>
            /// Moves to the next item in the list
            /// </summary>
            /// <returns>True if more items are available</returns>
            public bool MoveNext()
            {
                // advance the index
                _index += 1;

                if (_index < _collection.Count)
                {
                    // advance the pointer in the lower bits list
                    var lowerBits = _collection.GetLowBits(_lowerBitsPointer);
                    _lowerBitsPointer += _collection._lowerBitCount;

                    // get the upper bits
                    _upperBitsPointer = UnaryRead(_upperBitsPointer, false) + 1;
                    if (_upperBitsPointer == 0) return false; // exit if UnaryRead() returns uint.MaxValue
                    var upperBits = _upperBitsPointer - 1 - _index;

                    // calculate the item value
                    Current = (uint)(upperBits << _collection._lowerBitCount) | lowerBits;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            /// Moves to the given index
            /// </summary>
            /// <param name="index">Index to move to</param>
            /// <returns>True if the move was successful</returns>
            public bool SkipToIndex(int index)
            {
                // check for the number of items
                if (index >= _collection._count)
                {
                    return false;
                }

                // make sure we won't move backwards
                if (_index >= 0 && index + 1 <= _index)
                {
                    return false;
                }

                // intial setup if this is the first call to the enumerator
                if (_index < 0)
                {
                    _index = 0;
                    _upperBitsPointer = 1;
                }

                // reset the lower bits pointer to the new index
                _lowerBitsPointer = index * _collection._lowerBitCount;
                var lowerBits = _collection.GetLowBits(_lowerBitsPointer);
                _lowerBitsPointer += _collection._lowerBitCount;

                // skip forward using the skip table
                var skipIndex = (uint)Math.Min(index / _collection._indexQuantum, _collection._indexPointerCount - 1);
                if (_collection._indexPointers[skipIndex] > _upperBitsPointer)
                {
                    _upperBitsPointer = _collection._indexPointers[skipIndex];
                    _index = (int)(skipIndex * _collection._indexQuantum);
                }

                // get the upper bits
                while (_index < index)
                {
                    _upperBitsPointer = UnaryRead(_upperBitsPointer, false) + 1;
                    if (_upperBitsPointer == 0) return false; // exit if UnaryRead() returns uint.MaxValue
                    _index += 1;
                }
                var upperBits = _upperBitsPointer - 1 - _index;

                // calculate the value
                Current = (uint)(upperBits << _collection._lowerBitCount) | lowerBits;

                return true;
            }

            /// <summary>
            /// Moves to the next item greater than or equal to the given boundary
            /// </summary>
            /// <param name="boundary"></param>
            /// <returns></returns>
            public bool SkipToBoundary(uint boundary)
            {
                // calculate the number of negated unary reads to perform
                var numberOfReads = boundary >> _collection._lowerBitCount;

                // look up the starting position in the skip list
                var skipIndex = (uint)Math.Min(numberOfReads / _collection._skipQuantum, _collection._skipPointerCount - 1);
                var position = _collection._skipPointers[skipIndex];

                // perform a number of negated unary reads (minus the number of reads we skipped)
                for (var i = 0; i < numberOfReads - skipIndex * (uint)_collection._skipQuantum; i++)
                {
                    position = UnaryRead(position, true) + 1;
                    if (position == 0) return false; // exit if UnaryRead() returns uint.MaxValue
                }

                // calculate the index of the item we skipped to
                var index = position - numberOfReads;

                // read until we get a value greater than or equal to the boundary
                uint value;
                do
                {
                    // check the current index
                    if (index >= _collection._count)
                    {
                        return false;
                    }

                    // now perform a normal unary read to get the upper bits
                    position = UnaryRead(position, false) + 1;
                    if (position == 0) return false; // exit if UnaryRead() returns uint.MaxValue
                    var upperBits = position - index - 1;

                    // get the lower bits
                    var lowerBits = _collection.GetLowBits(_collection._lowerBitCount * (int)index);

                    // get the value of the item we skipped to
                    value = upperBits << _collection._lowerBitCount | lowerBits;

                    // increment the index for the next item to read
                    index += 1;
                }
                while (value < boundary || (index - 1) <= _index);

                _index = (int)index - 1;
                _lowerBitsPointer = (int)(index * _collection._lowerBitCount);
                _upperBitsPointer = position;
                Current = value;

                return true;
            }

            /// <summary>
            /// Resets the enumerator
            /// </summary>
            public void Reset()
            {
                _index = -1;
                _lowerBitsPointer = 0;
                _upperBitsPointer = 0;
            }

            /// <summary>
            /// Releases resources
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// Performs a unary read starting at the given location
            /// </summary>
            /// <param name="start">Bit index to start reading at</param>
            /// <param name="negated"></param>
            /// <returns>The bit index of the next set bit or <see cref="uint.MaxValue"/> if not found</returns>
            private uint UnaryRead(uint start, bool negated)
            {
                var i = start / 32; // word offset
                var b = start % 32; // bit offset in word
                var n = 0U; // index of lowest bit set in current value
                do
                {
                    var p = 0U;

                    // check for the end of the upper bit section
                    if (i * 32 > _collection._upperBitCount)
                    {
                        return uint.MaxValue;
                    }

                    // read the current value
                    var v = _collection._upperBits[i++];
                    if (negated) v = ~v;
                    v >>= (int)b;

                    // find the index of the lowest bit set or 32 if no bit is set
                    n = _collection.Select(v, 0, out p);

                    // do not skip any bits if we have to read the next word
                    if (n >= 32) b = 0;
                }
                while (n >= 32);

                // calculate the absolute index of the set bit relative to 'offset'
                return n + b + (i - 1) * 32;
            }
        }
    }

    /// <summary>
    /// Extension methods for compressing lists
    /// </summary>
    public static class EliasFanoExtensions
    {
        /// <summary>
        /// Compresses a list
        /// </summary>
        /// <param name="list">List to compress</param>
        /// <returns>Compressed list</returns>
        public static IReadOnlyList<uint> Compress(this IReadOnlyList<uint> list)
        {
            return new EliasFanoList(list);
        }

        /// <summary>
        /// Compresses an array
        /// </summary>
        /// <param name="array">Array to compress</param>
        /// <returns>Compressed list</returns>
        public static IReadOnlyList<uint> Compress(this uint[] array)
        {
            return new EliasFanoList(array[array.Length - 1], array.Length, array);
        }
    }
}
