#define NEWREADER

using log4net;
using logviewer.Interfaces;
using logviewer.query.Index;
using logviewer.query.Interfaces;
using logviewer.query.Parsing;
using logviewer.query.Readers;
using logviewer.query.Types;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.query
{
    /// <summary>
    /// Base class encapsulating access to an indexed log. It can be queried using the <see cref="Query"/> class.
    /// </summary>
    internal abstract class BaseLog : logviewer.Interfaces.ILog
    {
        /// <summary>
        /// The logger for the application log
        /// </summary>
        private readonly log4net.ILog _logger = LogManager.GetLogger(typeof(BaseLog));
        
        /// <summary>
        /// The application settings
        /// </summary>
        private readonly ISettings _settings;

        /// <summary>
        /// Additional indexers for processing the log while indexing
        /// </summary>
        private readonly ILogIndexer[] _indexers;

        /// <summary>
        /// The index of the log
        /// </summary>
        private readonly InvertedIndex _index;
        
        /// <summary>
        /// The source files or directories of the log
        /// </summary>
        private string[] _source = new string[0];
        
        /// <summary>
        /// The maximum progress value
        /// </summary>
        private long _progressMaximum = 0;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseLog"/> class.
        /// </summary>
        [ImportingConstructor]
        internal BaseLog(ISettings settings, InvertedIndex index, [ImportMany] IEnumerable<ILogIndexer> indexers)
        {
            _settings = settings;
            _index = index;
            _indexers = indexers.ToArray();
        }

        #region events

        /// <summary>
        /// Notifies changes of the log
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        
        /// <summary>
        /// Notifies changes of the log
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region properties

        /// <summary>
        /// Gets the source files of the log
        /// </summary>
        public string[] Files => FindFiles().Select(t => t.Item1).Distinct().ToArray();

        /// <summary>
        /// Gets the number of log entries
        /// </summary>
        public int Count => _index.Count;

        /// <summary>
        /// Gets the list of tokens
        /// </summary>
        public IReadOnlyCollection<string> Tokens => _index.Tokens;

        /// <summary>
        /// Gets the list of fields
        /// </summary>
        public IReadOnlyCollection<string> Fields => _index.Fields;

        /// <summary>
        /// Gets a value indicating the log is updating
        /// </summary>
        public bool IsUpdating
        {
            get;
            private set;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Load and index the given log files
        /// </summary>
        /// <param name="files">The files to index</param>
        /// <param name="progress">Action to report indexing progress</param>
        /// <param name="cancellationToken">CancellationToken for cancelling the index update</param>
        public void Load(string[] files, Action<double> progress, CancellationToken cancellationToken)
        {
            // set the source files
            _source = files;

            // initialize indexers
            for (var i = 0; i < _indexers.Length; i++) _indexers[i].Initialize();
            
            // and index the log
            Update(progress, cancellationToken);
        }
        
        /// <summary>
        /// Updates the index for the log
        /// </summary>
        /// <param name="progress">Action to report indexing progress</param>
        /// <param name="cancellationToken">CancellationToken for cancelling the index update</param>
        public bool Update(Action<double> progress, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            var updated = false;
            IsUpdating = true;

            // find all files of the log on disk
            var files = FindFiles();

            // remove indexed file which are no longer present
            foreach (var file in _index.Files.Where(f => !files.Any(p => p.Item1 == f.Item1 && p.Item2 == f.Item2)))
            {
                _index.Remove(file.Item1, file.Item2);
                updated = true;
            }

            // find the files which were changed since the last indexing or which are not present in the index
            var indexFiles = _index.Files;
            var indexTimestamp = indexFiles.Select(f => f.Item3).Concat(new[] { DateTime.MinValue }).Max();
            var changedFiles = files
            .GroupJoin(indexFiles, f => f.Item1 + f.Item2, f => f.Item1 + f.Item2, (file, index) => new { file, index = index.FirstOrDefault() })
            .Where(a => a.index == null || a.file.Item3 > a.index.Item3)
            .Select(a => a.file)
            .ToList();

            // when the tailing log file has changed just process the new lines, otherwise completely index all changed files
            if (changedFiles.Count > 0)
            {
                // update the maximum progress value
                _progressMaximum = changedFiles.Sum(f => f.Item4);

                // update the index with the changed files
                var progressCount = 0L;
                var stateCollection = new ConcurrentStack<object[]>();
                Parallel.ForEach(changedFiles, 
                    element =>
                    {
                        // create the indexer state for the file
                        var state = _indexers.Select(i => i.Initialize(element.Item5, element.Item1, element.Item2, element.Item4, element.Item3, false)).ToArray();

                        // create a stream for the file or archive member
                        var length = 0L;
                        var stream = OpenFileStream(element.Item1, element.Item2, out length);

                        // read and tokenize the file
                        LogReader<Token> tokenreader = null;
                        var lastProgress = 0L;

                        var sw2 = Stopwatch.StartNew();
                        try
                        {
                            var buffer = new Token[1024];
                            tokenreader = CreateTokenReader(stream, element.Item1, element.Item2);
                            var count = 0;
                            while ((count = tokenreader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                for (var i = 0; i < _indexers.Length; i++)
                                {
                                    _indexers[i].Update(state[i], buffer, count);
                                }
                                
                                if (tokenreader.Position > lastProgress + 256 * 1024)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    progress?.Invoke(Interlocked.Add(ref progressCount, tokenreader.Position - lastProgress) * 100.0 / _progressMaximum);
                                    lastProgress = tokenreader.Position;
                                }
                            }
                        }
                        finally
                        {
                            if (tokenreader != null)
                            {
                                tokenreader.Dispose();
                            }

                            sw2.Stop();
                            _logger.Info($"Log::Read(): Reading {element.Item1}:{element.Item2} completed in {sw2.ElapsedMilliseconds}ms");
                        }

                        // complete the files on the indexers
                        for (var i = 0; i < _indexers.Length; i++)
                        {
                            _indexers[i].Complete(state[i]);
                        }
                    });

                // complete the indexers
                for (var i = 0; i < _indexers.Length; i++)
                {
                    _indexers[i].Complete();
                }

                
                updated = true;
            }

            // send the collection changed event
            if (updated)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            sw.Stop();

            // signal the end of the update
            IsUpdating = false;

            // raise property changes
            if (updated)
            {
                _logger.Info($"Log::Update(): Updating index completed in {sw.ElapsedMilliseconds}ms");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Files)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tokens)));
            }
            else
            {
                _logger.Info("Log::Update(): Index is up to date");
            }

            return updated;
        }

        /// <summary>
        /// Query the log
        /// </summary>
        /// <param name="query">The textual description of the query</param>
        /// <param name="progress">A callback for reporting progresss</param>
        /// <param name="cancellation">A <see cref="CancellationToken"/> for canceling the operation</param>
        /// <returns>In instance of <see cref="IQuery"/> representing the query</returns>
        public IQuery Query(string query, Action<double> progress, CancellationToken cancellation)
        {
            var impl = QueryFactory.CreateQuery(this, _settings, query);
            impl.Update(progress, cancellation);
            return impl;
        }

        /// <summary>
        /// Read the log items
        /// </summary>
        /// <param name="progress">A callback for reporting progresss</param>
        /// <param name="cancellation">A <see cref="CancellationToken"/> for canceling the operation</param>
        /// <returns>Enumerable returning the log items</returns>
        public IEnumerable<ILogItem> Read(Action<double> progress, CancellationToken cancellation)
        {
            var total = 0L;
            return Read(_index.Search(new Token[0]), (s, t) => progress(Interlocked.Add(ref total, s) * 100 / (double)t), cancellation);
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Factory method for creating log item readers
        /// </summary>
        /// <param name="stream">Input stream to read</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member</param>
        /// <returns>Reader returning log items</returns>
        protected abstract LogReader<ILogItem> CreateItemReader(Stream stream, string file, string member);

        /// <summary>
        /// Factory method for creating index token readers
        /// </summary>
        /// <param name="stream">Input stream to read</param>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member</param>
        /// <returns>Reader returning index tokens</returns>
        protected abstract LogReader<Token> CreateTokenReader(Stream stream, string file, string member);

        #endregion

        #region internal methods

        /// <summary>
        /// Estimates the number of matches of the given tokens. 
        /// Result will be exact for a single token and larger for multiple tokens.
        /// </summary>
        /// <param name="tokens">The tokens to estimate</param>
        /// <returns>The number of ocurrences</returns>
        internal int Estimate(params Token[] tokens)
        {
            if (tokens.Length == 0)
            {
                return _index.Count;
            }
            else
            {
                return _index.Estimate(tokens);
            }
        }

        /// <summary>
        /// Searches the log for the given tokens and returns a stream of index items
        /// </summary>
        /// <param name="tokens">Tokens to search</param>
        /// <returns>Enumerable returning index items matching all tokens</returns>
        internal IEnumerable<IndexItem> Search(params Token[] tokens)
        {
            return _index.Search(tokens);
        }

        /// <summary>
        /// Read the log using the given index entries
        /// </summary>
        /// <param name="index">Enumerable returning the index entries to read</param>
        /// <returns>Enumerable returning the log entries</returns>
        internal IEnumerable<LogItem> Read(IEnumerable<IndexItem> index) => Read(index, null, CancellationToken.None);

        /// <summary>
        /// Read the log using the given index entries
        /// </summary>
        /// <param name="index">Enumerable returning the index entries to read</param>
        /// <param name="progress">Action to report reading progress based on bytes consumed</param>
        /// <param name="cancellation">CancellationToken for canceling the read operation</param>
        /// <returns>Enumerable returning the log entries</returns>
        internal IEnumerable<LogItem> Read(IEnumerable<IndexItem> index, Action<long, long> progress, CancellationToken cancellation)
        {
            var linesRead = 0;
            ZipArchive archive = null;
            Stream stream = null;
            var sw = Stopwatch.StartNew();
            try
            {
                var indexEnumerator = index.GetEnumerator();
                var readerFile = string.Empty;
                var readerMember = string.Empty;
                LogReader<ILogItem> reader = null;
                var lastProgress = 0L;

                while (true)
                {
                    // no more index entries to read
                    if (!indexEnumerator.MoveNext())
                    {
                        break;
                    }

                    // the file of the current index entry changed
                    if (readerFile != indexEnumerator.Current.File)
                    {
                        if (stream != null)
                        {
                            stream.Dispose();
                            reader = null;
                        }

                        if (archive != null)
                        {
                            archive.Dispose();
                        }

                        try
                        {
                            stream = new FileStream(indexEnumerator.Current.File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            readerFile = indexEnumerator.Current.File;
                            readerMember = string.Empty;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Log::Read(): Error while opening {indexEnumerator.Current.File}: {ex.Message}");
                            break;
                        }

                        if (!string.IsNullOrEmpty(indexEnumerator.Current.Member))
                        {
                            try
                            {
                                archive = new ZipArchive(stream);
                                stream = archive.GetEntry(indexEnumerator.Current.Member).Open();
                                readerMember = indexEnumerator.Current.Member;
                            }
                            catch (Exception ex)
                            {
                                _logger.Error($"Log::Read(): Error while opening {indexEnumerator.Current.File}::{indexEnumerator.Current.Member}: {ex.Message}");
                                break;
                            }
                        }
                    }

                    // the member of the current index entry changed
                    if (readerMember != indexEnumerator.Current.Member && !string.IsNullOrEmpty(indexEnumerator.Current.Member))
                    {
                        if (stream != null)
                        {
                            stream.Dispose();
                            reader = null;
                        }

                        try
                        {
                            stream = archive.GetEntry(indexEnumerator.Current.Member).Open();
                            readerMember = indexEnumerator.Current.Member;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Log::Read(): Error while opening {indexEnumerator.Current.File}::{indexEnumerator.Current.Member}: {ex.Message}");
                            break;
                        }
                    }
                    
                    // create the reader if not yet created
                    if (reader == null)
                    {
                        try
                        {
                            reader = CreateItemReader(stream, readerFile, readerMember);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Log::Read(): Error while creating reader for {indexEnumerator.Current.File}::{indexEnumerator.Current.Member}: {ex.Message}");
                            break;
                        }
                    }

                    // the current index entry points to a position further into the file
                    if (indexEnumerator.Current.Position > reader.Position)
                    {
                        reader.Seek(indexEnumerator.Current.Position, indexEnumerator.Current.Line, SeekOrigin.Begin);
                    }

                    // read data
                    yield return (LogItem)reader.Read();
                    linesRead += 1;

                    // check for cancellation and report progress
                    if (reader.Position > lastProgress + 256 * 1024)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        progress?.Invoke(reader.Position - lastProgress, _progressMaximum);
                        lastProgress = reader.Position;
                    }
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }

                if (archive != null)
                {
                    archive.Dispose();
                }

                sw.Stop();
                _logger.Info($"Log::Read(): Reading completed in {sw.ElapsedMilliseconds}ms ({linesRead} lines returned)");

                cancellation.ThrowIfCancellationRequested();
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Opens a stream to a given file
        /// </summary>
        /// <param name="file">Name of the file</param>
        /// <param name="member">Name of the archive member</param>
        /// <param name="length">Length of the stream</param>
        /// <returns>Stream returning file contents</returns>
        private Stream OpenFileStream(string file, string member, out long length)
        {
            // get information on the actual file
            var info = new FileInfo(file);

            // create a stream for the file or archive member
            length = info.Length;
            Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (!string.IsNullOrEmpty(member))
            {
                var archive = new ZipArchive(stream);
                stream = archive.GetEntry(member).Open();
                length = archive.GetEntry(member).Length;
            }

            return stream;
        }
        
        /// <summary>
        /// Find all files of the log
        /// </summary>
        /// <returns>A list containing log files in unordered sequence</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Objekte nicht mehrmals verwerfen")]
        private List<Tuple<string, string, DateTime, long, int>> FindFiles()
        {
            var comparer = new FileNameComparer();
            return _source.SelectMany(f =>
            {
                if (File.Exists(f))
                {
                    return new string[] { f };
                }
                else if (Directory.Exists(f))
                {
                    return Directory.EnumerateFiles(f);
                }
                else if (Directory.Exists(Path.GetDirectoryName(f)))
                {
                    return Directory
                        .EnumerateFiles(Path.GetDirectoryName(f))
                        .Where(f2 => f2.StartsWith(f));
                }
                else
                {
                    return new string[0];
                }
            }).SelectMany(file =>
            {
                if (Path.GetExtension(file) == ".zip")
                {
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        return archive.Entries.Select(e => new Tuple<string, string, DateTime, long>(file, e.FullName, e.LastWriteTime.DateTime, e.Length));
                    }
                }
                else
                {
                    var info = new FileInfo(file);
                    return new [] { new Tuple<string, string, DateTime, long>(file, string.Empty, info.LastWriteTime, info.Length) };
                }
            })
            .OrderBy(f => f.Item1, comparer)
            .ThenBy(f => f.Item2, comparer)
            .Select((f, i) => new Tuple<string, string, DateTime, long, int>(f.Item1, f.Item2, f.Item3, f.Item4, i))
            .ToList();
        }

        #endregion
    }
}
