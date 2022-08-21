using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp;
using PocketSharp.Models;
using FluentPocket.Handlers;
using FluentPocket.Views.Dialog;
using ReadSharp;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using NavigationViewItem = Windows.UI.Xaml.Controls.NavigationViewItem;

namespace FluentPocket.ViewModels
{
    internal class MainContentViewModel : INotifyPropertyChanged
    {
        public readonly IncrementalLoadingCollection<PocketIncrementalSource.Articles, PocketItem> ArticlesList
            = new IncrementalLoadingCollection<PocketIncrementalSource.Articles, PocketItem>();
        public readonly IncrementalLoadingCollection<PocketIncrementalSource.Archives, PocketItem> ArchivesList
            = new IncrementalLoadingCollection<PocketIncrementalSource.Archives, PocketItem>();
        public readonly IncrementalLoadingCollection<PocketIncrementalSource.Favorites, PocketItem> FavoritesList
            = new IncrementalLoadingCollection<PocketIncrementalSource.Favorites, PocketItem>();
        public ObservableCollection<PocketItem> SearchList = new ObservableCollection<PocketItem>();
        internal PocketHandler PocketHandler => PocketHandler.GetInstance();
        public event PropertyChangedEventHandler PropertyChanged;
        private ICommand _addArticle;
        private ICommand _refreshArticles;
        private bool _listIsLoading;
        public int PivotListSelectedIndex { get; set; }
        public NavigationViewItem NavigationSelectedItem { get; set; }
        protected virtual void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public bool ListIsLoading
        {
            get => _listIsLoading;
            //|| ArticlesList.IsLoading || ArchivesList.IsLoading || FavoritesList.IsLoading;
            set
            {
                _listIsLoading = value;
                OnPropertyChanged(nameof(ListIsLoading));
            }
        }

        public ObservableCollection<PocketItem> CurrentList()
        {
            if (NavigationSelectedItem != null)
            {
                switch (NavigationSelectedItem.Tag)
                {
                    case "1":
                        return FavoritesList;
                    case "2":
                        return ArchivesList;
                    case "3":
                        return SearchList;
                    default:
                        return ArticlesList;
                }
            }
            return ArticlesList;
        }

        public async Task SearchCommand(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return;
            Logger.Logger.L("Search");
            q = Uri.EscapeUriString(q);
            SearchList.Clear();
            var task = q[0] == '#'
                ? PocketHandler.GetListAsync(State.all, null, q.Substring(1), null, 40, 0)
                : PocketHandler.GetListAsync(State.all, null, null, q, 40, 0);
            ListIsLoading = true;
            foreach (var pocketItem in await task)
                SearchList.Add(pocketItem);
            OnPropertyChanged(nameof(SearchList));
            ListIsLoading = false;
        }

        internal ICommand AddArticle =>
            _addArticle ?? (_addArticle = new SimpleCommand(async param =>
            {
                if (!Utils.HasInternet)
                {
                    await UiUtils.ShowDialogAsync("You need to connect to the internet first");
                    return;
                }

                ContentDialog dialog = new ContentDialog();
                dialog.Content = new AddDialog { PrimaryBtnText = "Add" };
                dialog.PrimaryButtonText = "Add";
                dialog.PrimaryButtonClick += ContentDialog_PrimaryButtonClickAsync;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                var result = await dialog.ShowAsync();
            }));

        private async void ContentDialog_PrimaryButtonClickAsync(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ((AddDialog)sender?.Content).ContentDialog_PrimaryButtonClick(sender, args);
            if (((AddDialog)sender?.Content).PocketItem == null) return;
            ArticlesList.Insert(0, ((AddDialog)sender?.Content).PocketItem);
            await PocketHandler.PutItemInCache(0, ((AddDialog)sender?.Content).PocketItem);
            RefreshArticles.Execute(null);
        }

        internal void ShareArticle(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            request.Data.SetText(PocketHandler?.CurrentPocketItem?.Uri?.ToString() ?? "");
            request.Data.Properties.Title = "Shared by FluentPocket";
        }

        public async Task ToggleArchiveArticleAsync(PocketItem pocketItem, bool isArchive)
        {
            if (pocketItem == null) return;
            try
            {
                if (isArchive) // Want to add
                {
                    await PocketHandler.Client.Unarchive(pocketItem);
                    NotificationHandler.InAppNotification("Added", 2000);
                    if (ArticlesList.Count > 0 && ArticlesList[0] != pocketItem) ArticlesList.Insert(0, pocketItem);
                    ArchivesList.Remove(pocketItem);
                }
                else // Want to Archive
                {
                    await PocketHandler.ArchiveArticle(pocketItem);
                    if (ArchivesList.Count > 0 && ArchivesList[0] != pocketItem) ArchivesList.Insert(0, pocketItem);
                    ArticlesList.Remove(pocketItem);
                    NotificationHandler.InAppNotification("Archived", 2000);
                }
            }
            catch (Exception e) { NotificationHandler.InAppNotification(e.Message, 2000); }
        }

