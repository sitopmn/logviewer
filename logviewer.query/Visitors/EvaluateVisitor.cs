using logviewer.query.Interfaces;
using logviewer.query.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Visitors
{
    /// <summary>
    /// A visitor for evaluating an query on a log, an index and a query tree
    /// </summary>
    internal class EvaluateVisitor : IVisitor
    {
        public EvaluateVisitor(ParallelQuery<LogItem> log)
        {
            Items = log;
        }

        public ParallelQuery<LogItem> Items { get; private set; }

        public Dictionary<string, Type> Fields { get; private set; } = new Dictionary<string, Type>();

        public void Visit(LimitNode node)
        {
            node.Inner[0].Accept(this);
            Items = Items.Take(node.Count);
        }

        public void Visit(OrderByNode node)
        {
            node.Inner[0].Accept(this);

            OrderedParallelQuery<LogItem> ordered;

            if (node.Descending[0])
            {
                ordered = Items.OrderByDescending(i => i.Fields[node.Fields[0]]);
            }
            else
            {
                ordered = Items.OrderBy(i => i.Fields[node.Fields[0]]);
            }

            for (var n = 1; n < node.Fields.Length; n++)
            {
                if (node.Descending[n])
                {
                    ordered = ordered.ThenByDescending(i => i.Fields[node.Fields[n]]);
                }
                else
                {
                    ordered = ordered.ThenBy(i => i.Fields[node.Fields[n]]);
                }
            }

            Items = ordered;
        }

        public void Visit(ProjectNode node)
        {
            node.Inner[0].Accept(this);
            Items = Items.Select(i =>
            {
                var newFields = node.Projections.Select(p => p(i)).ToArray();
                i.Fields.Clear();
                for (var n = 0; n < node.Names.Length; n++) i.Fields[node.Names[n]] = newFields[n];
                if (!i.Fields.ContainsKey("message")) i.Fields["message"] = string.Empty;
                return i;
            });
        }

        public void Visit(AggregateNode node)
        {
            node.Inner[0].Accept(this);

            Items = Items.Aggregate(() => node.Aggregates.Select(a => a.Initialize()).ToArray(), Update, Join, Complete).AsParallel().AsOrdered();

            object[] Update(object[] state, LogItem item)
            {
                for (var n = 0; n < node.Aggregates.Length; n++)
                {
                    state[n] = node.Aggregates[n].Update(state[n], item);
                }

                return state;
            }

            object[] Join(object[] a, object[] b)
            {
                for (var n = 0; n < node.Aggregates.Length; n++)
                {
                    a[n] = node.Aggregates[n].Join(a[n], b[n]);
                }

                return a;
            }

            IEnumerable<LogItem> Complete(object[] state)
            {
                var aggregates = node.Aggregates.Select((a, i) => a.Complete(state[i]).GetEnumerator()).ToArray();
                var hasNext = aggregates.Select(a => a.MoveNext()).ToArray();
                while (hasNext.Any(a => a))
                {
                    var item = new LogItem(string.Empty, string.Empty, string.Empty, 0, 0);
                    for (var n = 0; n < aggregates.Length; n++)
                    {
                        if (hasNext[n])
                        {
                            item.Fields[node.Names[n]] = aggregates[n].Current;
                        }
                        else
                        {
                            item.Fields[node.Names[n]] = null;
                        }

                        hasNext[n] = aggregates[n].MoveNext();
                    }

                    yield return item;
                }
            }
        }

        public void Visit(GroupByNode node)
        {
            node.Inner[0].Accept(this);

            Items = Items.Aggregate(() => new Dictionary<GroupKey, object[]>(), Update, Join, Complete).AsParallel().AsOrdered();

            Dictionary<GroupKey, object[]> Update(Dictionary<GroupKey, object[]> state, LogItem item)
            {
                // calculate the key for the item
                var key = new GroupKey(item, node.GroupFunctions.Select(k => k(item)).ToArray());

                // find the group for the item
                object[] group;
                if (!state.TryGetValue(key, out group))
                {
                    group = node.Aggregates.Select(a => a.Initialize()).ToArray();
                    state[key] = group;
                }

                // update the group with the item
                for (var n = 0; n < node.Aggregates.Length; n++)
                {
                    group[n] = node.Aggregates[n].Update(group[n], item);
                }

                return state;
            }

            Dictionary<GroupKey, object[]> Join(Dictionary<GroupKey, object[]> a, Dictionary<GroupKey, object[]> b)
            {
                foreach (var group in b)
                {
                    if (a.ContainsKey(group.Key))
                    {
                        a[group.Key] = node.Aggregates.Select((x, i) => x.Join(a[group.Key][i], group.Value[i])).ToArray();
                    }
                    else
                    {
                        a[group.Key] = group.Value;
                    }
                }

                return a;
            }

            IEnumerable<LogItem> Complete(Dictionary<GroupKey, object[]> state)
            {
                foreach (var key in state.Keys)
                {
                    // create a new item and populate it using the group key
                    var item = new LogItem(string.Empty, key.Anchor.File, key.Anchor.Member, key.Anchor.Position, key.Anchor.Line);
                    for (var n = 0; n < node.GroupNames.Length; n++) item.Fields[node.GroupNames[n]] = key.Values[n];

                    // complete all aggregates for the group and add them to the group
                    var aggregates = node.Aggregates.Select((a, i) => a.Complete(state[key][i]).ToList()).ToList();
                    for (var n = 0; n < node.Aggregates.Length; n++)
                    {
                        if (aggregates[n].Count > 1)
                        {
                            item.Fields[node.AggregateNames[n]] = string.Join("\n", aggregates[n]);
                        }
                        else
                        {
                            item.Fields[node.AggregateNames[n]] = aggregates[n].FirstOrDefault();
                        }
                    }

                    yield return item;
                }
            }
        }

        public void Visit(PredicateNode node)
        {
            var predicate = Expression.Lambda<Func<LogItem, bool>>(node.Predicate, QueryFactory.ItemVariable).Compile();
            node.Inner[0].Accept(this);
            Items = Items.Where(predicate);
        }

        public void Visit(MatchNode node)
        {
            var predicate = Expression.Lambda<Func<LogItem, bool>>(node.Predicate(), QueryFactory.ItemVariable).Compile();
            Items = Items.Where(predicate);
        }

        public void Visit(AndNode node) => Visit((MatchNode)node);

        public void Visit(OrNode node) => Visit((MatchNode)node);

        public void Visit(NotNode node) => Visit((MatchNode)node);

        public void Visit(PhraseNode node) => Visit((MatchNode)node);

        public void Visit(ScanNode node) => Visit((MatchNode)node);

        /// <summary>
        /// A key for grouping query result items
        /// </summary>
        private struct GroupKey
        {
            public GroupKey(LogItem anchor, object[] values)
            {
                Anchor = anchor;
                Values = values;
                Length = values.Length;
            }

            public readonly object[] Values;

            public readonly int Length;

            public readonly LogItem Anchor;

            public override int GetHashCode()
            {
                return Values.Aggregate(0, (a, b) => a ^ (b != null ? b.GetHashCode() : 0)) ^ Length.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is GroupKey key)
                {
                    return key.Length == Length && key.Values.SequenceEqual(Values);
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
