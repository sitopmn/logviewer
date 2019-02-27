using logviewer.Interfaces;
using logviewer.core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;

namespace logviewer.ViewModel
{
    /// <summary>
    /// ViewModel for the help dialog
    /// </summary>
    [Export]
    public class DialogHelpViewModel : NotificationObject
    {
        /// <summary>
        /// Storage for the help markdown
        /// </summary>
        private string _help;

        /// <summary>
        /// Storage for the license markdown
        /// </summary>
        private string _license;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogHelpViewModel"/> class
        /// </summary>
        /// <param name="sections">List of configured documentation sections</param>
        [ImportingConstructor]
        public DialogHelpViewModel()
        {
            Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
        }

        /// <summary>
        /// Gets the help text
        /// </summary>
        public string Help => LoadHelpFromFile("HELP.md", ref _help);

        /// <summary>
        /// Gets the license text
        /// </summary>
        public string License => LoadHelpFromFile("LICENSE", ref _license);

        /// <summary>
        /// Gets the version number
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Loads help contents from file if not yet loaded
        /// </summary>
        /// <param name="file">Name of the file containing the help to load</param>
        /// <param name="storage">Field to store the loaded help</param>
        /// <returns>Help contents loaded from the given file</returns>
        private string LoadHelpFromFile(string file, ref string storage)
        {
            if (string.IsNullOrEmpty(storage))
            {
                try
                {
                    using (var reader = new StreamReader(file))
                    {
                        storage = reader.ReadToEnd();
                    }
                }
                catch
                {
                    storage = "No help available";
                }
            }

            return storage;
        }
    }
}
