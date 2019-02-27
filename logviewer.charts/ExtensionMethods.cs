using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace logviewer.charts
{
    internal static class ExtensionMethods
    {
        public static T FindFirstChild<T>(this DependencyObject start) where T : DependencyObject
        {
            var stack = new Stack<DependencyObject>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                var control = stack.Pop();
                if (control is T)
                {
                    return (T)control;
                }
                else
                {
                    for (var i = 0; i < VisualTreeHelper.GetChildrenCount(control); i++)
                    {
                        stack.Push(VisualTreeHelper.GetChild(control, i));
                    }
                }
            }

            return null;
        }

        public static T FindParent<T>(this DependencyObject start) where T : DependencyObject
        {
            while (start != null && !(start is T))
            {
                start = VisualTreeHelper.GetParent(start);
            }

            return (T)start;
        }

        public static void Handle<TItem>(this NotifyCollectionChangedEventArgs e, object sender, IEnumerable currentItems, Action<TItem> newItem, Action<TItem> oldItem)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    foreach (var i in currentItems.Cast<TItem>())
                    {
                        oldItem(i);
                    }
                    foreach(var i in ((IList)sender).Cast<TItem>())
                    {
                        newItem(i);
                    }
                    break;

                case NotifyCollectionChangedAction.Add:
                    foreach (var i in e.NewItems.Cast<TItem>())
                    {
                        newItem(i);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var i in e.OldItems.Cast<TItem>())
                    {
                        oldItem(i);
                    }
                    break;
            }
        }
    }
}
