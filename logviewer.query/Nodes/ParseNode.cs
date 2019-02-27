using logviewer.query.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Nodes
{
    internal class ParseNode : Node
    {
        public ParseNode(Node inner, IParser parser, string field)
            : base(inner)
        {
            Parser = parser;
            Field = field;
        }

        public IParser Parser { get; private set; }

        public string Field { get; private set; }

        public override void Accept(IVisitor visitor) => visitor.Visit(this);

        public bool Parse(LogItem item)
        {
            var input = item.Fields[Field] as string;
            if (input != null)
            {
                Dictionary<string, object> fields;
                if (Parser.TryParse(item.Fields[Field] as string, out fields))
                {
                    item.Fields.Clear();
                    foreach (var kvp in fields)
                    {
                        item.Fields.Add(kvp.Key, kvp.Value);
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
