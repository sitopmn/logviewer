using logviewer.Interfaces;
using logviewer.query.Index;
using logviewer.query.Interfaces;
using logviewer.query.Nodes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace logviewer.query.Visitors
{
    /// <summary>
    /// A visitor for generating an index from a log and a query tree
    /// </summary>
    internal class IndexVisitor : IVisitor
    {
        private readonly BaseLog _log;

        public IndexVisitor(BaseLog log)
        {
            _log = log;
        }

        public IEnumerable<IndexItem> Index { get; private set; } = Enumerable.Empty<IndexItem>();

        public Query.EvaluationMode Mode { get; private set; } = Query.EvaluationMode.Index;

        public long EstimatedCount { get; private set; }

        public void Visit(LimitNode node)
        {
            node.Inner[0].Accept(this);
        }

        public void Visit(OrderByNode node)
        {
            node.Inner[0].Accept(this);
            Mode = Query.EvaluationMode.Aggregate;
        }

        public void Visit(ProjectNode node)
        {
            node.Inner[0].Accept(this);
        }

        public void Visit(AggregateNode node)
        {
            node.Inner[0].Accept(this);
            Mode = Query.EvaluationMode.Aggregate;
        }

        public void Visit(GroupByNode node)
        {
            node.Inner[0].Accept(this);
            Mode = Query.EvaluationMode.Aggregate;
        }
        
        public void Visit(AndNode node)
        {
            var mode = Query.EvaluationMode.Index;
            var exclude = Enumerable.Empty<IndexItem>();
            var excludeCount = 0L;
            var include = new List<IEnumerable<IndexItem>>();
            var estimate = long.MaxValue;

            // generate include/exclude enumerators
            for (var i = 0; i < node.Inner.Count; i++)
            {
                if (node.Inner[i] is NotNode)
                {
                    node.Inner[i].Inner[0].Accept(this);
                    exclude = exclude.Concat(Index);
                    excludeCount += EstimatedCount;
                }
                else
                {
                    node.Inner[i].Accept(this);
                    if (EstimatedCount != _log.Count)
                    {
                        include.Add(Index);
                        estimate = Math.Min(estimate, EstimatedCount);
                    }
                }

                mode = new[] { Mode, mode }.Max();
            }

            // if no include is found, scan all
            if (include.Count == 0)
            {
                include.Add(_log.Search());
            }

            // define the iterator function
            IEnumerable<IndexItem> Iterator()
            {
                var except = new HashSet<IndexItem>(exclude);
                var enumerator = include.Select(i => Buffered(i).GetEnumerator()).ToArray();
                var hasNext = enumerator.Select(i => i.MoveNext()).ToArray();
                while (hasNext.All(i => i))
                {
                    var lowest = enumerator.Aggregate<IEnumerator<IndexItem>, IEnumerator<IndexItem>>(null, (a, i) => a != null ? (a.Current.CompareTo(i.Current) < 0 ? a : i) : i);
                    if (!except.Contains(lowest.Current) && enumerator.All(i => lowest.Current.CompareTo(i.Current) == 0))
                    {
                        yield return lowest.Current;
                    }
                
                    hasNext[Array.IndexOf(enumerator, lowest)] = lowest.MoveNext();
                }
            };

            // and set the results
            EstimatedCount = estimate - excludeCount;
            Mode = mode;
            Index = Iterator();
        }

        public void Visit(OrNode node)
        {
            var estimate = 0L;
            var mode = Query.EvaluationMode.Index;
            var include = new List<IEnumerable<IndexItem>>();
            var fullScan = false;

            // generate include enumerators
            for (var i = 0; i < node.Inner.Count; i++)
            {
                node.Inner[i].Accept(this);

                // optimize for the corner case where the full log is read i.e. the query
                // "{time:time} [" or "Foo":event="a" or "Bar":event="b"
                if (EstimatedCount == _log.Count)
                {
                    fullScan = true;
                }
                else if (EstimatedCount > 0)
                {
                    include.Add(Index);
                }

                mode = new[] { Mode, mode }.Max();
                estimate += EstimatedCount;
            }

            // define the iterator function
            IEnumerable<IndexItem> Iterator()
            {
                var enumerators = include.Select(i => Buffered(i).GetEnumerator()).ToArray();
                var hasNext = enumerators.Select(i => i.MoveNext()).ToArray();
                var last = new IndexItem();
                while (hasNext.Any(i => i))
                {
                    var lowest = enumerators.Where((a, i) => hasNext[i]).Aggregate<IEnumerator<IndexItem>, IEnumerator<IndexItem>>(null, (a, i) => a != null ? (a.Current.CompareTo(i.Current) < 0 ? a : i) : i);

                    if (last.CompareTo(lowest.Current) != 0)
                    {
                        yield return lowest.Current;
                        last = lowest.Current;
                    }

                    hasNext[Array.IndexOf(enumerators, lowest)] = lowest.MoveNext();
                }
            };

            // and set the results
            EstimatedCount = Math.Min(estimate, _log.Count);
            Mode = mode;
            if (fullScan)
            {
                Index = _log.Search();
            }
            else if (include.Count == 1)
            {
                Index = include[0];
            }
            else
            {
                Index = Iterator();
            }
        }

        public void Visit(NotNode node)
        {
            node.Inner[0].Accept(this);
            EstimatedCount = _log.Count - EstimatedCount;
            Index = _log.Search().Except(Index);
            Mode = Query.EvaluationMode.Index;
        }

        public void Visit(PredicateNode node)
        {
            node.Inner[0].Accept(this);
            Mode = Query.EvaluationMode.Evaluate;
        }

        public void Visit(PhraseNode node)
        {
            EstimatedCount = _log.Estimate(node.Tokens);
            Mode = node.Exact ? Query.EvaluationMode.Evaluate : Query.EvaluationMode.Index;
            if (EstimatedCount >= _log.Count)
            {
                Index = _log.Search();
            }
            else
            {
                Index = _log.Search(node.Tokens);
            }
        }

        public void Visit(ScanNode node)
        {
            Index = _log.Search();
            Mode = Query.EvaluationMode.Index;
            EstimatedCount = _log.Count;
        }

        private IEnumerable<IndexItem> Buffered(IEnumerable<IndexItem> input)
        {
            var buffer = new BlockingCollection<IndexItem>(1024);
            Task.Run(() =>
            {
                foreach (var e in input) buffer.Add(e);
                buffer.CompleteAdding();
            });

            return buffer.GetConsumingEnumerable();
        }
    }
}
