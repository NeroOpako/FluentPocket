using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FluentPocket.Handlers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using PocketSharp.Models;

namespace FluentPocket.Views.Dialog
{
    public sealed partial class AddDialog : Page
    {
        public string PrimaryBtnText { get; set; } = "Add";
        public PocketSharp.Models.PocketItem PocketItem { get; set; }
        private static PocketHandler PocketHandler => PocketHandler.GetInstance();
        public AddDialog() => InitializeComponent();
        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            ChipsList.AvailableChips = PocketHandler.Tags;
            if (PrimaryBtnText != "Save") return;
            UrlTextBox.Visibility = Visibility.Collapsed;
            if (PocketHandler?.CurrentPocketItem?.Tags == null) return;
           // foreach (var tag in PocketHandler.CurrentPocketItem.Tags) sl.Add(tag.Name);
            ChipsList.SelectedChips = PocketHandler.CurrentPocketItem.Tags.Select(t => t.Name);
            Bindings.Update();
        }

        public async void ContentDialog_PrimaryButtonClick(object sender, ContentDialogButtonClickEventArgs args)
        {
            if (PrimaryBtnText == "Save")
              {
                  try
                  {
                      if (!await PocketHandler.Client.ReplaceTags(PocketHandler.CurrentPocketItem, ChipsList.SelectedChips.ToArray())
                          .ConfigureAwait(true))
                          return;
                      NotificationHandler.InAppNotification("Tags get updated", 2000);
                      PocketHandler.CurrentPocketItem.Tags =
                          ChipsList.SelectedChips.Select(chip => new PocketTag { Name = chip });
                  }
                  catch { }
                  return;
              }

            try
            {
                var foo = PocketHandler.Client.Add(new Uri(UrlTextBox.Text.Trim()), ChipsList.SelectedChips.ToArray());
                PocketItem = await foo.ConfigureAwait(true);
                await PocketHandler.PutItemInCache(0, PocketItem);
            }
            catch { }

        }
    }
}