using logviewer.core;

namespace logviewer.Model
{
    /// <summary>
    /// The types of visualizations for columns
    /// </summary>
    public enum VisualizationType
    {
        /// <summary>
        /// The column is not visualized
        /// </summary>
        [EnumConverter(MaterialDesignThemes.Wpf.PackIconKind.FileQuestion, "UNDEFINED")]
        None = 0,

        /// <summary>
        /// The column is visualized using a line chart
        /// </summary>
        [EnumConverter(MaterialDesignThemes.Wpf.PackIconKind.ChartLine, "LINE CHART")]
        Line = 1,

        /// <summary>
        /// The column is visualized using a step chart
        /// </summary>
        [EnumConverter(MaterialDesignThemes.Wpf.PackIconKind.ChartHistogram, "STEP CHART")]
        Step = 2,

        /// <summary>
        /// The column is visualized using a scatter chart
        /// </summary>
        [EnumConverter(MaterialDesignThemes.Wpf.PackIconKind.ChartScatterplotHexbin, "SCATTER CHART")]
        Scatter = 3,

        /// <summary>
        /// The column is visualized using a column chart
        /// </summary>
        [EnumConverter(MaterialDesignThemes.Wpf.PackIconKind.ChartBar, "COLUMN CHART")]
        Column = 4,
    }
}
