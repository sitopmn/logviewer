using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Aggregates
{
    /// <summary>
    /// An aggregate returning the first input value
    /// </summary>
    internal class FirstAggregate : Aggregate<object>
    {
        public FirstAggregate(Expression[] input) : base(input) { }
        protected override object Initialize() => null;
        protected override object Update(object state, object[] input) => state != null ? state : input[0];
        protected override object Join(object a, object b) => a;
        protected override IEnumerable<object> Complete(object state) => new object[] { state };
    }
}
