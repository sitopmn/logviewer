using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Aggregates
{
    /// <summary>
    /// An aggregate returning the mean of all input values
    /// </summary>
    internal class MeanAggregate : ScalarAggregate<Tuple<int, double>>
    {
        public MeanAggregate(Expression[] input) : base(input) { }
        public override Type Type => typeof(double);
        protected override Tuple<int, double> Initialize() => new Tuple<int, double>(0, 0);
        protected override Tuple<int, double> Update(Tuple<int, double> state, double input) => new Tuple<int, double>(state.Item1 + 1, state.Item2 + input);
        protected override Tuple<int, double> Join(Tuple<int, double> a, Tuple<int, double> b) => new Tuple<int, double>(a.Item1 + b.Item1, a.Item2 + b.Item2);
        protected override IEnumerable<object> Complete(Tuple<int, double> state) => new object[] { state.Item2 / state.Item1 };
    }
}
