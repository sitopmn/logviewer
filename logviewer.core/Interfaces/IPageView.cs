using System;
using System.ComponentModel.Composition;

namespace logviewer.Interfaces
{
    /// <summary>
    /// Interface for display page implementations
    /// </summary>
    public interface IPageView
    {
    }

    /// <summary>
    /// Metadata for display pages
    /// </summary>
    public interface IPageViewMetadata
    {
        /// <summary>
        /// Gets the view model type
        /// </summary>
        Type ViewModel
        {
            get;
        }

        /// <summary>
        /// Gets the view type
        /// </summary>
        Type View
        {
            get;
        }

        /// <summary>
        /// Gets the context for the page
        /// </summary>
        Type Context
        {
            get;
        }

        /// <summary>
        /// Gets the index of the page in the menu
        /// </summary>
        int Index
        {
            get;
        }
    }

    /// <summary>
    /// Declares an expot for a display page
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExportPageViewAttribute : ExportAttribute, IPageViewMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportPageViewAttribute"/> class.
        /// </summary>
        public ExportPageViewAttribute(Type view, Type viewModel)
            : base(typeof(IPageView))
        {
            View = view;
            ViewModel = viewModel;
        }

        public ExportPageViewAttribute(Type view, Type viewModel, Type context, int index)
             : this(view, viewModel)
        {
            Context = context;
            Index = index;
        }

        /// <summary>
        /// Gets or sets the view model type for the page
        /// </summary>
        public Type ViewModel
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the view type
        /// </summary>
        public Type View
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the context for the page
        /// </summary>
        public Type Context
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the index of the page in the menu
        /// </summary>
        public int Index
        {
            get;
            private set;
        }
    }
}
