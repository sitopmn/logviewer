using System;
using System.Windows.Input;

namespace logviewer.core
{
    public class DelegateCommand : ICommand
    {
        private Action _execute;

        private Func<bool> _canExecute;

        public DelegateCommand(Action execute)
        {
            _execute = execute;
        }

        public DelegateCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            EventHandler handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }

    public class DelegateCommand<T> : ICommand
    {
        private Action<T> _execute;

        private Func<T, bool> _canExecute;
        private Action exportView;

        public DelegateCommand(Action<T> execute)
        {
            _execute = execute;
        }

        public DelegateCommand(Action exportView)
        {
            this.exportView = exportView;
        }

        public DelegateCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            EventHandler handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}