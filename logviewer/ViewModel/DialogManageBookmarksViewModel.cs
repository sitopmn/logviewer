using logviewer.Model;
using logviewer.core;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace logviewer.ViewModel
{
    /// <summary>
    /// ViewModel for the dialog for managing bookmarks
    /// </summary>
    public class DialogManageBookmarksViewModel : NotificationObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogManageBookmarksViewModel"/> class.
        /// </summary>
        /// <param name="localBookmarks">List of bookmarks currently loaded in the application</param>
        /// <param name="fileBookmarks">List of bookmarks stored in the bookmark file. This list is updated by the viewmodel</param>
        /// <param name="fileIsReadOnly">Indicates the file is not writeable</param>
        public DialogManageBookmarksViewModel(IList<SearchContext> localBookmarks, IList<SearchContext> fileBookmarks, bool fileIsReadOnly)
        {
            IsFileReadOnly = fileIsReadOnly;
            CopyFromFileCommand = new DelegateCommand<CompareBookmarkViewModel>(CopyFromFile);
            CopyToFileCommand = new DelegateCommand<CompareBookmarkViewModel>(CopyToFile);
            RemoveFromFileCommand = new DelegateCommand<CompareBookmarkViewModel>(RemoveFromFile);
            RemoveFromLocalCommand = new DelegateCommand<CompareBookmarkViewModel>(RemoveFromLocal);
            Bookmarks = BuildCompareList(localBookmarks, fileBookmarks);
        }

        /// <summary>
        /// Gets the bookmarks in local storage
        /// </summary>
        public IEnumerable<SearchContext> LocalBookmarks => Bookmarks.Where(b => b.IsPresentLocally).Select(b => b.LocalContext);

        /// <summary>
        /// Gets the bookmarks in file storage
        /// </summary>
        public IEnumerable<SearchContext> FileBookmarks => Bookmarks.Where(b => b.IsPresentInFile).Select(b => b.FileContext);
        
        /// <summary>
        /// Gets a value indicating whether the file can be written
        /// </summary>
        public bool IsFileReadOnly { get; }

        /// <summary>
        /// Gets a list of bookmarks to display
        /// </summary>
        public IList<CompareBookmarkViewModel> Bookmarks { get; }

        /// <summary>
        /// Gets a command to copy the bookmark from the file
        /// </summary>
        public ICommand CopyFromFileCommand { get; }

        /// <summary>
        /// Gets a command to copy the bookmark to the file
        /// </summary>
        public ICommand CopyToFileCommand { get; }

        /// <summary>
        /// Gets a command to remove the bookmark from the file
        /// </summary>
        public ICommand RemoveFromFileCommand { get; }

        /// <summary>
        /// Gets a command to remove the bookmark from the local storage
        /// </summary>
        public ICommand RemoveFromLocalCommand { get; }

        /// <summary>
        /// Removes the bookmark from the file
        /// </summary>
        /// <param name="bookmark">The bookmark to process</param>
        private void RemoveFromFile(CompareBookmarkViewModel bookmark)
        {
            bookmark.FileContext = null;
        }

        /// <summary>
        /// Removes the bookmark from the local storage
        /// </summary>
        /// <param name="bookmark">The bookmark to process</param>
        private void RemoveFromLocal(CompareBookmarkViewModel bookmark)
        {
            bookmark.LocalContext = null;
        }

        /// <summary>
        /// Copies the bookmark to the file
        /// </summary>
        /// <param name="bookmark">The bookmark to process</param>
        private void CopyToFile(CompareBookmarkViewModel bookmark)
        {
            bookmark.FileContext = bookmark.LocalContext;
        }

        /// <summary>
        /// Copies the bookmark from the file
        /// </summary>
        /// <param name="bookmark">The bookmark to process</param>
        private void CopyFromFile(CompareBookmarkViewModel bookmark)
        {
            bookmark.LocalContext = bookmark.FileContext;
        }

        /// <summary>
        /// Updates the bookmark list
        /// </summary>
        private IList<CompareBookmarkViewModel> BuildCompareList(IList<SearchContext> localBookmarks, IList<SearchContext> fileBookmarks)
        {
            if (IsFileReadOnly)
            {
                return fileBookmarks
                    .Select(f => new CompareBookmarkViewModel(localBookmarks.FirstOrDefault(l => f.Title.Equals(l.Title)), f))
                    .ToList();
            }
            else
            {
                return localBookmarks
                    .Select(l => new CompareBookmarkViewModel(l, fileBookmarks.FirstOrDefault(f => f.Title.Equals(l.Title))))
                    .Concat(fileBookmarks
                        .Where(f => !localBookmarks.Any(b => b.Title.Equals(f.Title)))
                        .Select(f => new CompareBookmarkViewModel(null, f)))
                    .ToList();
            }
        }
    }

    /// <summary>
    /// ViewModel representing the comparison of two bookmarks
    /// </summary>
    public class CompareBookmarkViewModel : NotificationObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompareBookmarkViewModel"/> class
        /// </summary>
        /// <param name="local">The bookmark loaded locally</param>
        /// <param name="file">The bookmark loaded from the file</param>
        public CompareBookmarkViewModel(SearchContext local, SearchContext file)
        {
            FileContext = file;
            LocalContext = local;
        }

        /// <summary>
        /// Gets the name of the bookmark
        /// </summary>
        public string Title => LocalContext != null ? LocalContext.Title : FileContext.Title;

        /// <summary>
        /// Gets a value indicating the item is present in the local storage
        /// </summary>
        public bool IsPresentLocally => LocalContext != null;

        /// <summary>
        /// Gets a value indicating the item is present in the file
        /// </summary>
        public bool IsPresentInFile => FileContext != null;

        /// <summary>
        /// Gets a value indicating the contents of both sides are different
        /// </summary>
        public bool IsContentDifferent => 
            LocalContext != null && 
            FileContext != null && 
            (
                !LocalContext.Query.Equals(FileContext.Query) || 
                LocalContext.VisualizationAxis != FileContext.VisualizationAxis || 
                LocalContext.VisualizationType != FileContext.VisualizationType || 
                !LocalContext.VisualizationSeries.SequenceEqual(FileContext.VisualizationSeries)
            );

        /// <summary>
        /// Gets the actual bookmark from the file
        /// </summary>
        public SearchContext FileContext
        {
            get => GetValue<SearchContext>();
            set => SetValue(value, () =>
            {
                RaisePropertyChanged(nameof(IsPresentInFile));
                RaisePropertyChanged(nameof(IsContentDifferent));
            });
        }

        /// <summary>
        /// Gets the actual bookmark from the local storage
        /// </summary>
        public SearchContext LocalContext
        {
            get => GetValue<SearchContext>();
            set => SetValue(value, () =>
            {
                RaisePropertyChanged(nameof(IsPresentLocally));
                RaisePropertyChanged(nameof(IsContentDifferent));
            });
        }
    }
}
