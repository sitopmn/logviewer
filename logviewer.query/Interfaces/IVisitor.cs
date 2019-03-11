using logviewer.query.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Interfaces
{
    /// <summary>
    /// An interface for query tree visitors
    /// </summary>
    internal interface IVisitor
    {
        void Visit(LimitNode node);
        void Visit(OrderByNode node);
        void Visit(ProjectNode node);
        void Visit(AggregateNode node);
        void Visit(GroupByNode node);
        void Visit(AndNode node);
        void Visit(OrNode node);
        void Visit(NotNode node);
        void Visit(PredicateNode node);
        void Visit(PhraseNode node);
        void Visit(ScanNode node);
    }
}
