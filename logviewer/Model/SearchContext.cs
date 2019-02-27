using logviewer.Interfaces;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Xml.Serialization;

namespace logviewer.Model
{
    /// <summary>
    /// Navigation context for search pages
    /// </summary>
    public class SearchContext : logviewer.Model.Context
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchContext"/> class.
        /// </summary>
        public SearchContext()
            : this(Properties.Resources.Menu_Search, string.Empty, VisualizationAxisType.Linear, string.Empty, new List<Visualization>(), null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchContext"/> class.
        /// </summary>
        /// <param name="title">The title of the search</param>
        /// <param name="query">The search query</param>
        /// <param name="visualizationType">The type of visualization</param>
        /// <param name="visualizationAxis">The name of the visualization axis column</param>
        /// <param name="visualizations">The list of visualizations</param>
        /// <param name="executedQuery">A pre build query object</param>
        public SearchContext(string title, string query, VisualizationAxisType visualizationType, string visualizationAxis, List<Visualization> visualizations, IQuery executedQuery)
            : base(PackIconKind.Magnify, title)
        {
            Query = query;
            Executor = executedQuery;
            VisualizationType = visualizationType;
            VisualizationAxis = visualizationAxis;
            VisualizationSeries = visualizations;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchContext"/> class.
        /// </summary>
        /// <param name="search">A <see cref="SearchContext"/> instance to clone</param>
        private SearchContext(SearchContext search)
            : base(search)
        {
            Query = search.Query;
            VisualizationAxis = search.VisualizationAxis;
            VisualizationType = search.VisualizationType;
            VisualizationSeries = search.VisualizationSeries.ToList();
            Executor = search.Executor;
        }

        /// <summary>
        /// Gets or sets a value indicating the query was loaded from a repository
        /// </summary>
        [XmlIgnore]
        public bool IsFromRepository
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the icon of the search
        /// </summary>
        [XmlIgnore]
        public override PackIconKind Icon
        {
            get
            {
                if (VisualizationSeries.Count > 0)
                {
                    if (VisualizationType == VisualizationAxisType.Linear)
                    {
                        return PackIconKind.ChartLine;
                    }
                    else
                    {
                        return PackIconKind.ChartDonutVariant;
                    }
                }
                else
                {
                    return PackIconKind.Magnify;
                }
            }
        }

        /// <summary>
        /// Gets or sets the search query
        /// </summary>
        public string Query
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets the type of visualization
        /// </summary>
        public VisualizationAxisType VisualizationType
        {
            get => GetValue<VisualizationAxisType>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets the name of the visualization axis column
        /// </summary>
        public string VisualizationAxis
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets the list of visualizations
        /// </summary>
        [XmlArrayItem("Series")]
        public List<Visualization>  VisualizationSeries
        {
            get => GetValue<List<Visualization>>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets a pre build query object
        /// </summary>
        [XmlIgnore]
        public IQuery Executor
        {
            get => GetValue<IQuery>();
            set => SetValue(value);
        }

        /// <summary>
        /// Calculates the hash code of the object
        /// </summary>
        /// <returns>The hash code of the object</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Checks another object for equality with this instance
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>True if both objects are equal</returns>
        public override bool Equals(object obj)
        {
            var other = obj as SearchContext;
            return other != null &&
                other.Title == Title &&
                other.Query == Query &&
                other.VisualizationAxis == VisualizationAxis &&
                other.VisualizationType == VisualizationType;
        }

        /// <summary>
        /// Creates a clone of this object
        /// </summary>
        /// <returns>A clone of this object</returns>
        public override object Clone()
        {
            return new SearchContext(this);
        }

        /// <summary>
        /// Visualization data for a column in a <see cref="SearchContext"/> object
        /// </summary>
        public struct Visualization
        {
            /// <summary>
            /// The name of the column to visualize
            /// </summary>
            [XmlAttribute("Column")]
            public string Key;

            /// <summary>
            /// The visualization selected for the column
            /// </summary>
            [XmlAttribute("Type")]
            public VisualizationType Value;
        }
    }
}
