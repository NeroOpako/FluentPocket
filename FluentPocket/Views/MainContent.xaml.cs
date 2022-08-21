using System;
using PocketSharp.Models;
using FluentPocket.Handlers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using FluentPocket.ViewModels;
using Windows.ApplicationModel;
using Windows.System;

namespace FluentPocket.Views
{
    public sealed partial class MainContent : Page
    {
        private readonly MainContentViewModel _vm;

        private readonly string _versionString = $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";
        private PocketUser User => PocketHandler.GetInstance().User;
        public MainContent()
        {
            InitializeComponent();
            _vm = new MainContentViewModel();
            var uiUtils = new UiUtils();
            uiUtils.TitleBarButtonTransparentBackground(Application.Current.RequestedTheme == ApplicationTheme.Dark);
            Loaded += async (s, e) =>
            {
                // TODO move this stuff to repository
                if (!Utils.HasInternet)
                    foreach (var pocketItem in await _vm.PocketHandler.GetItemsCache())
                        _vm.ArticlesList.Add(pocketItem);
                // Logger.Logger.InitOnlineLogger(Keys.AppCenter);
                //Logger.Logger.SetDebugMode(App.DebugMode);
                nv.SelectedItem = nv.MenuItems[0];
            };

        }
        private async void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!Utils.HasInternet)
            {
                ErrorBar.IsOpen = true;
                return;
            }
            if (string.IsNullOrWhiteSpace(sender?.Text)) return;
            nv.SelectedItem = null;
            NavigationViewItem navigationSelectedItem = new NavigationViewItem();
            navigationSelectedItem.Tag = "3";
            navigationSelectedItem.Content = "Search results";
            _vm.NavigationSelectedItem = navigationSelectedItem;
            ArticlesListView.ItemsSource = _vm.SearchList;
            ArticlesListView.Visibility = Visibility.Visible;
            await _vm.SearchCommand(sender?.Text);
            SearchBox.Text = "";
        }
        private void ItemRightTapped(object sender, RightTappedRoutedEventArgs e)  {
            if (!Utils.HasInternet)
            {
                ErrorBar.IsOpen = true;
                return;
            }
            _vm.ItemRightTapped(sender, e);
        }

        private async void ItemTapped(object sender, TappedRoutedEventArgs e) {
            if (!Utils.HasInternet)
            {
                ErrorBar.IsOpen = true;
                return;
            }
            await _vm.ItemTappedAsync(sender, e);
        }

        private void AddArticleTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!Utils.HasInternet)
            {
                ErrorBar.IsOpen = true;
                return;
            }
            _vm.AddArticleTapped(sender, e);
        }

        private void RefreshArticlesTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!Utils.HasInternet)
            {
                ErrorBar.IsOpen = true;
                return;
            }
            _vm.RefreshArticlesTapped(sender, e);
        }

        private void NavigationView_SelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e) {
            _vm.NavigationSelectedItem = (NavigationViewItem) nv.SelectedItem;
            SearchBox.Text = "";
            _vm.SearchList.Clear();
            ArticlesListView.ItemsSource = _vm.CurrentList();
        }
        private async void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (!Utils.HasInternet)
            {
                ErrorBar.IsOpen = true;
                return;
            }

            ContentDialog dialog = new ContentDialog();
            dialog.Content = new TextBlock { Text = "Are you sure you want to logout?" };
            dialog.PrimaryButtonText = "Logout";
            dialog.PrimaryButtonClick += (s, ev) =>
            {
                PocketHandler.GetInstance().Logout();
                Frame?.Navigate(typeof(Views.LoginPage));
                Frame?.BackStack.Clear();
            };
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = ContentDialogButton.Primary;
            var result = await dialog.ShowAsync();
        }
        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            Akavache.BlobCache.LocalMachine.InvalidateAll();
            Akavache.BlobCache.LocalMachine.Vacuum();
        }

        private async void DevPageFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (!Utils.HasInternet)
            {
                ErrorBar.IsOpen = true;
                return;
            }
            await Launcher.LaunchUriAsync(new Uri("https://github.com/NeroOpako/FluentPocket"));
        }
    }
}