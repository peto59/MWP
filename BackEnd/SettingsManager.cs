using Xamarin.Essentials;

namespace Ass_Pain
{
    internal static class SettingsManager
    {
        private const string ShareName = "AssPainSharedPreferences";

        public static void ResetSettings()
        {
            Preferences.Clear(ShareName);
            RegisterSettings();
        }

        private static void RegisterSettings()
        {
            shouldUseChromaprintAtDownload = new BoolSetting(ShareName, "ShouldUseChromaprintAtDownload", true);
            canUseNetwork = new IntSetting(ShareName, "canUseNetwork", (int)CanUseNetworkState.None);
            defaultDownloadAction = new IntSetting(ShareName, "defaultDownloadAction", (int)DownloadActions.DownloadOnly);
            checkUpdates = new IntSetting(ShareName, "checkUpdates", (int)AutoUpdate.NoState);
        }

        private static Setting<bool> shouldUseChromaprintAtDownload = new BoolSetting(ShareName, "ShouldUseChromaprintAtDownload", true);
        public static bool ShouldUseChromaprintAtDownload
        {
            get => shouldUseChromaprintAtDownload.Value;
            set => shouldUseChromaprintAtDownload.Value = value;
        }

        /*private static Setting<bool> shouldUseChromaprintAtDiscover = new BoolSetting(ShareName, "ShouldUseChromaprintAtDiscover", false);
        public static bool ShouldUseChromaprintAtDiscover
        {
            get => shouldUseChromaprintAtDiscover.Value;
            set => shouldUseChromaprintAtDiscover.Value = value;
        }*/
        
        private static Setting<int> canUseNetwork = new IntSetting(ShareName, "canUseNetwork", (int)CanUseNetworkState.None);
        public static CanUseNetworkState CanUseNetwork
        {
            get => (CanUseNetworkState)canUseNetwork.Value;
            set => canUseNetwork.Value = (int)value;
        }
        
        //TODO: set to false
        private static Setting<bool> canUseWan = new BoolSetting(ShareName, "canUseWan", true);
        public static bool CanUseWan
        {
            get => canUseWan.Value;
            set => canUseWan.Value = value;
        }
        
        private static Setting<int> wanPort = new IntSetting(ShareName, "wanPort", 8010);
        public static int WanPort
        {
            get => wanPort.Value;
            set
            {
                wanPort.Value = value switch
                {
                    < 1024 => 1024,
                    > 65535 => 65535,
                    _ => value
                };
            }
        }

        //TODO: you don't use default download action, remove?
        private static Setting<int> defaultDownloadAction = new IntSetting(ShareName, "defaultDownloadAction", (int)DownloadActions.DownloadOnly);
        public static DownloadActions DefaultDownloadAction
        {
            get => (DownloadActions)defaultDownloadAction.Value;
            set => defaultDownloadAction.Value = (int)value;
        }
        
        private static Setting<int> checkUpdates = new IntSetting(ShareName, "checkUpdates", (int)AutoUpdate.NoState);
        public static AutoUpdate CheckUpdates
        {
            get => (AutoUpdate)checkUpdates.Value;
            set => checkUpdates.Value = (int)value;
        }
    }
}