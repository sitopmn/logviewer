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
    /// View model for the bookmark save dialog
    /// </summary>
    public class DialogSaveViewModel : NotificationObject
    {
        /// <summary>
        /// List of existing bookmark names
        /// </summary>
        private readonly List<string> _existingTitles;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogSaveViewModel"/> class.
        /// </summary>
        /// <param name="title">Title of the bookmark</param>
        /// <param name="existingTitles">List of existing bookmark names</param>
        public DialogSaveViewModel(string title, List<string> existingTitles)
        {
            _existingTitles = existingTitles;
            Title = title;
        }

        /// <summary>
        /// Gets or sets the title of the bookmark
        /// </summary>
        public string Title
        {
            get => GetValue<string>();
            set => SetValue(value, t =>
            {
                IsExisting = _existingTitles.Contains(t);
                IsComplete = !string.IsNullOrWhiteSpace(t);
            });
        }

        /// <summary>
        /// Gets a value indicating a bookmark using the same title is already present
        /// </summary>
        public bool IsExisting
        {
            get => GetValue<bool>();
            private set => SetValue(value);
        }

        /// <summary>
        /// Gets a value indication data entry is complete
        /// </summary>
        public bool IsComplete
        {
            get => GetValue<bool>();
            private set => SetValue(value);
        }
    }
}
