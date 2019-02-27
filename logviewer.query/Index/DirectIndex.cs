using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Index
{
    /// <summary>
    /// A direct index for log files
    /// </summary>
    internal class DirectIndex : IEnumerable<IndexItem>
    {
        /// <summary>
        /// Index containing lines
        /// </summary>
        private readonly List<IndexFile> _index = new List<IndexFile>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectIndex"/> class.
        /// </summary>
        /// <param name="items">An enumeration of index items</param>
        public DirectIndex(IEnumerable<IndexItem> items)
        {
            if (items is DirectIndex index)
            {
                // shortcut in case the given enumerable already is a compatible index: we don't iterate it all again
                _index.AddRange(index._index);
            }
            else
            {
                var currentFile = string.Empty;
                var currentMember = string.Empty;
                var currentPositions = new List<uint>();
                var currentLines = new List<uint>();
                var currentIndex = 0;
                var startIndex = 0;
                foreach (var i in items)
                {
                    if (i.File != currentFile || i.Member != currentMember)
                    {
                        if (!string.IsNullOrEmpty(currentFile))
                        {
                            _index.Add(new IndexFile(currentFile, currentMember, startIndex, currentPositions.Compress(), currentLines.Compress()));
                        }

                        currentFile = i.File;
                        currentMember = i.Member;
                        currentPositions.Clear();
                        currentLines.Clear();
                        startIndex = currentIndex;
                    }

                    currentPositions.Add((uint)i.Position);
                    currentLines.Add((uint)i.Line);
                    currentIndex += 1;
                }
                if (!string.IsNullOrEmpty(currentFile))
                {
                    _index.Add(new IndexFile(currentFile, currentMember, startIndex, currentPositions.Compress(), currentLines.Compress()));
                }
            }
        }

        public DirectIndex(IEnumerable<Tuple<string, string, IReadOnlyList<uint>, IReadOnlyList<uint>>> files)
        {
            var fileIndex = 0;
            foreach (var file in files)
            {
                _index.Add(new IndexFile(file.Item1, file.Item2, fileIndex, file.Item3, file.Item4));
                fileIndex += file.Item3.Count;
            }
        }

        /// <summary>
        /// Gets the number of items in the index
        /// </summary>
        public int Count => _index.Sum(f => f.Positions.Count);

        /// <summary>
        /// Adds items to the index
        /// </summary>
        /// <param name="items">Enumerable returning the items to add</param>
        public void Add(IEnumerable<IndexItem> items)
        {
            foreach (var item in items)
            {
                Trace.WriteLine($"Adding {item.Position} to index");

                var index = _index.FindIndex(i => i.File == item.File && i.Member == item.Member);
                if (index == _index.Count - 1 && item.Position > _index[index].Positions[_index[index].Positions.Count - 1])
                {
                    ((EliasFanoList)_index[index].Positions).Add((uint)item.Position);
                    ((EliasFanoList)_index[index].Lines).Add((uint)item.Line);
                }
                else if (index >= 0)
                {
                    throw new InvalidOperationException("Can only append to the index");
                }
                else
                {
                    _index.Add(new IndexFile(item.File, item.Member, Count, new[] { (uint)item.Position }.Compress(), new[] { (uint)item.Line }.Compress()));
                }
            }
        }

        /// <summary>
        /// Gets an enumerable returning all index entries skipping the first <see cref="count"/> entries.
        /// </summary>
        /// <param name="count">The number of entries to skip</param>
        /// <returns>An enumerator returning all index entries except for the first <see cref="count"/> entries</returns>
        public IEnumerable<IndexItem> Skip(int count)
        {
            var firstIndex = _index.FindLastIndex(f => f.StartIndex <= count);
            if (firstIndex >= 0)
            {
                var fileEnumerator = _index.Skip(firstIndex).GetEnumerator();
                ISkippingEnumerator<uint> positionEnumerator = null;
                ISkippingEnumerator<uint> lineEnumerator = null;
                var skip = count - _index[firstIndex].StartIndex;
                while (fileEnumerator.MoveNext())
                {
                    positionEnumerator = (ISkippingEnumerator<uint>)fileEnumerator.Current.Positions.GetEnumerator();
                    lineEnumerator = (ISkippingEnumerator<uint>)fileEnumerator.Current.Lines.GetEnumerator();

                    if (skip > 0)
                    {
                        if (!positionEnumerator.SkipToIndex(skip)) yield break;
                        if (!lineEnumerator.SkipToIndex(skip)) yield break;
                        skip = 0;
                    }
                    else
                    {
                        if (!positionEnumerator.MoveNext()) yield break;
                        if (!lineEnumerator.MoveNext()) yield break;
                    }

                    do
                    {
                        yield return new IndexItem(fileEnumerator.Current.File, fileEnumerator.Current.Member, positionEnumerator.Current, (int)lineEnumerator.Current);
                    }
                    while (positionEnumerator.MoveNext() && lineEnumerator.MoveNext());
                }
            }
        }

        /// <summary>
        /// Gets an enumerator returning index entries
        /// </summary>
        /// <returns>An enumerator returning all index entries</returns>
        public IEnumerator<IndexItem> GetEnumerator()
        {
            return Skip(0).GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator returning index entries
        /// </summary>
        /// <returns>An enumerator returning all index entries</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the index of specified log item in the complete log file
        /// </summary>
        /// <param name="item">The item to find</param>
        /// <returns>The index of the item or -1 if it is not found</returns>
        public int IndexOf(IndexItem item)
        {
            var index = 0;
            foreach (var f in _index)
            {
                if (f.File == item.File && f.Member == item.Member)
                {
                    foreach (var p in f.Positions)
                    {
                        if (p == item.Position)
                        {
                            return index;
                        }
                        else
                        {
                            index += 1;
                        }
                    }
                }
                else
                {
                    index += f.Positions.Count;
                }
            }

            return -1;
        }

        /// <summary>
        /// Structure describing an indexed file
        /// </summary>
        private class IndexFile
        {
            public readonly string File;
            public readonly string Member;
            public readonly int StartIndex;
            public readonly IReadOnlyList<uint> Positions;
            public readonly IReadOnlyList<uint> Lines;
            public IndexFile(string file, string member, int startIndex, IReadOnlyList<uint> positions, IReadOnlyList<uint> lines)
            {
                File = file;
                Member = member;
                StartIndex = startIndex;
                Positions = positions;
                Lines = lines;
            }
        }
    }
}
