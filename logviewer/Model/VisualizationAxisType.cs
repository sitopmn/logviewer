using logviewer.core;

namespace logviewer.Model
{
    /// <summary>
    /// General type of a visualization
    /// </summary>
    public enum VisualizationAxisType
    {
        /// <summary>
        /// The visualization uses a linear X axis
        /// </summary>
        [EnumConverter(MaterialDesignThemes.Wpf.PackIconKind.ChartLineVariant, "Linear")]
        Linear = 0,

        /// <summary>
        /// The visualization uses an angular X axis (i.e. a pie chart)
        /// </summary>
        [EnumConverter(MaterialDesignThemes.Wpf.PackIconKind.ChartDonutVariant, "Angular")]
        Angular = 1,
    }
}
