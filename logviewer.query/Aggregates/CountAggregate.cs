using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Aggregates
{
    /// <summary>
    /// An aggregate returning the number of input values
    /// </summary>
    internal class CountAggregate : Aggregate<int>
    {
        public CountAggregate(Expression[] input) : base(input) { }
        public override Type Type => typeof(int);
        protected override int Initialize() => 0;
        protected override int Update(int state, object[] input)
        {
            if (base.Type == typeof(bool))
            {
                if (true.Equals(input[0]))
                {
                    return state + 1;
                }
                else
                {
                    return state;
                }
            }
            else if (input[0] != null)
            {
                return state + 1;
            }
            else
            {
                return state;
            }
        }
        protected override int Join(int a, int b) => a + b;
        protected override IEnumerable<object> Complete(int state) => new object[] { state };
    }
}
