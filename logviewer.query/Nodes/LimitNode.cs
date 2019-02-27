using logviewer.query.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Nodes
{
    /// <summary>
    /// A query node for limiting the number of results
    /// </summary>
    internal class LimitNode : Node
    {
        public LimitNode(Node inner, int count)
            : base(inner)
        {
            Count = count;
        }

        public int Count { get; }

        public override void Accept(IVisitor visitor) => visitor.Visit(this);
    }
}
