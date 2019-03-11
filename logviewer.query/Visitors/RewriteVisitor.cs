using logviewer.query.Interfaces;
using logviewer.query.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Visitors
{
    internal class RewriteVisitor : IVisitor
    {
        public Node Tree { get; private set; }

        public void Visit(LimitNode node)
        {
            node.Inner[0].Accept(this);
            Tree = new LimitNode(Tree, node.Count);
        }

        public void Visit(OrderByNode node)
        {
            node.Inner[0].Accept(this);
            Tree = new OrderByNode(Tree, node.Fields, node.Descending);
        }

        public void Visit(ProjectNode node)
        {
            node.Inner[0].Accept(this);
            Tree = new ProjectNode(Tree, node.Names, node.ProjectionExpressions);
        }

        public void Visit(AggregateNode node)
        {
            node.Inner[0].Accept(this);
            Tree = new AggregateNode(Tree, node.Names, node.Aggregates);
        }

        public void Visit(GroupByNode node)
        {
            node.Inner[0].Accept(this);
            Tree = new GroupByNode(Tree, node.GroupNames, node.GroupExpressions, node.AggregateNames, node.Aggregates);
        }

        public void Visit(AndNode node)
        {
            var nodes = new List<MatchNode>();
            foreach (var inner in node.Inner)
            {
                inner.Accept(this);
                if (Tree is AndNode innerAnd)
                {
                    // a and (b and c) -> a and b and c
                    nodes.AddRange(Tree.Inner.Cast<MatchNode>());
                }
                else if (Tree is NotNode innerNot && innerNot.Inner[0] is OrNode innerOr)
                {
                    // a and not (b or c or d) -> a and not b and not c and not d
                    nodes.AddRange(innerOr.Inner.Cast<MatchNode>().Select(n => new NotNode(n)));
                }
                else
                {
                    nodes.Add((MatchNode)Tree);
                }
            }

            Tree = new AndNode(nodes.ToArray());
        }

        public void Visit(OrNode node)
        {
            var nodes = new List<MatchNode>();
            foreach (var inner in node.Inner)
            {
                inner.Accept(this);
                if (Tree is OrNode innerOr)
                {
                    // a or (b or c) -> a or b or c
                    nodes.AddRange(innerOr.Inner.Cast<MatchNode>());
                }
                else
                {
                    nodes.Add((MatchNode)Tree);
                }
            }

            Tree = new OrNode(nodes.ToArray());
        }

        public void Visit(NotNode node)
        {
            node.Inner[0].Accept(this);
            Tree = new NotNode((MatchNode)Tree);
        }

        public void Visit(PredicateNode node)
        {
            node.Inner[0].Accept(this);
            Tree = new PredicateNode(Tree, node.Predicate);
        }

        public void Visit(PhraseNode node)
        {
            Tree = node;
        }

        public void Visit(ScanNode node)
        {
            Tree = node;
        }
    }
}
