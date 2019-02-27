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
    /// A visitor for formatting a query tree for output
    /// </summary>
    internal class FormatVisitor : IVisitor
    {
        private readonly StringBuilder _builder = new StringBuilder();

        private int _level = 0;

        public void Visit(LimitNode node) => Format(node, $"Count = {node.Count}");
        public void Visit(OrderByNode node) => Format(node, $"{string.Join(",", node.Fields)}");
        public void Visit(ProjectNode node) => Format(node, $"{string.Join(",", node.Names)}");
        public void Visit(AggregateNode node) => Format(node, $"{string.Join(",", node.Names)}");
        public void Visit(GroupByNode node) => Format(node, $"{string.Join(",", node.GroupNames)}|{string.Join(",", node.AggregateNames)}");
        public void Visit(ParseNode node) => Format(node, $"Parser = {node.Parser.GetType().Name}");
        public void Visit(AndNode node) => Format(node);
        public void Visit(OrNode node) => Format(node);
        public void Visit(NotNode node) => Format(node);
        public void Visit(PredicateNode node) => Format(node);
        public void Visit(PhraseNode node) => Format(node, $"Phrase = {node.Phrase}");
        public void Visit(ScanNode node) => Format(node);

        public override string ToString() => _builder.ToString();

        private void Format(Node node, string description)
        {
            _builder.AppendLine($"{new string(' ', _level * 2)} + {node.GetType().Name} [{description}]");
            _level += 1;
            foreach (var inner in node.Inner) inner.Accept(this);
            _level -= 1;
        }

        private void Format(Node node)
        {
            _builder.AppendLine($"{new string(' ', _level * 2)} + {node.GetType().Name}");
            _level += 1;
            foreach (var inner in node.Inner) inner.Accept(this);
            _level -= 1;
        }
    }
}
