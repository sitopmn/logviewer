using log4net;
using logviewer.Interfaces;
using logviewer.Model;
using logviewer.core;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using logviewer.core.Interfaces;

namespace logviewer.ViewModel
{
    /// <summary>
    /// Main view model of the application
    /// </summary>
    [Export]
    public class MainViewModel : NotificationObject
    {
        /// <summary>
        /// The logger for log messages
        /// </summary>
        private readonly log4net.ILog _logger = LogManager.GetLogger(typeof(MainViewModel));

        /// <summary>
        /// The currently loaded log
        /// </summary>
        private readonly ILogService _logService;

        /// <summary>
        /// Service managing the bookmarks
        /// </summary>
        private readonly IBookmarkService _bookmarkService;

        /// <summary>
        /// Dictionary containing factories for instantiating pages from context types
        /// </summary>
        private readonly Dictionary<Type, ExportFactory<IPageViewModel>> _pageFactories;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        [ImportingConstructor]
        public MainViewModel(ISnackbarMessageQueue messageQueue, ILogService logService, IBookmarkService bookmarkService, [ImportMany] IEnumerable<ExportFactory<IPageViewModel, IPageViewModelMetadata>> viewModels, [ImportMany] IEnumerable<Lazy<IPageView, IPageViewMetadata>> views, DialogHelpViewModel helpViewModel)
        {
            _logService = logService;
            _pageFactories = viewModels.ToDictionary(vm => vm.Metadata.Context, vm => (ExportFactory<IPageViewModel>)vm);
            _bookmarkService = bookmarkService;
            
            MessageQueue = messageQueue;
            Pages = new ObservableCollection<Model.Context>(views.Where(p => p.Metadata.Context != null).OrderBy(p => p.Metadata.Index).Select(p => (Model.Context)Activator.CreateInstance(p.Metadata.Context)));
            Bookmarks = new ObservableCollection<Context>(_bookmarkService.Bookmarks);
            Tabs = new ObservableCollection<IPageViewModel>();
            UseDarkColorScheme = Properties.Settings.Default.DarkColorScheme;
            OpenInNewTab = Properties.Settings.Default.OpenInNewTab;
            OpenTabCommand = new DelegateCommand<Context>(CreateTab);
            ReplaceTabCommand = new DelegateCommand<Context>(ReplaceTab);
            CloseTabCommand = new DelegateCommand<PageViewModel>(CloseTab, t => Tabs.Count > 1);
            OpenCommand = new DelegateCommand<string[]>(Open);
            EditCommand = new DelegateCommand<SearchContext>(_bookmarkService.Edit, c => !c.IsFromRepository);
            DeleteCommand = new DelegateCommand<SearchContext>(_bookmarkService.Remove, c => !c.IsFromRepository);
            SaveCommand = new DelegateCommand(() => _bookmarkService.Save((SearchContext)CurrentTab.Context), () => CurrentTab is IPageViewModel page && page.Context is SearchContext);
            ManageBookmarksCommand = new DelegateCommand(_bookmarkService.Manage);
            SelectBookmarkRepositoryCommand = new DelegateCommand(SelectBookmarkRepository);
            ClearBookmarkRepositoryCommand = new DelegateCommand(ClearBookmarkRepository, () => !string.IsNullOrEmpty(Properties.Settings.Default.BookmarkRepositoryFile));
            ShowHelpCommand = new DelegateCommand(() => DialogHost.Show(helpViewModel));

            // update the bookmark list when the service updates
            _bookmarkService.CollectionChanged += (s, e) =>
            {
                Bookmarks.Clear();
                Bookmarks.AddRange(_bookmarkService.Bookmarks);
            };

            // create the initial tab
            CreateTab((Context)Pages.First().Clone());
        }

        #region tabs