        public async Task DeleteArticleAsync(PocketItem pocketItem)
        {
            if (pocketItem == null) return;
            await PocketHandler.DeleteArticle(pocketItem);
            CurrentList()?.Remove(pocketItem);
            NotificationHandler.InAppNotification("Deleted", 2000);
        }

        public async Task ToggleFavoriteArticleAsync(PocketItem pocketItem)
        {
            if (pocketItem == null) return;
            if (pocketItem.IsFavorite)
            {
                await PocketHandler.GetInstance().Client.Unfavorite(pocketItem);
                if (FavoritesList.Count != 0) FavoritesList.Remove(pocketItem);
                NotificationHandler.InAppNotification("Remove from Favorite", 2000);
            }
            else
            {
                await PocketHandler.GetInstance().Client.Favorite(pocketItem);
                if (FavoritesList.Count != 0) FavoritesList.Add(pocketItem);
                NotificationHandler.InAppNotification("Saved as Favorite", 2000);
            }
        }

        public async Task ItemTappedAsync(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var item = ((FrameworkElement)e.OriginalSource).DataContext as PocketItem;
            await Launcher.LaunchUriAsync(item?.Uri);
        }

        public void ItemRightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            var item = ((FrameworkElement)e.OriginalSource).DataContext as PocketItem;
            PocketHandler.CurrentPocketItem = item;
            var flyout = new MenuFlyout();
            var el = new MenuFlyoutItem { Text = item.IsFavorite ? "Un-Favorite" : "Favorite", Icon = new SymbolIcon(item.IsFavorite ? Symbol.UnFavorite : Symbol.Favorite) };
            el.Click += async (sen, ee) =>
            {
                await ToggleFavoriteArticleAsync(item);
                item.IsFavorite = !item.IsFavorite;
                OnPropertyChanged(nameof(item));
                RefreshArticles.Execute(item);
            };
            flyout?.Items?.Add(el);
            el = new MenuFlyoutItem { Text = "Share", Icon = new SymbolIcon(Symbol.Share) };
            el.Click += (sen, ee) =>
            {
                DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested += (s, args) =>
                {
                    var request = args.Request;
                    request.Data.SetWebLink(new Uri(item.Uri.AbsoluteUri));
                    request.Data.Properties.Title = "Share an article";
                };
                DataTransferManager.ShowShareUI();
            };
            flyout?.Items?.Add(el);
            el = new MenuFlyoutItem { Text = "Edit Tags", Icon = new SymbolIcon(Symbol.Edit) };
            el.Click += async (sen, ee) =>
            {
                if (Utils.HasInternet)
                {
                    ContentDialog dialog = new ContentDialog();
                    dialog.Content = new AddDialog { PrimaryBtnText = "Save", PocketItem = item };
                    dialog.PrimaryButtonText = "Save";
                    dialog.PrimaryButtonClick += ContentDialog_PrimaryButtonClickAsync;
                    dialog.CloseButtonText = "Cancel";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    var result = await dialog.ShowAsync();
                    OnPropertyChanged(nameof(item));
                }
                else await UiUtils.ShowDialogAsync("You need to connect to the internet first");
            };
            flyout?.Items?.Add(el);
            el = new MenuFlyoutItem { Text = "Copy Link", Icon = new SymbolIcon(Symbol.Copy) };
            el.Click += (sen, ee) =>
            {
                Utils.CopyToClipboard(item?.Uri?.AbsoluteUri);
                NotificationHandler.InAppNotification("Copied", 2000);
            };
            flyout?.Items?.Add(el);
            el = new MenuFlyoutItem { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) };
            el.Click += async (sen, ee) =>
            {
                await DeleteArticleAsync(item);
                RefreshArticles.Execute(item);
            };
            flyout?.Items?.Add(el);
            el = new MenuFlyoutItem
            {
                Text = !item.IsArchive ? "Archive" : "Un-Archive",
                Icon = new FontIcon { Glyph = !item.IsArchive ? "\uE81C" : "\uE777" }
            };
            el.Click += async (sen, ee) =>
            {
                await ToggleArchiveArticleAsync(item, item.IsArchive);
                RefreshArticles.Execute(item);
            };
                flyout?.Items?.Insert(0, el);
            if (sender is Button parent) flyout.ShowAt(parent, e.GetPosition(parent));
        }

        internal ICommand RefreshArticles =>
           _refreshArticles ?? (_refreshArticles = new SimpleCommand(async param =>
           {
               if (!Utils.HasInternet)
               {
                   await UiUtils.ShowDialogAsync("You need to connect to the internet first");
                   return;
               }
               await ArticlesList.RefreshAsync();
               await ArchivesList.RefreshAsync();
               await FavoritesList.RefreshAsync();
               SearchList.Clear();
           }));
    }
}
