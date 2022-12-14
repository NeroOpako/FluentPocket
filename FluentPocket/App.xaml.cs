using System;
using System.Threading.Tasks;
using FluentPocket.Views;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using FluentPocket.Handlers;

namespace FluentPocket
{
    sealed partial class App : Application
    {
        internal static string Protocol = "fluentpocket://auth";
        internal static readonly bool DebugMode = System.Diagnostics.Debugger.IsAttached;

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
            CacheManager.CacheManager.Init(typeof(App));
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (!(Window.Current?.Content is Frame rootFrame))
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (e != null && e?.PrelaunchActivated != false) return;
            if (rootFrame.Content == null)
            {
                var handler = PocketHandler.GetInstance();
                handler.LoadCacheClient();
                rootFrame.Navigate(handler.Client == null ? typeof(LoginPage) : typeof(MainContent), e?.Arguments);
            }
            Window.Current.Activate();
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
            => throw new Exception("Failed to load Page " + e.SourcePageType.FullName);

        private void OnSuspending(object sender, SuspendingEventArgs e)
            => e.SuspendingOperation.GetDeferral().Complete();

        //OnShare
        protected override async void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            var shareOperation = args.ShareOperation;
            await Task.Factory.StartNew(async () =>
            {
                if (!shareOperation.Data.Contains(StandardDataFormats.WebLink)) return;
                await AddToPocketAsync((await shareOperation?.Data.GetWebLinkAsync()).AbsoluteUri, false);
                shareOperation.ReportCompleted();
            });
        }

        // Protocol & CMD
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            switch (args.Kind)
            {   
                case ActivationKind.Protocol://From CommandLine
                    {
                        var protocolArgs = args as ProtocolActivatedEventArgs;
                        Frame rootFrame = new Frame();
                        Window.Current.Content = rootFrame;
                        rootFrame.Navigate(typeof(LoginResultPage), protocolArgs.Uri);
                        break;
                    }
                case ActivationKind.CommandLineLaunch:
                    {
                        var arg = (args as CommandLineActivatedEventArgs)?.Operation?.Arguments ?? "";
                        if (arg.Length > 3 && Uri.IsWellFormedUriString(arg, UriKind.Absolute))
                            await AddToPocketAsync(arg);
                        else OnLaunched(null);
                        break;
                    }
            }
        }

        private static async Task AddToPocketAsync(string arg, bool exit = true)
        {
            try
            {
                var (item1, item2) = await PocketHandler.GetInstance().AddFromShare(new Uri(arg));
                Utils.ToastIt(item1, item2);
                if (exit) Current.Exit();
            }
            catch { }
        }
    }
}
