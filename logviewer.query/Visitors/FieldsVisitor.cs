using logviewer.query.Interfaces;
using logviewer.query.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Visitors
{
    /// <summary>
    /// A visitor for extracting field names and types from the query tree
    /// </summary>
    internal class FieldsVisitor : IVisitor
    {
        private readonly IReadOnlyCollection<string> _fields;

        public IDictionary<string, Type> Fields { get; private set; }
        
        public FieldsVisitor(IReadOnlyCollection<string> fields)
        {
            _fields = fields;
        }

        public void Visit(LimitNode node)
        {
            foreach (var inner in node.Inner) inner.Accept(this);
        }

        public void Visit(OrderByNode node)
        {
            foreach (var inner in node.Inner) inner.Accept(this);
        }

        public void Visit(ProjectNode node)
        {
            Fields = node.Names.Select((n, i) => new { name = n, type = node.Types[i] }).ToDictionary(n => n.name, n => n.type);
        }

        public void Visit(AggregateNode node)
        {
            Fields = node.Names.Select((n, i) => new { name = n, type = node.Types[i] }).ToDictionary(n => n.name, n => n.type);
        }

        public void Visit(GroupByNode node)
        {
            Fields = node.GroupNames
                .Select((n, i) => new KeyValuePair<string, Type>(n, node.GroupTypes[i]))
                .Concat(node.AggregateNames.Select((n, i) => new KeyValuePair<string, Type>(n, node.AggregateTypes[i])))
                .ToDictionary(k => k.Key, k => k.Value);
        }
        
        public void Visit(AndNode node)
        {
            var set = new HashSet<KeyValuePair<string, Type>>();

            foreach (var inner in node.Inner)
            {
                inner.Accept(this);
                foreach (var field in Fields)
                {
                    set.Add(field);
                }
            }

            Fields = set.ToDictionary(f => f.Key, f => f.Value);
        }

        public void Visit(OrNode node)
        {
            var set = new HashSet<KeyValuePair<string, Type>>();

            foreach (var inner in node.Inner)
            {
                inner.Accept(this);
                foreach (var field in Fields)
                {
                    set.Add(field);
                }
            }

            Fields = set.ToDictionary(f => f.Key, f => f.Value);
        }

        public void Visit(NotNode node)
        {
            Fields = new Dictionary<string, Type>();
        }

        public void Visit(PredicateNode node)
        {
            node.Inner[0].Accept(this);
        }

        public void Visit(PhraseNode node)
        {
            if (node.Fields.Count == 0)
            {
                Fields = _fields.ToDictionary(f => f, f => typeof(string));
            }
            else
            {
                Fields = node.Fields;
            }
        }

        public void Visit(ScanNode node)
        {
            Fields = _fields.ToDictionary(f => f, f => typeof(string));
        }
    }
}
