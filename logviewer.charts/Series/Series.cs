using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace logviewer.charts
{
    public class Series : Control
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(Series), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(IEnumerable<DataPoint>), typeof(Series), new PropertyMetadata(new ObservableCollection<DataPoint>(), (d, e) =>
            {
                if (d is Series chart)
                {
                    if (e.OldValue is INotifyCollectionChanged o) o.CollectionChanged -= chart.DataCollectionChanged;
                    if (e.NewValue is INotifyCollectionChanged n) n.CollectionChanged += chart.DataCollectionChanged;
                    chart.DataCollectionChanged(chart.Data, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }));

        public Series()
        {
            Background = new SolidColorBrush(Colors.Transparent);
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public IEnumerable<DataPoint> Data
        {
            get { return (IEnumerable<DataPoint>)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        protected virtual void OnDataChanged()
        { }

        private void DataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnDataChanged();
            InvalidateVisual();
        }
    }
}
