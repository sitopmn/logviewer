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
    /// A node constucting an 'or' filter
    /// </summary>
    internal class OrNode : MatchNode
    {
        public OrNode(params MatchNode[] inner)
            : base(inner.Where(i => i is OrNode).SelectMany(i => i.Inner.Cast<MatchNode>()).Union(inner.Where(i => !(i is OrNode))))
        { }

        public override void Accept(IVisitor visitor) => visitor.Visit(this);

        public override Expression Predicate()
        {
            var list = Inner.Cast<MatchNode>().Select(i => i.Predicate()).ToList();
            var expression = Expression.OrElse(list.Skip(0).First(), list.Skip(1).First());
            foreach (var i in list.Skip(2)) expression = Expression.OrElse(expression, i);
            return expression;
        }
    }
}
