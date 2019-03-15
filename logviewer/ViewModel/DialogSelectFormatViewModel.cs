using logviewer.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.ViewModel
{
    /// <summary>
    /// ViewModel for the log format selection dialog
    /// </summary>
    internal class DialogSelectFormatViewModel : NotificationObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogSelectFormatViewModel"/> class
        /// </summary>
        /// <param name="formats">List of supported formats</param>
        public DialogSelectFormatViewModel(IEnumerable<string> formats)
        {
            Formats = formats.ToList();
        }

        /// <summary>
        /// Gets the list of supported formats
        /// </summary>
        public List<string> Formats
        {
            get;
        }

        /// <summary>
        /// Gets or sets the selected format
        /// </summary>
        public string SelectedFormat
        {
            get => GetValue<string>();
            set => SetValue(value);
        }
    }
}
