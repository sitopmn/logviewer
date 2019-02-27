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
    /// A node constucting an 'and' filter
    /// </summary>
    internal class AndNode : MatchNode
    {
        public AndNode(params MatchNode[] inner)
            : base(inner.Where(i => i is AndNode).SelectMany(i => i.Inner.Cast<MatchNode>()).Union(inner.Where(i => !(i is AndNode))))
        { }

        public override void Accept(IVisitor visitor) => visitor.Visit(this);

        public override Expression Predicate()
        {
            var list = Inner.Cast<MatchNode>().Select(i => i.Predicate()).ToList();
            var expression = Expression.AndAlso(list.Skip(0).First(), list.Skip(1).First());
            foreach (var i in list.Skip(2)) expression = Expression.AndAlso(expression, i);
            return expression;
        }
    }
}
