using logviewer.Interfaces;
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
    /// Visitor for extracting the set of tokens required by the query
    /// </summary>
    internal class TokenVisitor : IVisitor
    {
        public List<HashSet<string>> Tokens { get; } = new List<HashSet<string>>();

        public void Visit(LimitNode node) => VisitInternal(node);
        public void Visit(OrderByNode node) => VisitInternal(node);
        public void Visit(ProjectNode node) => VisitInternal(node);
        public void Visit(AggregateNode node) => VisitInternal(node);
        public void Visit(GroupByNode node) => VisitInternal(node);
        public void Visit(ParseNode node) => VisitInternal(node);
        public void Visit(AndNode node) => VisitInternal(node);
        public void Visit(OrNode node) => VisitInternal(node);
        public void Visit(NotNode node) => VisitInternal(node);
        public void Visit(PredicateNode node) => VisitInternal(node);
        public void Visit(ScanNode node) => VisitInternal(node);

        public void Visit(PhraseNode node)
        {
            var set = new HashSet<string>();
            foreach (var token in node.Tokens.Where(t => t.Type != ETokenType.Item))
            {
                set.Add(token.Data);
            }

            if (set.Count > 0)
            {
                Tokens.Add(set);
            }
        }

        private void VisitInternal(Node node)
        {
            foreach (var inner in node.Inner) inner.Accept(this);
        }
    }
}
