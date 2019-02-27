using logviewer.query.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Nodes
{
    /// <summary>
    /// A query node for ordering the results
    /// </summary>
    internal class OrderByNode : Node
    {
        public OrderByNode(Node inner, string[] field, bool[] descending)
            : base(inner)
        {
            Fields = field;
            Descending = descending;
        }

        public string[] Fields { get; }

        public bool[] Descending { get; }

        public override void Accept(IVisitor visitor) => visitor.Visit(this);
    }
}
