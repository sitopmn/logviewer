using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace logviewer.core
{
    public static class DragDropExtensions
    {
        #region fields

        private static readonly List<FrameworkElement> _dropTargets = new List<FrameworkElement>();

        #endregion

        #region drag handling

        private static bool _isDragging = false;

        public static object GetDragData(DependencyObject obj)
        {
            return (object)obj.GetValue(DragDataProperty);
        }

        public static void SetDragData(DependencyObject obj, object value)
        {
            obj.SetValue(DragDataProperty, value);
        }

        // Using a DependencyProperty as the backing store for DragData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragDataProperty =
            DependencyProperty.RegisterAttached("DragData", typeof(object), typeof(DragDropExtensions), new PropertyMetadata(null, (d, e) =>
            {
                if (d is FrameworkElement element)
                {
                    element.MouseMove -= MouseMoveHandler;
                    element.MouseMove += MouseMoveHandler;
                }
            }));

        public static string GetDragFormat(DependencyObject obj)
        {
            return (string)obj.GetValue(DragFormatProperty);
        }

        public static void SetDragFormat(DependencyObject obj, string value)
        {
            obj.SetValue(DragFormatProperty, value);
        }

        // Using a DependencyProperty as the backing store for DragFormat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragFormatProperty =
            DependencyProperty.RegisterAttached("DragFormat", typeof(string), typeof(DragDropExtensions), new PropertyMetadata(string.Empty));

        private static void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                var result = VisualTreeHelper.HitTest(element, e.GetPosition(element));
                if (result.VisualHit != null && result.VisualHit.FindParent<System.Windows.Controls.Primitives.Thumb>() != null)
                {
                    return;
                }

                _isDragging = true;
                var format = !string.IsNullOrEmpty(GetDragFormat(element)) ? GetDragFormat(element) : GetDragData(element).GetType().Name;
                var data = new DataObject(format, GetDragData(element));

                foreach (var target in _dropTargets.Where(t => GetHideDropTarget(t)))
                {
                    target.Visibility = data.GetDataPresent(GetDropFormat(target)) ? Visibility.Visible : Visibility.Collapsed;
                }

                DragDrop.DoDragDrop(element, data, DragDropEffects.All);

                foreach (var target in _dropTargets.Where(t => GetHideDropTarget(t)))
                {
                    target.Visibility = Visibility.Collapsed;
                }

                e.Handled = true;
                _isDragging = false;
            }
        }

        #endregion

        #region drop handling

        public static ICommand GetDropCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(DropCommandProperty);
        }

        public static void SetDropCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(DropCommandProperty, value);
        }

        // Using a DependencyProperty as the backing store for DropCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.RegisterAttached("DropCommand", typeof(ICommand), typeof(DragDropExtensions), new PropertyMetadata(null, (d, e) =>
            {
                if (d is FrameworkElement element)
                {
                    if (!_dropTargets.Contains(element))
                    {
                        _dropTargets.Add(element);
                    }

                    element.AllowDrop = true;
                    element.Drop -= DropHandler;
                    element.Drop += DropHandler;
                    element.DragOver -= DragOverHandler;
                    element.DragOver += DragOverHandler;
                }
            }));

        public static string GetDropFormat(DependencyObject obj)
        {
            return (string)obj.GetValue(DropFormatProperty);
        }

        public static void SetDropFormat(DependencyObject obj, string value)
        {
            obj.SetValue(DropFormatProperty, value);
        }

        // Using a DependencyProperty as the backing store for DropFormat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DropFormatProperty =
            DependencyProperty.RegisterAttached("DropFormat", typeof(string), typeof(DragDropExtensions), new PropertyMetadata(string.Empty));

        public static DragDropEffects GetDropEffects(DependencyObject obj)
        {
            return (DragDropEffects)obj.GetValue(DropEffectsProperty);
        }

        public static void SetDropEffects(DependencyObject obj, DragDropEffects value)
        {
            obj.SetValue(DropEffectsProperty, value);
        }

        // Using a DependencyProperty as the backing store for DropEffects.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DropEffectsProperty =
            DependencyProperty.RegisterAttached("DropEffects", typeof(DragDropEffects), typeof(DragDropExtensions), new PropertyMetadata(DragDropEffects.All));

        public static bool GetHideDropTarget(DependencyObject obj)
        {
            return (bool)obj.GetValue(HideDropTargetProperty);
        }

        public static void SetHideDropTarget(DependencyObject obj, bool value)
        {
            obj.SetValue(HideDropTargetProperty, value);
        }

        // Using a DependencyProperty as the backing store for HideDropTarget.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HideDropTargetProperty =
            DependencyProperty.RegisterAttached("HideDropTarget", typeof(bool), typeof(DragDropExtensions), new PropertyMetadata(false, (d, e) =>
            {
                if (GetHideDropTarget(d))
                {
                    ((FrameworkElement)d).Visibility = Visibility.Collapsed;
                }
                else
                {
                    ((FrameworkElement)d).Visibility = Visibility.Visible;
                }
            }));

        private static void DragOverHandler(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;

            //e.Effects = DragDropEffects.None;
            if (sender is FrameworkElement element)
            {
                var format = GetDropFormat(element);
                var command = GetDropCommand(element);
                if (e.Data.GetDataPresent(format) && command != null)
                {
                    var data = e.Data.GetData(format);
                    if (command.CanExecute(data))
                    {
                        e.Effects = GetDropEffects(element);
                    }
                }
            }

            e.Handled = true;
        }

        private static void DropHandler(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                var format = GetDropFormat(element);
                var command = GetDropCommand(element);
                if (e.Data.GetDataPresent(format) && command != null)
                {
                    var data = e.Data.GetData(format);
                    if (command.CanExecute(data))
                    {
                        command.Execute(data);
                        e.Effects = GetDropEffects(element);
                        e.Handled = true;
                    }
                }
            }
        }

        #endregion
    }
}
