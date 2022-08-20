using System;
using FluentPocket.Handlers;
using Windows.Security.Authentication.Web;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FluentPocket.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage() => InitializeComponent();

        private async void Login_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = await PocketHandler.GetInstance().LoginUriAsync();
                var success = await Windows.System.Launcher.LaunchUriAsync(uri);
            }
            catch
            {
                var dialog = new MessageDialog("Error.");
                dialog.Commands.Add(new UICommand("Close"));
                await dialog.ShowAsync();
            }
        }
    }
}
