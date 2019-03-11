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
    /// A node returning all log items
    /// </summary>
    internal class ScanNode : MatchNode
    {
        public override void Accept(IVisitor visitor) => visitor.Visit(this);

        public override Expression Predicate() => Expression.Constant(true);
    }
}
