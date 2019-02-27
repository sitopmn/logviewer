using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Aggregates
{
    /// <summary>
    /// An aggregate returning the least common input value
    /// </summary>
    internal class LeastAggregate : Aggregate<Dictionary<object, int>>
    {
        public LeastAggregate(Expression[] input) : base(input) { }
        protected override Dictionary<object, int> Initialize() => new Dictionary<object, int>();
        protected override Dictionary<object, int> Update(Dictionary<object, int> state, object[] input)
        {
            if (input[0] != null)
            {
                if (state.ContainsKey(input[0]))
                {
                    state[input[0]] += 1;
                }
                else
                {
                    state[input[0]] = 1;
                }
            }

            return state;
        }
        protected override Dictionary<object, int> Join(Dictionary<object, int> a, Dictionary<object, int> b)
        {
            foreach (var k in b)
            {
                if (a.ContainsKey(k.Key))
                {
                    a[k.Key] += k.Value;
                }
                else
                {
                    a[k.Key] = k.Value;
                }
            }

            return a;
        }
        protected override IEnumerable<object> Complete(Dictionary<object, int> state) => new object[] { state.Count > 0 ? state.Aggregate((a, b) => a.Value < b.Value ? a : b).Key : null };
    }
}
