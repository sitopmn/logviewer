using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace logviewer.core
{
    /// <summary>
    /// Base class for view model implementations
    /// </summary>
    public class NotificationObject : INotifyPropertyChanged
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        /// <summary>
        /// Notifies property changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="name">The name of the changed property</param>
        protected void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected T GetValue<T>([CallerMemberName] string name = "")
        {
            if (!_values.ContainsKey(name))
            {
                _values[name] = default(T);
            }

            return (T)_values[name];
        }

        protected void SetValue<T>(T value, [CallerMemberName] string name = "")
        {
            _values[name] = value;
            RaisePropertyChanged(name);
        }

        protected void SetValue<T>(T value, Action<T> handler, [CallerMemberName] string name = "")
        {
            _values[name] = value;
            handler(value);
            RaisePropertyChanged(name);
        }

        protected void SetValue<T>(T value, Action handler, [CallerMemberName] string name = "")
        {
            _values[name] = value;
            handler();
            RaisePropertyChanged(name);
        }

        protected void SetValue<T>(T value, Action<T, T> handler, [CallerMemberName] string name = "")
        {
            var old = _values.ContainsKey(name) ? (T)_values[name] : default(T);
            _values[name] = value;
            handler(old, value);
            RaisePropertyChanged(name);
        }

        protected void Invoke(Action action)
        {
            Invoke(DispatcherPriority.Normal, (Delegate)action);
        }

        protected void Invoke<T1>(Action<T1> action, T1 a1)
        {
            Invoke(DispatcherPriority.Normal, (Delegate)action, a1);
        }

        protected void Invoke<T1, T2>(Action<T1, T2> action, T1 a1, T2 a2)
        {
            Invoke(DispatcherPriority.Normal, (Delegate)action, a1, a2);
        }

        protected void Invoke<T1, T2, T3>(Action<T1, T2, T3> action, T1 a1, T2 a2, T3 a3)
        {
            Invoke(DispatcherPriority.Normal, (Delegate)action, a1, a2, a3);
        }

        protected void Invoke<T1, T2, T3, T4>(Action<T1, T2, T3, T3> action, T1 a1, T2 a2, T3 a3, T4 a4)
        {
            Invoke(DispatcherPriority.Normal, (Delegate)action, a1, a2, a3, a4);
        }

        protected void Invoke(DispatcherPriority priority, Action action)
        {
            Invoke(priority, (Delegate)action);
        }

        protected void Invoke<T1>(DispatcherPriority priority, Action<T1> action, T1 a1)
        {
            Invoke(priority, (Delegate)action, a1);
        }

        protected void Invoke<T1, T2>(DispatcherPriority priority, Action<T1, T2> action, T1 a1, T2 a2)
        {
            Invoke(priority, (Delegate)action, a1, a2);
        }

        protected void Invoke<T1, T2, T3>(DispatcherPriority priority, Action<T1, T2, T3> action, T1 a1, T2 a2, T3 a3)
        {
            Invoke(priority, (Delegate)action, a1, a2, a3);
        }

        protected void Invoke<T1, T2, T3, T4>(DispatcherPriority priority, Action<T1, T2, T3, T3> action, T1 a1, T2 a2, T3 a3, T4 a4)
        {
            Invoke(priority, (Delegate)action, a1, a2, a3, a4);
        }

        private void Invoke(DispatcherPriority priority, Delegate d, params object[] args)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(d, priority, args);
                return;
            }

            d.DynamicInvoke(args);
        }

    }
}
