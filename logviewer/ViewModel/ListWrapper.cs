using logviewer.core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.ViewModel
{
    public class ListWrapper<T> : NotificationObject, IList<T>, IList, INotifyCollectionChanged
    {
        private readonly IReadOnlyList<T> _list;

        private readonly Func<T, int> _indexOf;

        bool IList.IsReadOnly => true;

        bool IList.IsFixedSize => false;

        int ICollection.Count => _list.Count;

        object ICollection.SyncRoot => null;

        bool ICollection.IsSynchronized => false;

        int ICollection<T>.Count => _list.Count;

        bool ICollection<T>.IsReadOnly => true;

        T IList<T>.this[int index] { get => _list[index]; set => throw new NotSupportedException(); }
        object IList.this[int index] { get => _list[index]; set => throw new NotSupportedException(); }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ListWrapper(IReadOnlyList<T> list)
        {
            _list = list;

            if (_list is INotifyCollectionChanged change)
            {
                change.CollectionChanged += (s, e) => Invoke(SourceCollectionChanged, e);
            }

            if (_list is INotifyPropertyChanged property)
            {
                property.PropertyChanged += (s, e) => Invoke(() => RaisePropertyChanged(e.PropertyName));
            }

            var indexOfMethod = list.GetType().GetMethod("IndexOf", new[] { typeof(T) });
            if (indexOfMethod != null)
            {
                var parameter = Expression.Parameter(typeof(T));
                _indexOf = Expression.Lambda<Func<T, int>>(Expression.Call(Expression.Constant(list), indexOfMethod, parameter), parameter).Compile();
            }
        }

        private void SourceCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 1)
            {
                var index = e.NewStartingIndex;
                foreach (var item in e.NewItems)
                {
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index++));
                }
            }
            else
            {
                CollectionChanged?.Invoke(this, e);
            }
        }

        bool IList.Contains(object value) => _list.Contains((T)value);

        bool ICollection<T>.Contains(T item) => _list.Contains(item);

        int IList.IndexOf(object value) => _indexOf != null ? _indexOf((T)value) : throw new NotSupportedException();

        int IList<T>.IndexOf(T item) => _indexOf != null ? _indexOf(item) : throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();

        int IList.Add(object value) => throw new NotSupportedException();

        void IList.Clear() => throw new NotSupportedException();

        void IList.Insert(int index, object value) => throw new NotSupportedException();

        void IList.Remove(object value) => throw new NotSupportedException();

        void IList.RemoveAt(int index) => throw new NotSupportedException();

        void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        void ICollection<T>.Clear() => throw new NotSupportedException();

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();

        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    }
}
