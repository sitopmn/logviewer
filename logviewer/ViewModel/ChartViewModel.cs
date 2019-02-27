using logviewer.Interfaces;
using logviewer.core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace logviewer.Model
{
    /// <summary>
    /// ViewModel for the charts
    /// </summary>
    public class ChartViewModel : NotificationObject
    {
        /// <summary>
        /// Rows which are available for display but no series are defined yet.
        /// </summary>
        private IEnumerable<ILogItem> _deferredRows = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChartViewModel"/> class.
        /// </summary>
        public ChartViewModel()
        {
            AxisType = VisualizationAxisType.Linear;
            Series = new ObservableCollection<ColumnData>();
            AddSeriesCommand = new DelegateCommand<ColumnData>(AddSeries);
            RemoveSeriesCommand = new DelegateCommand<ColumnData>(c => RemoveSeries(c));
            AddAxisCommand = new DelegateCommand<ColumnData>(c => Axis = c);
            RemoveAxisCommand = new DelegateCommand<ColumnData>(c => Axis = null);
            ToggleSeriesCommand = new DelegateCommand<ColumnData>(SwitchVisualization);
            ToggleAxisCommand = new DelegateCommand(SwitchAxis);
        }

        /// <summary>
        /// Event which fires when the chart is modified
        /// </summary>
        public event EventHandler Updated;

        /// <summary>
        /// Gets a list of columns containing the series values
        /// </summary>
        public ObservableCollection<ColumnData> Series
        {
            get => GetValue<ObservableCollection<ColumnData>>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets the column which contains the axis values
        /// </summary>
        public ColumnData Axis
        {
            get => GetValue<ColumnData>();
            set => SetValue(value, () =>
            {
                if (value != null && _deferredRows != null && Series.Count > 0)
                {
                    Data = _deferredRows;
                    _deferredRows = null;
                }

                RaisePropertyChanged(nameof(IsCategorized));
            });
        }

        /// <summary>
        /// Gets the visualization axis type
        /// </summary>
        public VisualizationAxisType AxisType
        {
            get => GetValue<VisualizationAxisType>();
            private set => SetValue(ValidateAxisType(value));
        }

        /// <summary>
        /// Gets the actual data to visualize
        /// </summary>
        public IEnumerable<ILogItem> Data
        {
            get => GetValue<IEnumerable<ILogItem>>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets the number of log items to visualize
        /// </summary>
        public int Count
        {
            get => GetValue<int>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets a value indicating the axis is a category axis
        /// </summary>
        public bool IsCategorized
        {
            get => Axis != null && Axis.DisplayType == typeof(string);
        }

        /// <summary>
        /// Gets the command to add a series to the chart
        /// </summary>
        public ICommand AddSeriesCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to remove a series from the chart
        /// </summary>
        public ICommand RemoveSeriesCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to toggle the series type
        /// </summary>
        public ICommand ToggleSeriesCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to add an axis from the chart
        /// </summary>
        public ICommand AddAxisCommand
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Gets the command to remove an axis from the chart
        /// </summary>
        public ICommand RemoveAxisCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to switch the axis type
        /// </summary>
        public ICommand ToggleAxisCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Reset the chart
        /// </summary>
        public void Reset()
        {
            _deferredRows = null;
            Series.Clear();
            Axis = null;
            Updated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Update the chart data
        /// </summary>
        /// <param name="columns">The current list of columns</param>
        /// <param name="rows">The current list of rows</param>
        /// <param name="rowCount">The number of rows</param>
        /// <param name="axisType">The X axis type</param>
        /// <param name="axis">Name of the column containing the X axis values</param>
        /// <param name="series">List of column visualization settings</param>
        public void Update(IEnumerable<ColumnData> columns, IEnumerable<ILogItem> rows, int rowCount, VisualizationAxisType axisType, string axis, List<SearchContext.Visualization> series)
        {
            Axis = columns.FirstOrDefault(c => c.DisplayField == axis);

            Series.Clear();
            foreach (var v in series)
            {
                var column = columns.FirstOrDefault(c => c.DisplayField == v.Key);
                if (column != null)
                {
                    column.Visualization = v.Value;
                    Series.Add(column);
                }
            }

            Count = rowCount;
            AxisType = axisType;

            if (Axis == null || Series.Count == 0)
            {
                Data = Enumerable.Empty<ILogItem>();
                _deferredRows = rows;
            }
            else
            {
                Data = rows;
                _deferredRows = null;
            }
        }

        /// <summary>
        /// Switches between the column visualization types
        /// </summary>
        /// <param name="column">The column to set the visualization type on</param>
        private void SwitchVisualization(ColumnData column)
        {
            // advance to the next visualization
            do
            {
                column.Visualization = (VisualizationType)((int)column.Visualization % 4 + 1);
            }
            while (!VisualizationIsValid(column.Visualization));

            var temp = Series;
            Series = null;
            Series = temp;

            Updated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Checks if the given visualization type is valid for the current axis configuration
        /// </summary>
        /// <param name="visualization"><see cref="VisualizationType"/> to check</param>
        /// <returns>True if the configuration is valid</returns>
        private bool VisualizationIsValid(VisualizationType visualization)
        {
            switch (visualization)
            {
                case VisualizationType.Column:
                    return Axis.DisplayType == typeof(string);

                case VisualizationType.Scatter:
                    return true;

                case VisualizationType.Line:
                case VisualizationType.Step:
                    return Axis.DisplayType != typeof(string);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Adds a series to the chart
        /// </summary>
        /// <param name="column">Column to add as a series</param>
        private void AddSeries(ColumnData column)
        {
            if (column.Visualization == VisualizationType.None)
            {
                if (Axis.DisplayType == typeof(string))
                {
                    column.Visualization = VisualizationType.Column;
                }
                else
                {
                    if (column.DisplayType == typeof(string))
                    {
                        column.Visualization = VisualizationType.Scatter;
                    }
                    else
                    {
                        column.Visualization = VisualizationType.Line;
                    }
                }
            }

            Series.Add(column);
            AxisType = AxisType;

            if (_deferredRows != null)
            {
                Data = _deferredRows;
                _deferredRows = null;
            }

            Updated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Removes a series from the chart
        /// </summary>
        /// <param name="c">Series to remove</param>
        private void RemoveSeries(ColumnData c)
        {
            Series.Remove(c);
            Updated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Switches the X axis type
        /// </summary>
        private void SwitchAxis()
        {
            VisualizationAxisType type;

            if (AxisType == VisualizationAxisType.Angular)
            {
                type = VisualizationAxisType.Linear;
            }
            else
            {
                type = VisualizationAxisType.Angular;
            }

            AxisType = type;
            Updated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Validates the X axis type against the current column configuration
        /// </summary>
        /// <param name="type"><see cref="VisualizationAxisType"/> to validate</param>
        /// <returns>A validated <see cref="VisualizationAxisType"/></returns>
        private VisualizationAxisType ValidateAxisType(VisualizationAxisType type)
        {
            if (type == VisualizationAxisType.Angular && Axis != null && (Axis.DisplayType != typeof(string) || Series.Any(s => s.DisplayType == typeof(string))))
            {
                type = VisualizationAxisType.Linear;
            }

            return type;
        }
    }
}
