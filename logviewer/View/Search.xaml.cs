using logviewer.charts;
using logviewer.Controls;
using logviewer.Interfaces;
using logviewer.core;
using logviewer.ViewModel;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace logviewer.View
{
    /// <summary>
    /// Interaktionslogik für Search.xaml
    /// </summary>
    [ExportPageView(typeof(Search), typeof(SearchViewModel), typeof(Model.SearchContext), 10)]
    public partial class Search : UserControl, IPageView
    {
        public static readonly DependencyProperty ChartViewerProperty =
            DependencyProperty.Register("ChartViewer", typeof(ChartViewer), typeof(Search), new PropertyMetadata(null));

        private bool _detailsOpened = false;
        
        public Search()
        {
            InitializeComponent();
        }

        public ChartViewer ChartViewer
        {
            get { return (ChartViewer)GetValue(ChartViewerProperty); }
            set { SetValue(ChartViewerProperty, value); }
        }

        private static T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DetailsPopup.IsOpen = false;
            if (DataContext is SearchViewModel viewModel && !string.IsNullOrEmpty(viewModel.UserQuery) && sender is ListViewItem element && element.Content is ILogItem item)
            {
                var details = viewModel.GetDetailList(item);
                if (details.Count > 0)
                {
                    DetailsList.ItemsSource = details;
                    DetailsPopup.Width = element.ActualWidth;
                    DetailsPopup.PlacementTarget = element;
                    DetailsPopup.IsOpen = true;
                    e.Handled = true;
                    _detailsOpened = true;
                }
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            DetailsPopup.IsOpen = false;
            if (DataContext is SearchViewModel viewModel && ListView.SelectedItem is ILogItem item)
            {
                var index = viewModel.GetDetailIndex(item);
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    var model = new Model.SearchContext(string.Empty, string.Empty, Model.VisualizationAxisType.Linear, string.Empty, new List<Model.SearchContext.Visualization>(), null);
                    var window = this.FindParent<MainWindow>();
                    if (window.DataContext is MainViewModel mvm)
                    {
                        mvm.OpenTabCommand.Execute(model);
                    }
                }
                else
                {
                    viewModel.UserQuery = string.Empty;
                    await viewModel.Update();
                    if (GetVisualChild<VirtualizingHeaderPanel>(ListView) is VirtualizingHeaderPanel header)
                    {
                        ListView.SelectedIndex = index;
                        header.ScrollToIndex(index);
                    }
                }
            }
        }

        private void ResetZoomAndPanButton_Click(object sender, RoutedEventArgs e)
        {
            ChartViewer.ResetZoomAndPan();
        }

        private void ChartViewer_Loaded(object sender, RoutedEventArgs e)
        {
            ChartViewer = sender as ChartViewer;
            ChartViewer.PointSelectedCommand = new DelegateCommand<int>(ChartPointSelected);
        }

        private void ChartPointSelected(int index)
        {
            if (GetVisualChild<VirtualizingHeaderPanel>(ListView) is VirtualizingHeaderPanel header)
            {
                ListView.SelectedIndex = index;
                header.ScrollToIndex(index);
                ListView.InvalidateMeasure();
            }
        }

        private void DetailsPopup_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_detailsOpened)
            {
                DetailsPopup.IsOpen = false;
            }

            _detailsOpened = false;
        }
    }
}
