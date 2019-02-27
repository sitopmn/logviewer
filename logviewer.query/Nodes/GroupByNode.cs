using logviewer.query.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Nodes
{
    /// <summary>
    /// A query node for grouping log items
    /// </summary>
    internal class GroupByNode : Node
    {
        public GroupByNode(Node inner, string[] keyNames, Expression[] keys, string[] aggregateNames, IAggregate[] aggregates)
            : base(inner)
        {
            GroupNames = keyNames;
            GroupFunctions = keys.Select(e => Expression.Lambda<Func<LogItem, object>>(Expression.Convert(e, typeof(object)), QueryFactory.ItemVariable).Compile()).ToArray();
            GroupExpressions = keys;
            GroupTypes = keys.Select(e => e.Type).ToArray();
            AggregateNames = aggregateNames;
            Aggregates = aggregates;
            AggregateTypes = aggregates.Select(a => a.Type).ToArray();
        }

        public IAggregate[] Aggregates { get; }

        public string[] AggregateNames { get; }

        public Type[] AggregateTypes { get; }
        
        public string[] GroupNames { get; }

        public Type[] GroupTypes { get; }
        
        public Expression[] GroupExpressions { get; }

        public Func<LogItem, object>[] GroupFunctions { get; }

        public override void Accept(IVisitor visitor) => visitor.Visit(this);
    }
}
