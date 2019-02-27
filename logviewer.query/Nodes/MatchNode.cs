using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Nodes
{
    /// <summary>
    /// A base node for match nodes
    /// </summary>
    internal abstract class MatchNode : Node
    {
        public abstract Expression Predicate();

        protected MatchNode(params MatchNode[] inner)
            : base(inner)
        { }

        protected MatchNode(IEnumerable<MatchNode> inner)
            : base(inner.ToArray())
        { }
    }
}
