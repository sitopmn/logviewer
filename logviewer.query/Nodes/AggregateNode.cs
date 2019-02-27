using logviewer.query.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Nodes
{
    /// <summary>
    /// A query node for aggregating log items
    /// </summary>
    internal class AggregateNode : Node
    {
        public AggregateNode(Node inner, string[] aggregateNames, IAggregate[] aggregates)
            : base(inner)
        {
            Names = aggregateNames;
            Aggregates = aggregates;
        }

        public string[] Names { get; }

        public Type[] Types => Aggregates.Select(a => a.Type).ToArray();

        public IAggregate[] Aggregates { get; }

        public override void Accept(IVisitor visitor) => visitor.Visit(this);
    }
}
