using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace logviewer.Controls
{
    /// <summary>
    /// Interaktionslogik für DialogContent.xaml
    /// </summary>
    public partial class DialogLayout : UserControl
    {
        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register("HeaderContent", typeof(DependencyObject), typeof(DialogLayout), new PropertyMetadata(null));

        public static readonly DependencyProperty DialogContentProperty =
            DependencyProperty.Register("DialogContent", typeof(DependencyObject), typeof(DialogLayout), new PropertyMetadata(null));

        public static readonly DependencyProperty ActionContentProperty =
            DependencyProperty.Register("ActionContent", typeof(DependencyObject), typeof(DialogLayout), new PropertyMetadata(null));

        public DialogLayout()
        {
            InitializeComponent();
        }

        public DependencyObject HeaderContent
        {
            get { return (DependencyObject)GetValue(HeaderContentProperty); }
            set { SetValue(HeaderContentProperty, value); }
        }

        public DependencyObject DialogContent
        {
            get { return (DependencyObject)GetValue(DialogContentProperty); }
            set { SetValue(DialogContentProperty, value); }
        }

        public DependencyObject ActionContent
        {
            get { return (DependencyObject)GetValue(ActionContentProperty); }
            set { SetValue(ActionContentProperty, value); }
        }
    }
}
