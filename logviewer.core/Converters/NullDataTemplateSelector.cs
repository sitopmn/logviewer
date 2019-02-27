using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace logviewer.core
{
    public class NullDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NullTemplate
        {
            get;
            set;
        }

        public DataTemplate Template
        {
            get;
            set;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
            {
                return NullTemplate;
            }
            else
            {
                return Template;
            }
        }
    }
}
