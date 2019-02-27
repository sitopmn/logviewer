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
    /// An aggregate returning a distinct list of all input values
    /// </summary>
    internal class DistinctAggregate : Aggregate<HashSet<object>>
    {
        public DistinctAggregate(Expression[] input) : base(input) { }
        protected override HashSet<object> Initialize() => new HashSet<object>();
        protected override HashSet<object> Update(HashSet<object> state, object[] input) => state.Extend(input[0]);
        protected override HashSet<object> Join(HashSet<object> a, HashSet<object> b)
        {
            a.UnionWith(b);
            return a;
        }
        protected override IEnumerable<object> Complete(HashSet<object> state) => state;
    }
}
