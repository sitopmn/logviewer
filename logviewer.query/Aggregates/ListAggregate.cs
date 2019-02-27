using logviewer.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Aggregates
{
    /// <summary>
    /// An aggregate returning a list of all input values
    /// </summary>
    internal class ListAggregate : Aggregate<List<object>>
    {
        public ListAggregate(Expression[] input) : base(input) { Expression = input[0]; }
        public Expression Expression { get; }
        protected override List<object> Initialize() => new List<object>();
        protected override List<object> Update(List<object> state, object[] input) => state.Extend(input[0]);
        protected override List<object> Join(List<object> a, List<object> b)
        {
            a.AddRange(b);
            return a;
        }
        protected override IEnumerable<object> Complete(List<object> state) => state;
    }
}
