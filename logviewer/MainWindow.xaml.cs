using logviewer.Interfaces;
using logviewer.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

namespace logviewer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    [Export]
    public partial class MainWindow : Window
    {
        [ImportingConstructor]
        public MainWindow(MainViewModel viewModel, [ImportMany] IEnumerable<Lazy<IPageView, IPageViewMetadata>> pages)
        {
            InitializeComponent();

            // set the data context as viewmodel
            DataContext = viewModel;
            
            // create data templates for the views
            foreach (var t in pages.Select(CreateTemplate))
            {
                TabControl.Resources.Add(t.DataTemplateKey, t);
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Maximized)
            {
                MaximizeBorder.BorderThickness = new Thickness(8);
            }
            else
            {
                MaximizeBorder.BorderThickness = new Thickness(0);
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private DataTemplate CreateTemplate(Lazy<IPageView, IPageViewMetadata> arg)
        {
            const string xamlTemplate = "<DataTemplate DataType=\"{{x:Type vm:{0}}}\"><v:{1} /></DataTemplate>";
            var xaml = String.Format(xamlTemplate, arg.Metadata.ViewModel.Name, arg.Metadata.View.Name, arg.Metadata.ViewModel.Namespace, arg.Metadata.View.Namespace);

            var context = new ParserContext();

            context.XamlTypeMapper = new XamlTypeMapper(new string[0]);
            context.XamlTypeMapper.AddMappingProcessingInstruction("vm", arg.Metadata.ViewModel.Namespace, arg.Metadata.ViewModel.Assembly.FullName);
            context.XamlTypeMapper.AddMappingProcessingInstruction("v", arg.Metadata.View.Namespace, arg.Metadata.View.Assembly.FullName);

            context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            context.XmlnsDictionary.Add("vm", "vm");
            context.XmlnsDictionary.Add("v", "v");

            var template = (DataTemplate)XamlReader.Parse(xaml, context);
            return template;
        }
        
        private void HyperlinkCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is string url)
            {
                Process.Start(url);
            }
        }
    }
}
