using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace logviewer.View
{
    /// <summary>
    /// Interaktionslogik für Navigation.xaml
    /// </summary>
    public partial class Navigation : UserControl
    {
        public Navigation()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(BookmarksItemsControl.ItemsSource);
            if (view != null)
            {
                view.Filter += m => string.IsNullOrEmpty(SearchBookmarkTextBox.Text) || ((Model.Context)m).Title.IndexOf(SearchBookmarkTextBox.Text, StringComparison.CurrentCultureIgnoreCase) >= 0;     
            }
        }

        private void SearchBookmarkTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SearchBookmarkToggle.IsChecked = false;
            }
            else
            {
                CollectionViewSource.GetDefaultView(BookmarksItemsControl.ItemsSource).Refresh();
            }
        }

        private void SearchBookmarkToggle_Checked(object sender, RoutedEventArgs e)
        {
            ((Storyboard)SearchBookmarkGrid.FindResource("SearchBookmarkShowAnimation")).Begin();
        }

        private void SearchBookmarkToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ((Storyboard)SearchBookmarkGrid.FindResource("SearchBookmarkHideAnimation")).Begin();
            SearchBookmarkTextBox.Text = string.Empty;
            CollectionViewSource.GetDefaultView(BookmarksItemsControl.ItemsSource).Refresh();
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            Keyboard.Focus(SearchBookmarkTextBox);
        }

        private void ShowHelpButton_Click(object sender, RoutedEventArgs e)
        {
            DrawerHost.CloseDrawerCommand.Execute(Dock.Left, (Button)sender);
        }
    }
}
