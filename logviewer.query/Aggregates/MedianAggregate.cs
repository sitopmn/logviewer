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
    /// An aggregate returning the median of all input values
    /// </summary>
    internal class MedianAggregate : ScalarAggregate<List<double>>
    {
        public MedianAggregate(Expression[] input) : base(input) { }
        public override Type Type => typeof(double);
        protected override List<double> Initialize() => new List<double>();
        protected override List<double> Update(List<double> state, double input) => state.Extend(input);
        protected override List<double> Join(List<double> a, List<double> b)
        {
            a.AddRange(b);
            return a;
        }
        protected override IEnumerable<object> Complete(List<double> state) => new object[] { state.OrderBy(i => i).Take(state.Count / 2).First() };
    }
}
