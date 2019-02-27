using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace logviewer.Controls
{
    public class VirtualizingHeaderPanel : VirtualizingPanel, IScrollInfo
    {
        public static readonly DependencyProperty HeadersProperty =
            DependencyProperty.Register("Headers", typeof(UIElementCollection), typeof(VirtualizingHeaderPanel), new PropertyMetadata(null));

        public static readonly DependencyProperty IsStickyVerticallyProperty =
            DependencyProperty.RegisterAttached("IsStickyVertically", typeof(bool), typeof(VirtualizingHeaderPanel), new PropertyMetadata(false));

        public static readonly DependencyProperty IsStickyHorizontallyProperty =
            DependencyProperty.RegisterAttached("IsStickyHorizontally", typeof(bool), typeof(VirtualizingHeaderPanel), new PropertyMetadata(false));

        private Size _childSize = Size.Empty;

        private Size _expandedHeaderSize = Size.Empty;

        private Size _collapsedHeaderSize = Size.Empty;

        private int _deferredScrollToIndex = -1;

        private bool _init = false;

        public VirtualizingHeaderPanel()
            : base()
        {
            Headers = new UIElementCollection(this, this);
        }

        public UIElementCollection Headers
        {
            get { return (UIElementCollection)GetValue(HeadersProperty); }
            set { SetValue(HeadersProperty, value); }
        }

        public bool CanVerticallyScroll { get; set; }

        public bool CanHorizontallyScroll { get; set; }

        public double ExtentWidth { get; private set; }

        public double ExtentHeight { get; private set; }

        public double ViewportWidth => ActualWidth;

        public double ViewportHeight => ActualHeight;

        public double HorizontalOffset { get; private set; }

        public double VerticalOffset { get; private set; }
        
        public ScrollViewer ScrollOwner { get; set; }

        protected override int VisualChildrenCount => Children.Count + (_init ? Headers.Count : 0);
        //protected override int VisualChildrenCount => base.VisualChildrenCount;

        public static bool GetIsStickyVertically(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsStickyVerticallyProperty);
        }

        public static void SetIsStickyVertically(DependencyObject obj, bool value)
        {
            obj.SetValue(IsStickyVerticallyProperty, value);
        }

        public static bool GetIsStickyHorizontally(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsStickyHorizontallyProperty);
        }

        public static void SetIsStickyHorizontally(DependencyObject obj, bool value)
        {
            obj.SetValue(IsStickyHorizontallyProperty, value);
        }

        public void LineUp() => SetVerticalOffset(VerticalOffset - _childSize.Height * 1);
        public void LineDown() => SetVerticalOffset(VerticalOffset + _childSize.Height * 1);
        public void LineLeft() => throw new NotImplementedException();
        public void LineRight() => throw new NotImplementedException();
        public void PageUp() => SetVerticalOffset(VerticalOffset - _childSize.Height * 10);
        public void PageDown() => SetVerticalOffset(VerticalOffset + _childSize.Height * 10);
        public void PageLeft() => throw new NotImplementedException();
        public void PageRight() => throw new NotImplementedException();
        public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - _childSize.Height * 1);
        public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + _childSize.Height * 1);
        public void MouseWheelLeft() => throw new NotImplementedException();
        public void MouseWheelRight() => throw new NotImplementedException();

        public void SetHorizontalOffset(double offset)
        {
            HorizontalOffset = Math.Max(0, Math.Min(offset, ExtentWidth - ViewportWidth));
            InvalidateMeasure();
        }

        public void SetVerticalOffset(double offset)
        {
            VerticalOffset = Math.Max(0, Math.Min(offset, ExtentHeight - ViewportHeight));
            InvalidateMeasure();
        }

        public void ScrollToIndex(int childIndex)
        {
            if (!_childSize.IsEmpty)
            {
                SetVerticalOffset(childIndex);
            }
            else
            {
                _deferredScrollToIndex = childIndex;
            }
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return rectangle;
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < Children.Count)
            {
                return Children[index];
            }
            else 
            {
                return Headers[index - base.VisualChildrenCount];
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _init = true;

            _expandedHeaderSize = new Size();
            _collapsedHeaderSize = new Size();
            var children = this.InternalChildren;

            // measure the headers
            foreach (UIElement header in Headers)
            {
                //if (!InternalChildren.Contains(header))
                //{
                //    AddInternalChild(header);
                //}

                if (GetIsStickyHorizontally(header))
                {
                    header.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                    _expandedHeaderSize.Width = Math.Max(_expandedHeaderSize.Width, header.DesiredSize.Width);
                }
                else
                {
                    header.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                }
                
                _expandedHeaderSize.Height += header.DesiredSize.Height;

                if (GetIsStickyVertically(header))
                {
                    _collapsedHeaderSize.Height += header.DesiredSize.Height;
                }

                if (GetIsStickyHorizontally(header))
                {
                    _collapsedHeaderSize.Width = Math.Max(_collapsedHeaderSize.Width, availableSize.Width);
                }
                else
                {
                    _collapsedHeaderSize.Width = Math.Max(_collapsedHeaderSize.Width, header.DesiredSize.Width);
                }
            }

            // get the number of children
            var count = IsItemsHost ? ((ItemContainerGenerator)ItemContainerGenerator).Items.Count : Children.Count /* - Headers.Count*/;

            // get the first child for measuring if no child size is set
            _childSize = MeasureChildSize(availableSize);

            // generate visible children
            if (IsItemsHost && count > 0)
            {
                // generate children for the visible index range
                var startIndex = (int)Math.Max(Math.Floor(Math.Max(0, (VerticalOffset - _expandedHeaderSize.Height) / _childSize.Height)) - 1, 0);
                var visibleCount = (int)Math.Ceiling(availableSize.Height / _childSize.Height) + 1;
                CleanUpItems(startIndex, startIndex + visibleCount - 1);
                GenerateItems(startIndex, startIndex + visibleCount - 1);
            }

            // measure children
            foreach (var element in Children.Cast<UIElement>()/*.Except(Headers)*/.Select((c, i) => new { index = i, child = c }))
            {
                // element.child.Measure(availableSize);
                element.child.Measure(new Size(double.PositiveInfinity, _childSize.Height));
                _childSize.Width = Math.Max(_childSize.Width, element.child.DesiredSize.Width);
            }

            // calculate the height of all stacked children
            var childrenHeight = _childSize.IsEmpty ? 0 : _childSize.Height * count;

            // calculate the scroll extent
            ExtentWidth = Math.Max(_childSize.Width, _collapsedHeaderSize.Width);
            ExtentHeight = childrenHeight + _expandedHeaderSize.Height;
            ScrollOwner?.InvalidateScrollInfo();

            if (_deferredScrollToIndex >= 0)
            {
                SetVerticalOffset(_deferredScrollToIndex);
                _deferredScrollToIndex = -1;
            }
            else
            {
                SetVerticalOffset(VerticalOffset);
            }

            SetHorizontalOffset(HorizontalOffset);

            // return the actual minimum panel size required
            return new Size(Math.Min(_expandedHeaderSize.Width, availableSize.Width), Math.Min(_expandedHeaderSize.Height + childrenHeight, availableSize.Height));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var y = -VerticalOffset;
            var headerY = 0.0;

            // arrange the headers
            foreach (UIElement header in Headers)
            {
                var hx = 0.0;
                var width = finalSize.Width;
                if (!GetIsStickyHorizontally(header))
                {
                    hx = -HorizontalOffset;
                    width = ExtentWidth;
                }

                double hy;
                if (GetIsStickyVertically(header) && y < 0)
                {
                    hy = headerY;
                    headerY += header.DesiredSize.Height;
                }
                else
                {
                    hy = y;
                    y += header.DesiredSize.Height;
                }

                if (double.IsInfinity(width) || double.IsNaN(width))
                {
                    width = finalSize.Width;
                }

                header.Arrange(new Rect(hx, hy, width, header.DesiredSize.Height));
                SetZIndex(header, 1);
            }

            // arrange the children
            foreach (var element in Children.Cast<UIElement>()/*.Except(Headers)*/.Select((c, i) => new { index = i, child = c }))
            {
                var childIndex = IsItemsHost ? ItemContainerGenerator.IndexFromGeneratorPosition(new GeneratorPosition(element.index, 0)) : element.index;

                var ex = -HorizontalOffset;
                var ey = headerY + y + childIndex * _childSize.Height;

                element.child.Arrange(new Rect(ex, ey, ExtentWidth, element.child.DesiredSize.Height));
                SetZIndex(element.child, 0);
            }

            return finalSize;
        }

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            RemoveInternalChildRange(0 /*Headers.Count*/, Children.Count /* - Headers.Count*/);
            ItemContainerGenerator.RemoveAll();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateMeasure();
        }

        private Size MeasureChildSize(Size availableSize)
        {
            if (IsItemsHost)
            {
                var children = this.InternalChildren;
                using (ItemContainerGenerator.StartAt(ItemContainerGenerator.GeneratorPositionFromIndex(0), GeneratorDirection.Forward, true))
                {
                    var newChild = false;
                    var child = ItemContainerGenerator.GenerateNext(out newChild) as UIElement;
                    if (newChild)
                    {
                        AddInternalChild(child);
                        ItemContainerGenerator.PrepareItemContainer(child);
                    }

                    if (child != null)
                    {
                        child.Measure(availableSize);
                        return child.DesiredSize;
                    }
                }
            }
            else if (Children.Count > Headers.Count)
            {
                Children[Headers.Count].Measure(availableSize);
                return Children[Headers.Count].DesiredSize;
            }

            return Size.Empty;
        }

        private void GenerateItems(int firstVisibleItemIndex, int lastVisibleItemIndex)
        {
            var children = this.InternalChildren;
            var position = ItemContainerGenerator.GeneratorPositionFromIndex(firstVisibleItemIndex);
            var childIndex = position.Offset == 0 ? position.Index : position.Index + 1;
            using (ItemContainerGenerator.StartAt(position, GeneratorDirection.Forward, true))
            {
                for (var itemIndex = firstVisibleItemIndex; itemIndex <= lastVisibleItemIndex; itemIndex++, childIndex++)
                {
                    var newChild = false;
                    var child = ItemContainerGenerator.GenerateNext(out newChild) as UIElement;
                    if (newChild)
                    {
                        //if (childIndex >= (Children.Count - Headers.Count))
                        if (childIndex >= (Children.Count))
                        {
                            AddInternalChild(child);
                        }
                        else
                        {
                            //InsertInternalChild(childIndex + Headers.Count, child);
                            InsertInternalChild(childIndex, child);
                        }

                        ItemContainerGenerator.PrepareItemContainer(child);
                    }
                }
            }
        }

        private void CleanUpItems(int firstVisibleItemIndex, int lastVisibleItemIndex)
        {
            var children = this.InternalChildren;
            var generator = this.ItemContainerGenerator;

            for (int i = children.Count - 1; i >= /* Headers.Count */0; i--)
            {
                // Map a child index to an item index by going through a generator position
                GeneratorPosition childGeneratorPos = new GeneratorPosition(i /* - Headers.Count*/, 0);
                int itemIndex = generator.IndexFromGeneratorPosition(childGeneratorPos);
                if (itemIndex > 0 && (itemIndex < firstVisibleItemIndex || itemIndex > lastVisibleItemIndex))
                {
                    generator.Remove(childGeneratorPos, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        private void SetVerticalOffset(int childIndex)
        {
            var scrollToOffset = childIndex * _childSize.Height + _expandedHeaderSize.Height - _collapsedHeaderSize.Height - ViewportHeight / 2;
            if (ExtentHeight >= scrollToOffset)
            {
                SetVerticalOffset(scrollToOffset);
            }
        }
    }
}
