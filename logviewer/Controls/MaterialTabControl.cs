using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace logviewer.Controls
{
    /// <summary>
    /// Extended tab control for material design applications
    /// </summary>
    public class MaterialTabControl : TabControl
    {
        /// <summary>
        /// Dependency property for <see cref="BeforeTabStripContent"/>
        /// </summary>
        public static readonly DependencyProperty BeforeTabStripContentProperty =
            DependencyProperty.Register("BeforeTabStripContent", typeof(object), typeof(MaterialTabControl), new PropertyMetadata(null));

        /// <summary>
        /// Dependency property for <see cref="AfterTabStripContent"/>
        /// </summary>
        public static readonly DependencyProperty AfterTabStripContentProperty =
            DependencyProperty.Register("AfterTabStripContent", typeof(object), typeof(MaterialTabControl), new PropertyMetadata(null));

        /// <summary>
        /// Dependency property for <see cref="HeaderForeground"/>
        /// </summary>
        public static readonly DependencyProperty HeaderForegroundProperty =
            DependencyProperty.Register("HeaderForeground", typeof(Brush), typeof(MaterialTabControl), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content to display before the tab strip
        /// </summary>
        public object BeforeTabStripContent
        {
            get { return (object)GetValue(BeforeTabStripContentProperty); }
            set { SetValue(BeforeTabStripContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the content to display after the tab strip
        /// </summary>
        public object AfterTabStripContent
        {
            get { return (object)GetValue(AfterTabStripContentProperty); }
            set { SetValue(AfterTabStripContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the foreground brush for header items
        /// </summary>
        public Brush HeaderForeground
        {
            get { return (Brush)GetValue(HeaderForegroundProperty); }
            set { SetValue(HeaderForegroundProperty, value); }
        }
    }
}