        /// <summary>
        /// Gets or sets the current tab selected by the user
        /// </summary>
        public IPageViewModel CurrentTab
        {
            get => GetValue<IPageViewModel>();
            set => SetValue(value, async () =>
            {
                if (CurrentTab != null)
                {
                    await CurrentTab.Update();
                }
            });
        }

        /// <summary>
        /// Gets a list of current tabs
        /// </summary>
        public ObservableCollection<IPageViewModel> Tabs
        {
            get => GetValue<ObservableCollection<IPageViewModel>>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets a command to open a new tab
        /// </summary>
        public ICommand OpenTabCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a command to replace a tab with new content
        /// </summary>
        public ICommand ReplaceTabCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a command to close a tab
        /// </summary>
        public ICommand CloseTabCommand
        {
            get;
            private set;
        }

        #endregion

        #region menu

        /// <summary>
        /// Gets the list of navigation items
        /// </summary>
        public ObservableCollection<Context> Pages
        {
            get => GetValue<ObservableCollection<Model.Context>>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets the list of saved views
        /// </summary>
        public ObservableCollection<Context> Bookmarks
        {
            get => GetValue<ObservableCollection<Model.Context>>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets a value which indicates whether a dark or light color scheme is used
        /// </summary>
        public bool UseDarkColorScheme
        {
            get => GetValue<bool>();
            set => SetValue(value, v =>
            {
                if (v != Properties.Settings.Default.DarkColorScheme)
                {
                    Properties.Settings.Default.DarkColorScheme = v;
                    Properties.Settings.Default.Save();
                }

                new PaletteHelper().SetLightDark(v);
            });
        }

        /// <summary>
        /// Gets or sets a value indicating menu items should be opened in a new tab
        /// </summary>
        public bool OpenInNewTab
        {
            get => GetValue<bool>();
            set => SetValue(value, v =>
            {
                if (v != Properties.Settings.Default.OpenInNewTab)
                {
                    Properties.Settings.Default.OpenInNewTab = v;
                    Properties.Settings.Default.Save();
                }
            });
        }

        /// <summary>
        /// Gets the bookmark repository file
        /// </summary>
        public string BookmarkRepositoryFile => _bookmarkService.RepositoryFile;

        /// <summary>
        /// Gets a value indicating a log is opened
        /// </summary>
        public bool IsLogOpened => _logService.Log != null;
        
        /// <summary>
        /// Gets the command to delete a query
        /// </summary>
        public ICommand DeleteCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to delete a query
        /// </summary>
        public ICommand EditCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a command to save a query
        /// </summary>
        public ICommand SaveCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to manage bookmarks
        /// </summary>
        public ICommand ManageBookmarksCommand
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Gets the command to select the remote bookmark file
        /// </summary>
        public ICommand SelectBookmarkRepositoryCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to clear the bookmark repository
        /// </summary>
        public ICommand ClearBookmarkRepositoryCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to show the help dialog
        /// </summary>
        public ICommand ShowHelpCommand
        {
            get;
            private set;
        }

        #endregion

        #region progress

        /// <summary>
        /// Gets a value indicating an update is in progress
        /// </summary>
        public bool IsUpdating
        {
            get => GetValue<bool>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets a value indicating an update is in progress with progress information
        /// </summary>
        public bool IsReportingProgress
        {
            get => GetValue<bool>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets the update progress
        /// </summary>
        public double UpdateProgress
        {
            get => GetValue<double>();
            private set => SetValue(value);
        }

        #endregion

        /// <summary>
        /// Gets the message queue used for displaying snackbar messages
        /// </summary>
        public ISnackbarMessageQueue MessageQueue
        {
            get => GetValue<ISnackbarMessageQueue>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets the command to open a log
        /// </summary>
        public ICommand OpenCommand
        {
            get;
            private set;
        }

        #region private methods

        /// <summary>
        /// Replaces the current tab with new content
        /// </summary>
        /// <param name="context">Context to create a tab for</param>
        private void ReplaceTab(Context context)
        {
            if (context == null)
            {
                context = new SearchContext() { Title = Properties.Resources.Menu_Search };
            }
            else if (OpenInNewTab)
            {
                CreateTab(context);
                return;
            }

            if (_pageFactories.ContainsKey(context.GetType()))
            {
                var index = Tabs.IndexOf(CurrentTab);
                var page = _pageFactories[context.GetType()].CreateExport().Value;
                page.Progress += ProgressHandler;
                page.Context = (Context)context.Clone();
                Tabs[index].Dispose();
                Tabs[index] = page;
                CurrentTab = page;
            }

            ((DelegateCommand<PageViewModel>)CloseTabCommand).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Creates and selects a tab for the given context
        /// </summary>
        /// <param name="context">Context to create a tab for</param>
        private void CreateTab(Context context)
        {
            if (context == null)
            {
                context = new SearchContext() { Title = Properties.Resources.Menu_Search };
            }

            if (_pageFactories.ContainsKey(context.GetType()))
            {
                var page = _pageFactories[context.GetType()].CreateExport().Value;
                page.Progress += ProgressHandler;
                page.Context = (Context)context.Clone();
                Tabs.Add(page);
                CurrentTab = page;
            }

            ((DelegateCommand<PageViewModel>)CloseTabCommand).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Closes a tab
        /// </summary>
        /// <param name="tab">Tab to close</param>
        private void CloseTab(PageViewModel tab)
        {
            Tabs.Remove(tab);
            tab.Progress -= ProgressHandler;
            tab.Dispose();
            ((DelegateCommand<PageViewModel>)CloseTabCommand).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Opens a log
        /// </summary>
        /// <param name="source">The list of file to read</param>
        private async void Open(string[] source)
        {
            // show the file select dialog if no source files are given
            if (source == null || source.Length == 0)
            {
                var dlg = new OpenFileDialog();
                dlg.CheckFileExists = true;
                dlg.DefaultExt = "";
                dlg.Filter = "Any File *.*|*.*";
                dlg.Title = Properties.Resources.Menu_OpenLogFiles;
                dlg.Multiselect = true;
                if (dlg.ShowDialog() == false)
                {
                    return;
                }

                source = dlg.FileNames;
            }
            
            // index the log
            await Task.Run(() => _logService.Load(source, p => Invoke(p2 => ProgressHandler(this, new ProgressEventArgs(p2)), p), CancellationToken.None));
            RaisePropertyChanged(nameof(IsLogOpened));

            // reset the progress information
            ProgressHandler(this, ProgressEventArgs.End);

            // update the current view
            if (CurrentTab is IPageViewModel page)
            {
                await page.Update();
            }
        }

        /// <summary>
        /// Selects a bookmark repository file
        /// </summary>
        private void SelectBookmarkRepository()
        {
            var dlg = new OpenFileDialog();
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Bookmark File (*.xml)|*.xml";
            dlg.Title = "Select Bookmark Repository";
            dlg.FileName = Properties.Settings.Default.BookmarkRepositoryFile;
            if (dlg.ShowDialog() == true)
            {
                _bookmarkService.RepositoryFile = dlg.FileName;
                ((DelegateCommand)ClearBookmarkRepositoryCommand).RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(BookmarkRepositoryFile));
            }
        }

        /// <summary>
        /// Clears the bookmark repository file
        /// </summary>
        private void ClearBookmarkRepository()
        {
            _bookmarkService.RepositoryFile = string.Empty;
            ((DelegateCommand)ClearBookmarkRepositoryCommand).RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(BookmarkRepositoryFile));
        }

        /// <summary>
        /// Handles progress reports
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Argument of the event</param>
        private void ProgressHandler(object sender, ProgressEventArgs e)
        {
            Invoke(DispatcherPriority.Normal, () =>
            {
                IsUpdating = e.IsActive;
                IsReportingProgress = !e.IsIndeterminate;
                UpdateProgress = e.Progress;
            });
        }

        #endregion
    }
}
