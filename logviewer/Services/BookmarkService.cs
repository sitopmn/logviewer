using logviewer.Interfaces;
using logviewer.Model;
using logviewer.ViewModel;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace logviewer.Services
{
    /// <summary>
    /// Service for managing bookmarked searches
    /// </summary>
    [Export(typeof(IBookmarkService))]
    internal class BookmarkService : IBookmarkService
    {
        /// <summary>
        /// List of bookmarks
        /// </summary>
        private readonly List<SearchContext> _bookmarks = new List<SearchContext>();

        /// <summary>
        /// Initializes a new instance of the <see cref="BookmarkService"/> class
        /// </summary>
        public BookmarkService()
        {
            if (Properties.Settings.Default.Bookmarks == null)
            {
                Properties.Settings.Default.Bookmarks = new BookmarkList();
            }

            UpdateBookmarks();
        }

        /// <summary>
        /// Notifies changes to the bookmark collection
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Gets a list of bookmarks
        /// </summary>
        public IReadOnlyList<SearchContext> Bookmarks => _bookmarks;

        /// <summary>
        /// Gets or sets the path of the external bookmark repository file
        /// </summary>
        public string RepositoryFile
        {
            get
            {
                return Properties.Settings.Default.BookmarkRepositoryFile;
            }

            set
            {
                Properties.Settings.Default.BookmarkRepositoryFile = value;
                Properties.Settings.Default.Save();
                UpdateBookmarks();
            }
        }

        /// <summary>
        /// Saves the given query
        /// </summary>
        public async void Save(SearchContext original)
        {
            var model = (SearchContext)original.Clone();
            model.Executor = null;
            var viewmodel = new DialogSaveViewModel(model.Title, Properties.Settings.Default.Bookmarks.Select(v => v.Title).ToList());
            if ((bool)await DialogHost.Show(viewmodel))
            {
                model.Title = viewmodel.Title;
                var existing = Properties.Settings.Default.Bookmarks.FirstOrDefault(q => q.Title == model.Title);
                if (existing != null)
                {
                    var index = Properties.Settings.Default.Bookmarks.IndexOf(existing);
                    Properties.Settings.Default.Bookmarks.RemoveAt(index);
                    Properties.Settings.Default.Bookmarks.Insert(index, model);
                }
                else
                {
                    Properties.Settings.Default.Bookmarks.Add(model);
                }

                Properties.Settings.Default.Save();
                UpdateBookmarks();
            }
        }

        /// <summary>
        /// Edits the given query
        /// </summary>
        /// <param name="model">The query to delete</param>
        public async void Edit(SearchContext model)
        {
            var viewmodel = new DialogSaveViewModel(model.Title, Properties.Settings.Default.Bookmarks.Select(v => v.Title).ToList());
            if ((bool)await DialogHost.Show(viewmodel))
            {
                model.Title = viewmodel.Title;
                Properties.Settings.Default.Save();
                UpdateBookmarks();
            }
        }

        /// <summary>
        /// Deletes the given query
        /// </summary>
        /// <param name="model">The query to delete</param>
        public async void Remove(SearchContext model)
        {
            if ((bool)await DialogHost.Show(new DialogDeleteViewModel()))
            {
                Properties.Settings.Default.Bookmarks.Remove(model);
                Properties.Settings.Default.Save();
                UpdateBookmarks();
            }
        }

        /// <summary>
        /// Import/Export bookmarks to or from a file
        /// </summary>
        public async void Manage()
        {
            var dlg = new OpenFileDialog();
            dlg.DefaultExt = ".xml";
            dlg.CheckFileExists = false;
            dlg.FileName = "bookmarks.xml";
            dlg.Filter = "XML Bookmark File (*.xml)|*.xml";
            dlg.Title = "Select or create a bookmark file";
            if (dlg.ShowDialog() == true)
            {
                var serializer = new XmlSerializer(typeof(SearchContext[]));

                // read the contents of the bookmark file. the list will be empty if the file could not be read successfully.
                var isReadOnly = File.Exists(dlg.FileName) && File.GetAttributes(dlg.FileName).HasFlag(FileAttributes.ReadOnly);
                var localBookmarks = Properties.Settings.Default.Bookmarks.ToList();
                var fileBookmarks = ReadBookmarkFile(dlg.FileName).ToList();

                // show the manage dialog
                var viewmodel = new DialogManageBookmarksViewModel(localBookmarks, fileBookmarks, isReadOnly);
                if ((bool)await DialogHost.Show(viewmodel))
                {
                    if (!isReadOnly)
                    {
                        WriteBookmarkFile(dlg.FileName, viewmodel.FileBookmarks);
                    }

                    Properties.Settings.Default.Bookmarks.Clear();
                    Properties.Settings.Default.Bookmarks.AddRange(viewmodel.LocalBookmarks);
                    Properties.Settings.Default.Save();
                    UpdateBookmarks();
                }
            }
        }
        
        /// <summary>
        /// Updates the bookmark list
        /// </summary>
        private void UpdateBookmarks()
        {
            var bookmarks = Properties.Settings.Default.Bookmarks
                .Concat(ReadBookmarkFile(RepositoryFile)
                    .Where(b => !Properties.Settings.Default.Bookmarks.Any(l => l.Title == b.Title))
                    .Select(b => { b.IsFromRepository = true; return b; }))
                .OrderBy(b => b.Title);

            _bookmarks.Clear();
            _bookmarks.AddRange(bookmarks);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        
        /// <summary>
        /// Fetches bookmarks from the given file
        /// </summary>
        /// <param name="file">File to read from</param>
        /// <returns>Enumerable returning loaded remote bookmarks. In case the file could not be read, the enumerable is empty</returns>
        private IEnumerable<SearchContext> ReadBookmarkFile(string file)
        {
            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                var serializer = new XmlSerializer(typeof(SearchContext[]));
                using (var reader = new StreamReader(file))
                {
                    try
                    {
                        return serializer.Deserialize(reader) as SearchContext[];
                    }
                    finally
                    {
                    }
                }
            }

            return Enumerable.Empty<SearchContext>();
        }

        /// <summary>
        /// Saves bookmarks to the given file
        /// </summary>
        /// <param name="file">File to save to</param>
        /// <param name="bookmarks">List of bookmarks to save to the file</param>
        private void WriteBookmarkFile(string file, IEnumerable<SearchContext> bookmarks)
        {
            var serializer = new XmlSerializer(typeof(SearchContext[]));
            using (var writer = new StreamWriter(file))
            {
                try
                {
                    serializer.Serialize(writer, bookmarks.ToArray());
                }
                finally
                { }
            }
        }
    }
}
