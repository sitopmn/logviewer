using logviewer.charts;
using logviewer.Interfaces;
using logviewer.Model;
using logviewer.ViewModel;
using logviewer.core;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.ComponentModel.Composition;
using log4net;
using System.Windows;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace logviewer.ViewModel
{
    /// <summary>
    /// Base class for view models which query log data
    /// </summary>
    [ExportPageViewModel(Context = typeof(SearchContext))]
    public class SearchViewModel : PageViewModel
    {
        #region fields

        /// <summary>
        /// Logger for the class
        /// </summary>
        private readonly log4net.ILog _logger = LogManager.GetLogger(typeof(SearchViewModel));

        /// <summary>
        /// Queue for past searches
        /// </summary>
        private readonly Stack<SearchContext> _past = new Stack<SearchContext>();

        /// <summary>
        /// Queue for future searches
        /// </summary>
        private readonly Stack<SearchContext> _future = new Stack<SearchContext>();

        /// <summary>
        /// The message queue uesed to display snackbar messages
        /// </summary>
        private readonly ISnackbarMessageQueue _messageQueue;
       
        /// <summary>
        /// The log providing data
        /// </summary>
        private readonly Interfaces.ILog _log;
        
        /// <summary>
        /// The cancellation token to cancel running updates
        /// </summary>
        private CancellationTokenSource _updateToken;
        
        #endregion

        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchViewModel"/> class.
        /// </summary>
        /// <param name="messages">The message queue uesed to display snackbar messages</param>
        /// <param name="log">The log providing data</param>
        /// <param name="queryAssistant">Assistant for editing queries</param>
        [ImportingConstructor]
        public SearchViewModel(ISnackbarMessageQueue messages, Interfaces.ILog log)
        {
            _messageQueue = messages;
            _log = log;
            _log.Loaded += (s, e) => Invoke(async () =>
            {
                InvalidateQuery();
                await Update();
            });
            
            Rows = new ILogItem[0];
            Columns = new ObservableCollection<ColumnData>();
            Chart = new ChartViewModel();
            Chart.Updated += (s, e) => StoreVisualizationsToModel();

            NavigateBackwardCommand = new DelegateCommand(NavigateBackward, () => _past.Count > 0);
            NavigateForwardCommand = new DelegateCommand(NavigateForward, () => _future.Count > 0);
            UpdateCommand = new DelegateCommand(UpdateCommandExecute);
            CopyDataCommand = new DelegateCommand<ILogItem>(CopyData);
            SaveDataCommand = new DelegateCommand(SaveDataCommandExecute, SaveDataCommandCanExecute);
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the query entered by the user
        /// </summary>
        public string UserQuery
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets the columns to display
        /// </summary>
        public ObservableCollection<ColumnData> Columns
        {
            get => GetValue<ObservableCollection<ColumnData>>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets the chart data
        /// </summary>
        public ChartViewModel Chart
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the log entries
        /// </summary>
        public IList Rows
        {
            get => GetValue<IList>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets the vertical scroll offset
        /// </summary>
        public double VerticalScrollOffset
        {
            get => GetValue<double>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets a value indicating an cancealable update is active
        /// </summary>
        public bool IsUpdating
        {
            get => GetValue<bool>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets the command to run the query
        /// </summary>
        public ICommand UpdateCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to export the generated data to CSV format
        /// </summary>
        public ICommand SaveDataCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to copy the selected data into the clipboard
        /// </summary>
        public ICommand CopyDataCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to navigate backward in the history
        /// </summary>
        public ICommand NavigateBackwardCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to navigate forward in the history
        /// </summary>
        public ICommand NavigateForwardCommand
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Gets the model as a <see cref="Search"/> instance
        /// </summary>
        private SearchContext Search => (SearchContext)Context;

        #endregion

        #region public methods

        /// <summary>
        /// Updates the query data
        /// </summary>
        /// <returns>A task indicating completion of the update</returns>
        public async override Task Update()
        {
            // avoid running multiple updates...
            if (_updateToken != null)
            {
                return;
            }

            // if the log is updating everything we would do is for nothing...
            if (_log.IsUpdating)
            {
                return;
            }

            // create a new update token
            var newToken = new CancellationTokenSource();
            _updateToken = newToken;
            IsUpdating = true;

            // show the progress bar
            StartProgress();

            // update the log if required
            _log.Update(UpdateProgress, newToken.Token);

            // add a history entry to allow the user to return to earlier queries
            if (UserQuery != Search.Query)
            {
                _past.Push((SearchContext)Search.Clone());
                _future.Clear();
                UpdateNavigationState();
                InvalidateQuery();
            }

            // update the model with the actual state
            Search.Query = UserQuery;

            // clear the display
            Columns.Clear();
            Rows = new ILogItem[0];

            // parse the query
            IQuery query = null;
            if (Search.Executor == null)
            {
                try
                {
                    InvalidateQuery();
                    query = await Task.Run(() => _log.Query(Search.Query, UpdateProgress, newToken.Token));
                    Search.Executor = query;
                }
                catch (OperationCanceledException)
                {
                    EndProgress();
                    IsUpdating = false;
                    _updateToken = null;
                    return;
                }
                catch (TargetInvocationException tie)
                {
                    EndProgress();
                    _updateToken = null;
                    _messageQueue.Enqueue(tie.InnerException.Message);
                    _logger.Error($"An error occurred while updating: {tie.InnerException.Message}");
                    return;
                }
                catch (AggregateException ae)
                {
                    EndProgress();
                    IsUpdating = false;
                    _updateToken = null;
                    foreach (var e in ae.InnerExceptions)
                    {
                        _messageQueue.Enqueue(e.Message);
                        _logger.Error($"An error occurred while updating: {e.Message}\n{e.StackTrace}");
                    }
                    return;
                }
                catch (Exception e)
                {
                    EndProgress();
                    IsUpdating = false;
                    _updateToken = null;
                    _messageQueue.Enqueue(e.Message);
                    _logger.Error($"An error occurred while updating: {e.Message}\n{e.StackTrace}");
                    return;
                }
            }
            else
            {
                query = Search.Executor;
            }

            // populate the columns from the query
            Columns.AddRange(query.Columns.ToColumnData());
            RaisePropertyChanged(nameof(Columns));

            // populate the results
            var wrapped = new ListWrapper<ILogItem>(query);
            Rows = wrapped;
            Chart.Update(Columns, wrapped, Rows.Count, Search.VisualizationType, Search.VisualizationAxis, Search.VisualizationSeries);

            // store the visualizations to the model
            StoreVisualizationsToModel();
            
            // signal the process as finished
            EndProgress();
            IsUpdating = false;

            // release the update token
            if (_updateToken == newToken)
            {
                _updateToken = null;
            }

            // update commands
            ((DelegateCommand)SaveDataCommand).RaiseCanExecuteChanged();

            // release memory
            GC.Collect();
        }

        /// <summary>
        /// Gets a list of log items around the given item
        /// </summary>
        /// <param name="item">The record to show</param>
        public IList<ILogItem> GetDetailList(ILogItem item)
        {
            var fullLog = _log.Query(string.Empty, null, CancellationToken.None);
            var index = fullLog.IndexOf(item);
            if (index >= 0)
            {
                var padding = new ILogItem[0];
                if (index < 5)
                {
                    padding = Enumerable.Range(0, 5 - index).Select<int, ILogItem>(i => null).ToArray();
                }

                return padding.Concat(fullLog.Skip(Math.Max(0, index - 5)).Take(11 - padding.Length)).ToList();
            }
            else
            {
                return new List<ILogItem>();
            }
        }

        /// <summary>
        /// Gets the index of the given log item in the unfiltered log
        /// </summary>
        /// <param name="item">The item to get the index of</param>
        /// <returns>The index of the item or -1</returns>
        public int GetDetailIndex(ILogItem item)
        {
            var fullLog = _log.Query(string.Empty, null, CancellationToken.None);
            return fullLog.IndexOf(item);
        }

        /// <summary>
        /// Releases resources held by the view model
        /// </summary>
        public override void Dispose()
        {
            InvalidateQuery();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Update the user entered query when the model changes
        /// </summary>
        /// <param name="oldContext">The old context</param>
        /// <param name="newContext">The new context</param>
        public override async void OnModelUpdated(Context oldContext, Context newContext)
        {
            UserQuery = Search.Query;
            await Update();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Executes the command to update the query results
        /// </summary>
        private async void UpdateCommandExecute()
        {
            // cancel any running query
            if (_updateToken != null)
            {
                _updateToken.Cancel();
                return;
            }
            else
            {
                // invalidate the old query
                InvalidateQuery();
                await Update();
            }
        }

        /// <summary>
        /// Removes a stored query
        /// </summary>
        private void InvalidateQuery()
        {
            if (Search.Executor != null)
            {
                Search.Executor = null;
            }
        }

        /// <summary>
        /// Navigates forward in the navigation history
        /// </summary>
        private async void NavigateForward()
        {
            _past.Push((SearchContext)Search.Clone());
            Context = _future.Pop();
            UpdateNavigationState();
            await Update();
        }

        /// <summary>
        /// Navigates backward in the navigation history
        /// </summary>
        private async void NavigateBackward()
        {
            _future.Push((SearchContext)Search.Clone());
            Context = _past.Pop();
            UpdateNavigationState();
            await Update();
        }

        /// <summary>
        /// Updates the state of the navigation commands
        /// </summary>
        private void UpdateNavigationState()
        {
            ((DelegateCommand)NavigateForwardCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)NavigateBackwardCommand).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Stores the current visualizations into the model
        /// </summary>
        private void StoreVisualizationsToModel()
        {
            Search.VisualizationAxis = Chart.Axis != null ? Chart.Axis.DisplayField : string.Empty;
            Search.VisualizationType = Chart.AxisType;
            Search.VisualizationSeries = Chart.Series.Select(s => new SearchContext.Visualization() { Key = s.DisplayField, Value = s.Visualization }).ToList();
            RaisePropertyChanged(nameof(Icon));
        }

        /// <summary>
        /// Checks if the current data can be exported
        /// </summary>
        /// <returns>True when data can be exported</returns>
        private bool SaveDataCommandCanExecute()
        {
            return _log.Count > 0 && Rows.Count > 0;
        }

        /// <summary>
        /// Export the current data to csv format
        /// </summary>
        private async void SaveDataCommandExecute()
        {
            var dlg = new SaveFileDialog();
            dlg.AddExtension = true;
            dlg.CheckPathExists = true;
            dlg.DefaultExt = "csv";
            dlg.Filter = "Comma Separated Value File (*.csv)|*.csv|Text File (*.txt)|*.txt";

            var log = _log.Files.First();
            if (Directory.Exists(log))
            {
                dlg.InitialDirectory = log;
            }
            else
            {
                dlg.InitialDirectory = Path.GetDirectoryName(log);
            }

            if (dlg.ShowDialog() == true)
            {
                StartProgress();

                await Task.Run(() =>
                {
                    var currentProgress = 0;
                    var reportedProgress = 0;
                    var format = Path.GetExtension(dlg.FileName);
                    using (var writer = new StreamWriter(dlg.FileName))
                    {
                        if (format == ".csv")
                        {
                            writer.WriteLine(string.Join(";", Columns.Select(c => c.HeaderText)));
                            foreach (var row in Rows.Cast<ILogItem>())
                            {
                                var percent = currentProgress++ * 100 / Rows.Count;
                                if (percent != reportedProgress)
                                {
                                    UpdateProgress(percent);
                                    reportedProgress = percent;
                                }

                                writer.WriteLine(string.Join(";", Columns.Select(c => string.Format(c.DisplayFormat, row.Fields[c.DisplayField]))));
                            }
                        }
                        else
                        {
                            foreach (var row in Rows.Cast<ILogItem>())
                            {
                                var percent = currentProgress++ * 100 / Rows.Count;
                                if (percent != reportedProgress)
                                {
                                    UpdateProgress(percent);
                                    reportedProgress = percent;
                                }

                                writer.WriteLine(string.Join("\t", Columns.Select(c => string.Format(c.DisplayFormat, row.Fields[c.DisplayField]))));
                            }
                        }
                    }
                });

                EndProgress();
            }
        }

        /// <summary>
        /// Copies the selected log item into the clipboard
        /// </summary>
        /// <param name="item">Item to copy</param>
        private void CopyData(ILogItem item)
        {
            Clipboard.SetText(string.Join("\t", Columns.Select(c => item.Fields[c.DisplayField].ToString())));
        }

        #endregion
    }
}
