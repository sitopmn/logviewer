using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query.Interfaces
{
    /// <summary>
    /// Common interface for all aggregate implementations
    /// </summary>
    internal interface IAggregate
    {
        Type Type { get; }
        object Initialize();
        object Update(object state, LogItem item);
        object Join(object state, object other);
        IEnumerable<object> Complete(object state);
    }
    
    internal class AggregateMetadata
    {
        public AggregateMetadata(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
