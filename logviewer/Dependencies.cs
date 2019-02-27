using logviewer.Interfaces;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel.Composition;

namespace logviewer
{
    /// <summary>
    /// A factory class for serving global dependencies to the MEF framework
    /// </summary>
    internal class Dependencies
    {
        [Export]
        public ISnackbarMessageQueue MessageQueue { get; } = new SnackbarMessageQueue(TimeSpan.FromSeconds(2));

        [Export]
        public ISettings Settings => Properties.Settings.Default;
    }
}
