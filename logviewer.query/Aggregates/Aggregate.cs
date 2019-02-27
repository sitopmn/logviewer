using logviewer.query.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Aggregates
{
    /// <summary>
    /// A base class for aggregates
    /// </summary>
    /// <typeparam name="TSTate">Type of the state variable of the aggregate</typeparam>
    internal abstract class Aggregate<TSTate> : IAggregate
    {
        private readonly Func<LogItem, object>[] _input;

        protected Aggregate(Expression[] input)
        {
            _input = input.Select(i => Expression.Lambda<Func<LogItem, object>>(Expression.Convert(i, typeof(object)), QueryFactory.ItemVariable).Compile()).ToArray();
            Type = input.First().Type;
        }

        public virtual Type Type { get; }

        object IAggregate.Initialize() => Initialize();
        object IAggregate.Update(object state, LogItem item) => Update((TSTate)state, _input.Select(i => i(item)).ToArray());
        object IAggregate.Join(object state, object other) => Join((TSTate)state, (TSTate)other);
        IEnumerable<object> IAggregate.Complete(object state) => Complete((TSTate)state);

        protected abstract TSTate Initialize();
        protected abstract TSTate Update(TSTate state, object[] input);
        protected abstract TSTate Join(TSTate a, TSTate b);
        protected abstract IEnumerable<object> Complete(TSTate state);
    }
}
