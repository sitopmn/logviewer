﻿using log4net;
using logviewer.Interfaces;
using logviewer.query.Index;
using logviewer.query.Nodes;
using logviewer.query.Visitors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.query
{
    /// <summary>
    /// A class for executing queries on an indexed log provided by a <see cref="Log"/> instance.
    /// </summary>
    internal class Query : VirtualList<ILogItem>, IQuery
    {
        #region fields

        /// <summary>
        /// Logger for the query implementation
        /// </summary>
        private readonly log4net.ILog _logger = LogManager.GetLogger(typeof(Query));

        /// <summary>
        /// Tree representation of the query
        /// </summary>
        private readonly Node _tree;

        /// <summary>
        /// Log providing the query source data
        /// </summary>
        private readonly Log _log;

        /// <summary>
        /// Static columns of the query
        /// </summary>
        private readonly IDictionary<string, Type> _columns;

        /// <summary>
        /// Dynamic (runtime generated, data dependent) columns of the query
        /// </summary>
        private readonly IDictionary<string, Type> _dynamicColumns;

        /// <summary>
        /// Index of the query results into the log
        /// </summary>
        private DirectIndex _index;

        /// <summary>
        /// Evaluation mode of the query
        /// </summary>
        private EvaluationMode _mode;
        
        #endregion

        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="Query"/> class.
        /// </summary>
        /// <param name="log">The log to query</param>
        /// <param name="settings">The application settings</param>
        /// <param name="query">The query string to execute</param>
        internal Query(Log log, Node tree)
        {
            _log = log;
            _tree = tree;

            // define result columns for the query
            var visitor = new FieldsVisitor();
            _tree.Accept(visitor);
            _columns = visitor.Fields;
            if (visitor.GeneratesDynamicFields)
            {
                _dynamicColumns = new Dictionary<string, Type>();
            }

            // print the query tree for debugging
            var format = new FormatVisitor();
            _tree.Accept(format);
            _logger.Info($"Created query tree\r\n" + format.ToString().TrimEnd());
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets a list of columns generated by the query
        /// </summary>
        public IDictionary<string, Type> Columns => _dynamicColumns != null ? _columns.Concat(_dynamicColumns).ToDictionary(k => k.Key, k => k.Value) : _columns;

        /// <summary>
        /// Gets a value indicating the query was executed at least once
        /// </summary>
        private bool IsExecuted => _index != null;

        #endregion

        #region public methods
        
        /// <summary>
        /// Updates the query results
        /// </summary>
        public void Update(Action<double> progress, CancellationToken cancellation)
        {
            // stop here if the log is empty
            if (_log.Count == 0)
            {
                _logger.Debug($"Evaluation canceled by empty log");
                _mode = EvaluationMode.Aggregate;
                Reset(0);
                return;
            }
            
            // start timers for performance measurement
            var sw = Stopwatch.StartNew();
            var sw2 = Stopwatch.StartNew();

            // generate the raw index
            var indexVisitor = new IndexVisitor(_log);
            _tree.Accept(indexVisitor);

            // create an enumerator for the index
            var index = indexVisitor.Index;
            _mode = indexVisitor.Mode;
            
            // refine the index by evaluating log items
            IEnumerable<LogItem> items = null;
            Dictionary<string, Type> dynamicFields = null;
            if (_mode == EvaluationMode.Evaluate || _mode == EvaluationMode.Aggregate)
            {
                long progressTotal = 0;

                // build a parallel query for reading the log
                var rawItems = index
                    .AsParallel()
                    .AsOrdered()
                    .WithCancellation(cancellation)
                    .GroupBy(i => i.File + i.Member)
                    .SelectMany(f => _log.Read(f, (step, total) => progress?.Invoke((Interlocked.Add(ref progressTotal, step) * 98.0) / total), cancellation));

                // apply the query to the log
                var evaluateVisitor = new EvaluateVisitor(rawItems);
                _tree.Accept(evaluateVisitor);
                items = evaluateVisitor.Items;

                // store dynamic fields
                dynamicFields = evaluateVisitor.Fields;
            }

            // store the result as specified
            if (_mode == EvaluationMode.Aggregate)
            {
                _index = new DirectIndex(Enumerable.Empty<IndexItem>());
                Reset(items);
                _logger.Info($"Evaluation completed in {sw2.ElapsedMilliseconds} ms");
            }
            else if (_mode == EvaluationMode.Evaluate)
            {
                _index = new DirectIndex(items.Select(i => new IndexItem(i.File, i.Member, i.Position, i.Line)));
                Reset(_index.Count);
                _logger.Info($"Evaluation and reindexing completed after {sw2.ElapsedMilliseconds} ms");
            }
            else
            {
                _index = new DirectIndex(index);
                Reset(_index.Count);
                _logger.Info($"Indexing completed after {sw2.ElapsedMilliseconds} ms");
            }

            // generate the dynamic columns
            if (_dynamicColumns != null)
            {
                _dynamicColumns.Clear();
                foreach (var kvp in dynamicFields)
                {
                    _dynamicColumns.Add(kvp.Key, kvp.Value);
                }
            }

            sw.Stop();
            _logger.Info($"Execution completed in {sw.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Releases resources held by the query
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Gets the index of a log item
        /// </summary>
        /// <param name="item">The log item to search</param>
        /// <returns>The index of the given item or -1 if it is not found</returns>
        public override int IndexOf(ILogItem item)
        {
            if (item != null && IsExecuted)
            {
                if (_mode == EvaluationMode.Aggregate)
                {
                    return base.IndexOf(item);
                }
                else
                {
                    return _index.IndexOf(new IndexItem(item.File, item.Member, item.Position, item.Line));
                }
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Skips the given number of items and returns the remaining items
        /// </summary>
        /// <param name="count">Number of items to skip</param>
        /// <returns>Enumerable returning the remaining items</returns>
        public IEnumerable<ILogItem> Skip(int count)
        {
            for (var i = count; i < Count; i++)
            {
                yield return this[i];
            }
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Loads a part of the query results into a buffer
        /// </summary>
        /// <param name="index">Starting index to load from</param>
        /// <param name="data">Buffer to load the items into</param>
        protected override void Load(int index, ILogItem[] data)
        {
            var evaluator = new EvaluateVisitor(_log.Read(_index.Skip(index).Take(data.Length)).AsParallel().AsOrdered());
            _tree.Accept(evaluator);

            var i = 0;
            foreach (var item in evaluator.Items)
            {
                data[i++] = item;
            }
        }

        #endregion
        
        #region private types

        /// <summary>
        /// The evaluation strategy employed for the query
        /// </summary>
        internal enum EvaluationMode
        {
            /// <summary>
            /// A result index is created based on the inverted index
            /// </summary>
            Index = 0,

            /// <summary>
            /// As <see cref="Index"/> with an additional evaluation of the individual items
            /// </summary>
            Evaluate = 1,

            /// <summary>
            /// As <see cref="Aggregate"/> and a stored result list
            /// </summary>
            Aggregate = 2,
        }

        #endregion
    }
}
