using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Aggregates
{
    /// <summary>
    /// An aggregate returning the last input value
    /// </summary>
    internal class LastAggregate : Aggregate<object>
    {
        public LastAggregate(Expression[] input) : base(input) { }
        protected override object Initialize() => null;
        protected override object Update(object state, object[] input) => input[0] != null ? input[0] : state;
        protected override object Join(object a, object b) => b;
        protected override IEnumerable<object> Complete(object state) => new object[] { state };
    }
}
