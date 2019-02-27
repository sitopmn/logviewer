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
    /// A query node for projecting the results
    /// </summary>
    internal class ProjectNode : Node
    {
        public ProjectNode(Node inner, string[] names, Expression[] projections)
            : base(inner)
        {
            Names = names;
            ProjectionExpressions = projections;
            Projections = projections.Select(p => Expression.Lambda<Func<LogItem, object>>(Expression.Convert(p, typeof(object)), QueryFactory.ItemVariable).Compile()).ToArray();
            Types = projections.Select(p => p.Type).ToArray();
        }

        public string[] Names { get; }

        public Type[] Types { get; }
        
        public Expression[] ProjectionExpressions { get; }

        public Func<LogItem, object>[] Projections { get; }

        public override void Accept(IVisitor visitor) => visitor.Visit(this);
    }
}
