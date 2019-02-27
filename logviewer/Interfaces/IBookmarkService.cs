using logviewer.Model;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace logviewer.Interfaces
{
    /// <summary>
    /// Interface to the bookmark managing service
    /// </summary>
    public interface IBookmarkService : INotifyCollectionChanged
    {
        /// <summary>
        /// Gets a list of bookmarks
        /// </summary>
        IReadOnlyList<SearchContext> Bookmarks { get; }

        /// <summary>
        /// Gets or sets the path of the external bookmark repository file
        /// </summary>
        string RepositoryFile { get; set; }

        /// <summary>
        /// Saves the given query
        /// </summary>
        void Save(SearchContext original);

        /// <summary>
        /// Edits the given query
        /// </summary>
        /// <param name="model">The query to delete</param>
        void Edit(SearchContext model);

        /// <summary>
        /// Deletes the given query
        /// </summary>
        /// <param name="model">The query to delete</param>
        void Remove(SearchContext model);

        /// <summary>
        /// Imports/exports bookmarks from/to a file
        /// </summary>
        void Manage();
    }
}
