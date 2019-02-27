using logviewer.query.Interfaces;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Nodes
{
    /// <summary>
    /// Abstract base class for query nodes
    /// </summary>
    internal abstract class Node
    {
        protected Node(params Node[] inner)
        {
            Inner = new List<Node>(inner);
        }

        public List<Node> Inner { get; private set; }

        public abstract void Accept(IVisitor visitor);

        public static MatchNode MakeBinary(ExpressionType type, MatchNode left, MatchNode right)
        {
            switch (type)
            {
                case ExpressionType.AndAlso: return new AndNode(left, right);
                case ExpressionType.OrElse: return new OrNode(left, right);
                default: throw new ParseException($"Undefined binary operator {type}");
            }
        }
    }
}
