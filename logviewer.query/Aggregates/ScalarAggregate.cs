using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Aggregates
{
    internal abstract class ScalarAggregate<TState> : Aggregate<TState>
    {
        protected ScalarAggregate(Expression[] input)
            : base(input)
        {
        }

        protected override TState Update(TState state, object[] input)
        {
            double converted;

            if (input.Length == 0 || input[0] == null)
            {
                return state;
            }

            if (input[0] is string text)
            {
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out converted))
                {
                    return Update(state, converted);
                }
                else
                {
                    return state;
                }
            }

            try
            {
                converted = Convert.ToDouble(input[0], CultureInfo.InvariantCulture);
                return Update(state, converted);
            }
            catch
            {
                return state;
            }
        }

        protected abstract TState Update(TState state, double input);
    }
}
