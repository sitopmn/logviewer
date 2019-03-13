using log4net;
using logviewer.Interfaces;
using logviewer.query.Parsing;
using logviewer.core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace logviewer.query.Index
{
    /// <summary>
    /// A searchable inverted index for log files
    /// </summary>
    internal sealed class InvertedIndex : ILogIndexer
    {
        #region fields

        /// <summary>
        /// Logger for diagnostics
        /// </summary>
        private readonly log4net.ILog _logger = LogManager.GetLogger(typeof(InvertedIndex));

        /// <summary>
        /// Comparer for log file names
        /// </summary>
        private readonly static FileNameComparer _comparer = new FileNameComparer();

        /// <summary>
        /// Dictionary containing indices for each token
        /// </summary>
        private readonly Dictionary<string, List<IndexFile>> _tokens = new Dictionary<string, List<IndexFile>>();

        /// <summary>
        /// Index containing lines
        /// </summary>
        private readonly List<IndexFile> _lines = new List<IndexFile>();

        /// <summary>
        /// HashSet of field names found in the indexed data
        /// </summary>
        private readonly HashSet<string> _fields = new HashSet<string>();

        /// <summary>
        /// Lock object for modifying the index
        /// </summary>
        private readonly object _locker = new object();

        #endregion

        #region properties

        /// <summary>
        /// Gets tokens of the log
        /// </summary>
        public IReadOnlyCollection<string> Tokens => _tokens.Keys;

        /// <summary>
        /// Gets fields of the log
        /// </summary>
        public IReadOnlyCollection<string> Fields => _fields;

        /// <summary>
        /// Gets the list of files in the index
        /// </summary>
        public IReadOnlyCollection<Tuple<string, string, DateTime, long>> Files => _lines
            .OrderBy(f => f.File, _comparer)
            .ThenBy(f => f.Member, _comparer)
            .Select(f => new Tuple<string, string, DateTime, long>(f.File, f.Member, f.Timestamp, f.Length))
            .ToList();

        /// <summary>
        /// Number of indexed items
        /// </summary>
        public int Count => _lines.Sum(f => f.Positions.Count);
        
        #endregion

        #region ILogIndexer implementation

        /// <summary>
        /// Remove a file from the index
        /// </summary>
        /// <param name="file">Name of the file to remove</param>
        /// <param name="member">Name of the archive member to remove</param>
        public void Remove(string file, string member)
        {
            // lock the index while modifying
            lock (_locker)
            {
                if (_lines.Any(f => f.File == file && f.Member == member))
                {
                    // remove lines
                    foreach (var f in _lines.OrderBy(f => f.File, _comparer).ThenBy(f => f.Member, _comparer).SkipWhile(f => f.File != file || f.Member != member))
                    {
                        _lines.Remove(f);
                    }

                    // remove tokens
                    foreach (var token in _tokens)
                    {
                        foreach (var f in token.Value.OrderBy(f => f.File, _comparer).ThenBy(f => f.Member, _comparer).SkipWhile(f => f.File != file || f.Member != member))
                        {
                            token.Value.Remove(f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reset the index before a new log is indexed
        /// </summary>
        public void Initialize()
        {
            lock (_locker)
            {
                _tokens.Clear();
                _lines.Clear();
            }
        }

        /// <summary>
        /// Initializes indexing of a file
        /// </summary>
        /// <param name="fileIndex">Index of the file in the log</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Member if the file is a archive</param>
        /// <param name="length">Length of the file</param>
        /// <param name="timestamp">Timestamp of the file</param>
        /// <param name="append">True if the indexing is performed on appeded input data</param>
        /// <returns>State of the indexer</returns>
        public object Initialize(int fileIndex, string file, string member, long length, DateTime timestamp, bool append)
        {
            // remove data associated with the file if it is not to be appended
            if (!append)
            {
                Remove(file, member);
            }

            // create the indexer state
            var linePositions = new List<uint>();
            var tokens = new Dictionary<string, List<uint>>();
            var fields = new HashSet<string>();
            return new Tuple<List<uint>, Dictionary<string, List<uint>>, string, string, long, DateTime, HashSet<string>>(linePositions, tokens, file, member, length, timestamp, fields);
        }

        /// <summary>
        /// Updates the indexer state with a token
        /// </summary>
        /// <param name="state">State of the indexer</param>
        /// <param name="tokens">Tokens to update the state with</param>
        /// <param name="count">Number of tokens to process</param>
        public void Update(object state, Token[] token, int count)
        {
            var linePositions = ((Tuple<List<uint>, Dictionary<string, List<uint>>, string, string, long, DateTime, HashSet<string>>)state).Item1;
            var tokens = ((Tuple<List<uint>, Dictionary<string, List<uint>>, string, string, long, DateTime, HashSet<string>>)state).Item2;
            var fields = ((Tuple<List<uint>, Dictionary<string, List<uint>>, string, string, long, DateTime, HashSet<string>>)state).Item7;

            for (var i = 0; i < count; i++)
            {
                if (token[i].Type == ETokenType.Item)
                {
                    linePositions.Add((uint)token[i].Position);
                }
                else if (token[i].Type == ETokenType.Characters && token[i].Data.Length >= 3)
                {
                    List<uint> item;
                    if (!tokens.TryGetValue(token[i].Data, out item))
                    {
                        item = new List<uint>();
                        tokens.Add(token[i].Data, item);
                    }

                    item.Add((uint)token[i].Position);
                }
                else if (token[i].Type == ETokenType.Field && !fields.Contains(token[i].Data))
                {
                    fields.Add(token[i].Data);
                }
            }
        }

        /// <summary>
        /// Completes indexing of a file
        /// </summary>
        /// <param name="state">State of the indexer</param>
        public void Complete(object state)
        {
            var linePositions = ((Tuple<List<uint>, Dictionary<string, List<uint>>, string, string, long, DateTime, HashSet<string>>)state).Item1;
            var tokens = ((Tuple<List<uint>, Dictionary<string, List<uint>>, string, string, long, DateTime, HashSet<string>>)state).Item2;
            var file = ((Tuple<List<uint>, Dictionary<string, List<uint>>, string, string, long, DateTime, HashSet<string>>)state).Item3;
            var member = ((Tuple<List<uint>, Dictionary<string, List<uint>>, string, string, long, DateTime, HashSet<string>>)state).Item4;
            var length = ((Tuple<List<uint>, Dictionary<string, List<uint>>, string, string, long, DateTime, HashSet<string>>)state).Item5;
            var timestamp = ((Tuple<List<uint>, Dictionary<string, List<uint>>, string, string, long, DateTime, HashSet<string>>)state).Item6;
            var fields = ((Tuple<List<uint>, Dictionary<string, List<uint>>, string, string, long, DateTime, HashSet<string>>)state).Item7;

            lock (_locker)
            {
                // add the new lines to the index
                if (linePositions.Count > 0)
                {
                    var fileIndex = _lines.FindIndex(f => f.File == file && f.Member == member);
                    if (fileIndex < 0)
                    {
                        _lines.Add(new IndexFile(file, member, length, timestamp, linePositions.Compress()));
                        fileIndex = _lines.Count - 1;
                    }
                    else
                    {
                        if (linePositions.Count > 0)
                        {
                            _lines[fileIndex].Positions = new EliasFanoList(linePositions[linePositions.Count - 1], _lines[fileIndex].Positions.Count + linePositions.Count, _lines[fileIndex].Positions.Concat(linePositions));
                        }

                        _lines[fileIndex].Length = length;
                        _lines[fileIndex].Timestamp = timestamp;
                    }
                }

                // add the new tokens to the index
                foreach (var token in tokens)
                {
                    if (!_tokens.TryGetValue(token.Key, out List<IndexFile> list))
                    {
                        list = new List<IndexFile>();
                        _tokens[token.Key] = list;
                    }

                    var fileIndex = list.FindIndex(f => f.File == file && f.Member == member);
                    if (fileIndex < 0)
                    {
                        list.Add(new IndexFile(file, member, length, timestamp, token.Value.Compress()));
                        fileIndex = list.Count - 1;
                    }
                    else
                    {
                        list[fileIndex].Positions = new EliasFanoList(token.Value[token.Value.Count - 1], list[fileIndex].Positions.Count + token.Value.Count, list[fileIndex].Positions.Concat(token.Value));
                        list[fileIndex].Length = length;
                        list[fileIndex].Timestamp = timestamp;
                    }
                }

                // add the fields to the index
                foreach (var field in fields)
                {
                    if (!_fields.Contains(field))
                    {
                        _fields.Add(field);
                    }
                }
            }
        }

        /// <summary>
        /// Called after the complete log is indexed
        /// </summary>
        public void Complete()
        {
            // add the raw field if missing
            if (!_fields.Contains("_raw"))
            {
                _fields.Add("_raw");
            }
        }
        
        #endregion

        #region querying the index

        /// <summary>
        /// Estimates the number of items matching the given tokens
        /// </summary>
        /// <param name="tokens">The tokens to match</param>
        /// <returns>The estimated number of matching items, presumably larger than the actual number</returns>
        public int Estimate(Token[] tokens)
        {
            if (tokens.All(t => _tokens.ContainsKey(t.Data)))
            {
                return tokens.Select(t => _tokens[t.Data].Sum(f => f.Positions.Count)).Min();
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Searches the index for items
        /// </summary>
        /// <param name="tokens">The list of tokens which have to occur in the document in the given order</param>
        /// <returns>An enumerable returning the matched items</returns>
        public IEnumerable<IndexItem> Search(Token[] tokens)
        {
            // return all lines if no tokens given
            if (tokens.Length == 0)
            {
                var files = new List<Tuple<string, string, IReadOnlyList<uint>, IReadOnlyList<uint>>>();
                var fileStart = 0;
                foreach (var file in _lines.OrderBy(i => i.File, _comparer).ThenBy(i => i.Member, _comparer))
                {
                    var lines = new EliasFanoList((uint)(fileStart + file.Positions.Count), file.Positions.Count, Enumerable.Range(fileStart, file.Positions.Count).Select(i => (uint)i));
                    files.Add(new Tuple<string, string, IReadOnlyList<uint>, IReadOnlyList<uint>>(file.File, file.Member, file.Positions, lines));
                    fileStart += file.Positions.Count;
                }

                // return the original compressed index in case we want to compress it later on (highly possible when returning all lines -> full view of log is visible)
                return new DirectIndex(files);
            }
            else
            {
                return SearchIterator(tokens);
            }
        }
        
        /// <summary>
        /// Gets the index of specified index item
        /// </summary>
        /// <param name="item">The item to find</param>
        /// <returns>The index of the item or -1 if it is not found</returns>
        public int IndexOf(IndexItem item)
        {
            var index = 0;
            foreach (var f in _lines.OrderBy(i => i.File, _comparer).ThenBy(i => i.Member, _comparer))
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

        #endregion

        #region private methods

        /// <summary>
        /// Checks a document containing the given tokens against the token list
        /// </summary>
        /// <param name="tokens">The tokens to check against</param>
        /// <param name="documentPositions">The tokens of the document</param>
        /// <returns>True when the document matches the tokens</returns>
        private static bool CheckDocument(Token[] tokens, List<uint>[] documentPositions)
        {
            var found = false;
            if (documentPositions.All(d => d.Count > 0))
            {
                found = true;
                for (var t = 1; t < tokens.Length; t++)
                {
                    if (tokens[t].IsExact)
                    {
                        var delta = tokens[t].Position - tokens[t - 1].Position;
                        if (!documentPositions[t].Any(p1 => documentPositions[t - 1].Any(p2 => p1 - p2 == delta)))
                        {
                            found = false;
                            break;
                        }
                    }
                    else
                    {
                        if (!documentPositions[t].Any(p1 => documentPositions[t - 1].Any(p2 => p1 > p2)))
                        {
                            found = false;
                            break;
                        }
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Finds the start and end position of a line given a position within the line
        /// </summary>
        /// <param name="position">The position to find the line for</param>
        /// <param name="lines">Enumeration containing line start positions</param>
        /// <param name="lineStart">The start position of the line containing the given line</param>
        /// <param name="lineEnd">The end position of the line containing the given line</param>
        private static bool FindLine(uint position, IEnumerator<uint> lines, long length, ref uint lineStart, ref uint lineEnd, ref int lineNumber)
        {
            if (position >= length)
            {
                return false;
            }

            while (lineEnd <= position)
            {
                if (!lines.MoveNext())
                {
                    lineStart = lineEnd;
                    lineEnd = (uint)length;
                    return true;
                }
                lineStart = lineEnd;
                lineEnd = lines.Current;
                lineNumber += 1;
            }

            return true;
        }

        /// <summary>
        /// Iterator method for searching log lines containing the given tokens
        /// </summary>
        /// <param name="tokens">Tokens to search</param>
        /// <returns>Enumerable returning all matching log lines</returns>
        private IEnumerable<IndexItem> SearchIterator(Token[] tokens)
        {
            // stop here if not all tokens are found in the index
            if (tokens.Any(t => !_tokens.ContainsKey(t.Data)))
            {
                yield break;
            }

            // find the token with the smallest frequency
            var anchorToken = tokens.Aggregate(new Tuple<int, string>(int.MaxValue, null), (list, token) =>
            {
                var count = _tokens[token.Data].Sum(f => f.Positions.Count);
                if (count < list.Item1)
                {
                    return new Tuple<int, string>(count, token.Data);
                }
                else
                {
                    return list;
                }
            });

            // stop of no token was found
            if (string.IsNullOrEmpty(anchorToken.Item2))
            {
                yield break;
            }

            // find the index of the anchor token in the token array
            var anchorIndex = Array.FindIndex(tokens, t => t.Data == anchorToken.Item2);
            if (anchorIndex < 0)
            {
                throw new InvalidOperationException("Anchor token index not found!?");
            }

            // gather runtime stats
            var totalTokenCount = _tokens.Sum(t => t.Value.Sum(f => f.Positions.Count));
            var resultCount = 0;
            _logger.Info($"Index::Search(): Searching tokens {string.Join(", ", tokens.Select(t => $"'{t.Data}'[{_tokens[t.Data].Sum(f => f.Positions.Count)}, {Math.Log10(Count / (double)_tokens[t.Data].Sum(f => f.Positions.Count))}]"))}");
            var sw = Stopwatch.StartNew();

            try
            {
                // loop over each file in the anchor token
                foreach (var file in _tokens[anchorToken.Item2].OrderBy(i => i.File, _comparer).ThenBy(i => i.Member, _comparer))
                {
                    // select the current file for each token which is not the anchor token
                    var tokenFiles = tokens
                        .Select(t => _tokens[t.Data].FirstOrDefault(f => f.File == file.File && f.Member == file.Member))
                        .ToList();

                    // check if the current file is available for every token. if it is not, we can skip the file entirely
                    if (tokenFiles.Any(f => f == null))
                    {
                        continue;
                    }

                    // create enumerators for the token positions (except the anchor) in the current file and move them to the first item
                    var enumerators = tokenFiles.Select(f => (ISkippingEnumerator<uint>)f.Positions.GetEnumerator()).ToArray();
                    var next = enumerators.Select((e, i) => i == anchorIndex ? true : e.MoveNext()).ToArray();

                    // create an enumerator for the lines
                    int lineCount = _lines.Single(f => f.File == file.File && f.Member == file.Member).Positions.Count;
                    var lines = (ISkippingEnumerator<uint>)_lines.Single(f => f.File == file.File && f.Member == file.Member).Positions.GetEnumerator();
                    uint lineStart = 0;
                    uint lineEnd = lines.MoveNext() ? lines.Current : (uint)lineCount;
                    int lineNumber = 0;
                    uint lastLineStart = 0;
                    int lastLineNumber = -1;

                    // iterate over the anchor token
                    var documentPositions = enumerators.Select(t => new List<uint>()).ToArray();
                    while (enumerators[anchorIndex].MoveNext())
                    {
                        // find the current line the anchor token is located in
                        if (!FindLine(enumerators[anchorIndex].Current, lines, file.Length, ref lineStart, ref lineEnd, ref lineNumber))
                        {
                            throw new InvalidOperationException("Token found beyond file length");
                        }

                        // check if we moved into another document and return the last document to the caller if it matches
                        if (lastLineNumber != lineNumber)
                        {
                            // check if the document meets the search criteria exactly
                            if (CheckDocument(tokens, documentPositions))
                            {
                                yield return new IndexItem(file.File, file.Member, lastLineStart, lastLineNumber);
                                resultCount += 1;
                            }

                            // clear the document positions
                            foreach (var dp in documentPositions) dp.Clear();
                            lastLineStart = lineStart;
                            lastLineNumber = lineNumber;
                        }

                        // record all token positions for the current line
                        documentPositions[anchorIndex].Add(enumerators[anchorIndex].Current);

                        // read all enumerators until they move beyond the end of the current line
                        for (var i = 0; i < enumerators.Length; i++)
                        {
                            while (i != anchorIndex && next[i] && enumerators[i].Current < lineEnd)
                            {
                                if (enumerators[i].Current >= lineStart && enumerators[i].Current < lineEnd)
                                {
                                    documentPositions[i].Add(enumerators[i].Current);
                                }

                                next[i] = enumerators[i].SkipToBoundary(lineStart);
                            }
                        }

                        // if any enumerator has stopped, the whole query stops here
                        if (next.Any(n => n == false))
                        {
                            break;
                        }
                    }

                    // check the last document
                    if (CheckDocument(tokens, documentPositions))
                    {
                        yield return new IndexItem(file.File, file.Member, lastLineStart, lastLineNumber);
                        resultCount += 1;
                    }
                }
            }
            finally
            {
                sw.Stop();
                _logger.Info($"Index::Search(): Returned {resultCount} results in {sw.ElapsedMilliseconds} ms");
            }
        }

        #endregion

        #region private types

        /// <summary>
        /// Structure describing an indexed file
        /// </summary>
        private class IndexFile
        {
            public readonly string File;
            public readonly string Member;
            public long Length;
            public DateTime Timestamp;
            public IReadOnlyList<uint> Positions;

            /// <summary>
            /// Initializes a new instance of the <see cref="IndexFile"/> class.
            /// </summary>
            /// <param name="file">The file name</param>
            /// <param name="member">The member name if the file is an archive</param>
            /// <param name="length">The length of the file</param>
            /// <param name="timestamp">Timestamp of the file</param>
            public IndexFile(string file, string member, long length, DateTime timestamp, IReadOnlyList<uint> positions)
            {
                File = file;
                Member = member;
                Length = length;
                Timestamp = timestamp;
                Positions = positions;
            }
        }

        #endregion
    }
}
