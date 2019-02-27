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
    /// A node constucting an 'not' filter
    /// </summary>
    internal class NotNode : MatchNode
    {
        public NotNode(MatchNode inner)
            : base(inner)
        { }

        public override void Accept(IVisitor visitor) => visitor.Visit(this);

        public override Expression Predicate() => Expression.Not(((MatchNode)Inner[0]).Predicate());
    }
}
