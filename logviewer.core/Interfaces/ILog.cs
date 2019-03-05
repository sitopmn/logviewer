using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading;

namespace logviewer.Interfaces
{
    /// <summary>
    /// Interface for providing access to a log
    /// </summary>
    public interface ILog : INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region properties

        /// <summary>
        /// Gets the number of log records
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the source files of the log
        /// </summary>
        string[] Files { get; }

        /// <summary>
        /// Gets a value indicating the log is updating
        /// </summary>
        bool IsUpdating { get; }

        #endregion

        #region public methods
        
        /// <summary>
        /// Updates the index for the log
        /// </summary>
        /// <returns>A bool value indicating whether the log was updated</returns>
        /// <param name="progress">Action to report indexing progress</param>
        /// <param name="cancellationToken">CancellationToken for cancelling the index update</param>
        bool Update(Action<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Queries the log
        /// </summary>
        /// <param name="query">The textual description of the query</param>
        /// <param name="progress">A callback for reporting progresss</param>
        /// <param name="cancellation">A <see cref="CancellationToken"/> for canceling the operation</param>
        /// <returns>In instance of <see cref="IQuery"/> representing the query</returns>
        IQuery Query(string query, Action<double> progress, CancellationToken cancellation);

        /// <summary>
        /// Reads the log
        /// </summary>
        /// <param name="progress">A callback for reporting progresss</param>
        /// <param name="cancellation">A <see cref="CancellationToken"/> for canceling the operation</param>
        /// <returns>Enumerable returning the log items</returns>
        IEnumerable<ILogItem> Read(Action<double> progress, CancellationToken cancellation);

        #endregion
    }
}
