using logviewer.Interfaces;
using logviewer.Model;
using logviewer.core;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.ViewModel
{
    /// <summary>
    /// Base view model for pages
    /// </summary>
    public abstract class PageViewModel : NotificationObject, IPageViewModel
    {
        /// <summary>
        /// Event for reporting progress
        /// </summary>
        public event EventHandler<ProgressEventArgs> Progress;

        /// <summary>
        /// Gets the icon for the page
        /// </summary>
        public PackIconKind Icon => Context.Icon;

        /// <summary>
        /// Gets the title of the page
        /// </summary>
        public virtual string Title => Context.Title;

        /// <summary>
        /// Gets or sets the context of the page
        /// </summary>
        public Context Context
        {
            get => GetValue<Context>();
            set => SetValue(value, OnModelUpdated);
        }

        /// <summary>
        /// Is called when the model is updated
        /// </summary>
        /// <param name="oldContext">The old context</param>
        /// <param name="newContext">The new context</param>
        public virtual void OnModelUpdated(Context oldContext, Context newContext)
        { }

        /// <summary>
        /// Update the page contents
        /// </summary>
        public abstract Task Update();

        /// <summary>
        /// Releases resources held by the page view model
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Starts a progress display
        /// </summary>
        protected void StartProgress()
        {
            Progress?.Invoke(this, ProgressEventArgs.Start);
        }

        /// <summary>
        /// Updates the progress display
        /// </summary>
        /// <param name="progress">Current progress information</param>
        protected void UpdateProgress(double progress)
        {
            Progress?.Invoke(this, new ProgressEventArgs(progress));
        }

        /// <summary>
        /// Ends a progress display
        /// </summary>
        protected void EndProgress()
        {
            Progress?.Invoke(this, ProgressEventArgs.End);
        }
    }
}
