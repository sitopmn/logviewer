using logviewer.core;
using logviewer.core.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.ViewModel
{
    public class DialogOpenViewModel : NotificationObject
    {
        public DialogOpenViewModel(IEnumerable<string> formats)
        {
            Sources = new ObservableCollection<string>();
            Formats = new ObservableCollection<string>(formats);
            SelectedFormat = Formats.First();
        }

        public DialogOpenViewModel(IEnumerable<string> formats, string[] sources)
            : this(formats)
        {
            Sources.AddRange(sources);
        }

        public ObservableCollection<string> Sources { get; }

        public ObservableCollection<string> Formats { get; }

        public string SelectedFormat
        {
            get => GetValue<string>();
            set => SetValue(value);
        }
    }
}
