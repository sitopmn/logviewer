using logviewer.query.Nodes;
using logviewer.query.Types;
using System.Collections.Generic;
using System.Linq;

namespace logviewer.query
{
    internal static class NodeExtensions
    {
        public static MatchNode Matches(this MatchNode tree, string message)
        {
            if (tree is PhraseNode phrase && new Pattern(phrase.Phrase).IsMatch(message))
            {
                return tree;
            }
            else if (tree is ScanNode)
            {
                return tree;
            }
            else
            {
                var inner = tree.Inner.Cast<MatchNode>().Where(m => m.Matches(message) != null).ToList();
                if (inner.Count > 1)
                {
                    return tree;
                }
                else if (inner.Count == 1)
                {
                    return inner[0];
                }
            }

            return null;
        }

        public static Node FindParent(this Node tree, Node node)
        {
            var stack = new Stack<Node>();

            stack.Push(tree);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current.Inner.Contains(node))
                {
                    return current;
                }
                else
                {
                    foreach (var inner in current.Inner)
                    {
                        stack.Push(inner);
                    }
                }
            }

            return null;
        }

        public static TNode FindTopmost<TNode>(this Node tree) where TNode : Node
        {
            var stack = new Stack<Node>();

            stack.Push(tree);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is TNode target)
                {
                    return target;
                }
                else
                {
                    foreach (var inner in current.Inner)
                    {
                        stack.Push(inner);
                    }
                }
            }

            return null;
        }
    }
}
