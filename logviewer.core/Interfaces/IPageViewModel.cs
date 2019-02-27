using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace logviewer.Interfaces
{
    /// <summary>
    /// Interface for display page implementations
    /// </summary>
    public interface IPageViewModel : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// Event for reporting progress information
        /// </summary>
        event EventHandler<ProgressEventArgs> Progress;

        /// <summary>
        /// Gets the icon for the page
        /// </summary>
        PackIconKind Icon
        {
            get;
        }

        /// <summary>
        /// Gets the title for the page
        /// </summary>
        string Title
        {
            get;
        }

        /// <summary>
        /// Gets or sets the model of the page
        /// </summary>
        Model.Context Context
        {
            get;
            set;
        }

        /// <summary>
        /// Updates the page contents
        /// </summary>
        Task Update();
    }

    /// <summary>
    /// Metadata for display pages
    /// </summary>
    public interface IPageViewModelMetadata
    {
        /// <summary>
        /// Gets the navigation context type
        /// </summary>
        Type Context
        {
            get;
        }
    }

    /// <summary>
    /// Declares an expot for a display page
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExportPageViewModelAttribute : ExportAttribute, IPageViewModelMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportPageViewModelAttribute"/> class.
        /// </summary>
        public ExportPageViewModelAttribute()
            : base(typeof(IPageViewModel))
        {
        }

        /// <summary>
        /// Gets or the navigation context type
        /// </summary>
        public Type Context
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Event arguments for progress reporting
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Start the operation
        /// </summary>
        public static readonly ProgressEventArgs Start = new ProgressEventArgs(true, true, 0);

        /// <summary>
        /// End the operation
        /// </summary>
        public static readonly ProgressEventArgs End = new ProgressEventArgs(false, false, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressEventArgs"/> class
        /// </summary>
        /// <param name="progress">Current progress</param>
        public ProgressEventArgs(double progress)
            : this(true, false, progress)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressEventArgs"/> class
        /// </summary>
        private ProgressEventArgs(bool isActive, bool isIndeterminate, double progress)
        {
            IsActive = isActive;
            IsIndeterminate = isIndeterminate;
            Progress = progress;
        }

        /// <summary>
        /// Gets a value indicating the process is active
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Gets a value indicating the process is indeterminate
        /// </summary>
        public bool IsIndeterminate { get; }
        
        /// <summary>
        /// Gets the current progress. Greater than 0 is determinate progress, less than 0 is indeterminate progress
        /// </summary>
        public double Progress { get; }
    }
}
