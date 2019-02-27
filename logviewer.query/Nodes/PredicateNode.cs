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
    /// A node constucting a filter with a predicate expression
    /// </summary>
    internal class PredicateNode : Node
    {
        public PredicateNode(Node inner, Expression predicate)
            : base(inner)
        {
            Predicate = predicate;
        }

        public Expression Predicate { get; }

        public override void Accept(IVisitor visitor) => visitor.Visit(this);
    }
}
