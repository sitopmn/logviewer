using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Aggregates
{
    /// <summary>
    /// An aggregate returning the maximum input value
    /// </summary>
    internal class MaxAggregate : ScalarAggregate<double?>
    {
        public MaxAggregate(Expression[] input) : base(input) { }
        public override Type Type => typeof(double);
        protected override double? Initialize() => null;
        protected override double? Update(double? state, double input) => Math.Max(state.HasValue ? state.Value : double.MinValue, input);
        protected override double? Join(double? a, double? b) => a.HasValue && b.HasValue ? Math.Max(a.Value, b.Value) : (a.HasValue ? a : (b.HasValue ? b : null));
        protected override IEnumerable<object> Complete(double? state) => new object[] { state.HasValue ? (object)state.Value : null };
    }
}
