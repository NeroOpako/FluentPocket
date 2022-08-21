using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Notifications;

namespace FluentPocket.Handlers
{
    internal class Utils
    {
        internal static int UnixTimeStamp() => (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        internal static bool HasInternet => Microsoft.Toolkit.Uwp.Connectivity.NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable;

        internal static void CopyToClipboard(string text)
        {
            var pkg = new Windows.ApplicationModel.DataTransfer.DataPackage();
            pkg.SetText(text??"");
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(pkg);
        }

        internal static void ToastIt(string str1, string str2)
        {
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            var toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(str1));
            toastTextElements[1].AppendChild(toastXml.CreateTextNode(str2));
            // Set the duration on the toast
            var toastNode = toastXml.SelectSingleNode("/toast");
            ((XmlElement)toastNode)?.SetAttribute("duration", "long");
            // Create the actual toast object using this toast specification.
            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

    }

}
