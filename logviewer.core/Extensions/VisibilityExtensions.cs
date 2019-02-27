using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace logviewer.core
{
    public static class VisibilityExtensions
    {
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(VisibilityExtensions), new PropertyMetadata(true, IsVisiblePropertyChanged));

        public static readonly DependencyProperty IsHiddenProperty =
            DependencyProperty.RegisterAttached("IsHidden", typeof(bool), typeof(VisibilityExtensions), new PropertyMetadata(false, IsHiddenPropertyChanged));

        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.RegisterAttached("IsCollapsed", typeof(bool), typeof(VisibilityExtensions), new PropertyMetadata(false, IsCollapsedPropertyChanged));

        public static bool GetIsVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsVisibleProperty);
        }

        public static void SetIsVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(IsVisibleProperty, value);
        }

        public static bool GetIsHidden(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsHiddenProperty);
        }

        public static void SetIsHidden(DependencyObject obj, bool value)
        {
            obj.SetValue(IsHiddenProperty, value);
        }

        public static bool GetIsCollapsed(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsCollapsedProperty);
        }

        public static void SetIsCollapsed(DependencyObject obj, bool value)
        {
            obj.SetValue(IsCollapsedProperty, value);
        }

        private static void IsVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                element.Visibility = GetIsVisible(element) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static void IsCollapsedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                element.Visibility = GetIsCollapsed(element) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private static void IsHiddenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                element.Visibility = GetIsHidden(element) ? Visibility.Hidden : Visibility.Visible;
            }
        }

    }
}
