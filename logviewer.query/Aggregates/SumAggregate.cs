using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Aggregates
{
    /// <summary>
    /// An aggregate returning the sum of all input values
    /// </summary>
    internal class SumAggregate : ScalarAggregate<double>
    {
        public SumAggregate(Expression[] input) : base(input) { }
        public override Type Type => typeof(double);
        protected override double Initialize() => 0;
        protected override double Update(double state, double input) => state + input;
        protected override double Join(double a, double b) => a + b;
        protected override IEnumerable<object> Complete(double state) => new object[] { state };
    }
}
