using Microsoft.Toolkit.Uwp.Helpers;
using FluentPocket.Models;

namespace FluentPocket.Handlers
{
    internal class SettingsHandler
    {
        public static Settings Settings { get; set; } = new Settings();

        public static void Load()
        {
            try
            {
                var temp = ApplicationDataStorageHelper.GetCurrent().Read<Settings>(Keys.Settings);
                if (temp != null) Settings = temp;
            }
            catch { }
        }
        public static void Save() => ApplicationDataStorageHelper.GetCurrent().Save(Keys.Settings, Settings);

        public static void Clear()
        {
            Settings = new Settings();
            Save();
        }
    }
}
