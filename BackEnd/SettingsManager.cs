using MWP.DatatypesAndExtensions;
using Xamarin.Essentials;

namespace MWP.BackEnd
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
            _shouldUseChromaprintAtDownload = new BoolSetting(ShareName, "ShouldUseChromaprintAtDownload", true);
            _shouldUseChromaprintAtDiscover = new BoolSetting(ShareName, "ShouldUseChromaprintAtDiscover", true);
            _canUseNetwork = new IntSetting(ShareName, "canUseNetwork", (int)CanUseNetworkState.None);
            _defaultDownloadAction = new IntSetting(ShareName, "defaultDownloadAction", (int)DownloadActions.DownloadOnly);
            _checkUpdates = new IntSetting(ShareName, "checkUpdates", (int)AutoUpdate.NoState);
        }

        private static Setting<bool> _shouldUseChromaprintAtDownload = new BoolSetting(ShareName, "ShouldUseChromaprintAtDownload", true);
        public static bool ShouldUseChromaprintAtDownload
        {
            get => _shouldUseChromaprintAtDownload.Value;
            set => _shouldUseChromaprintAtDownload.Value = value;
        }

        private static Setting<bool> _shouldUseChromaprintAtDiscover = new BoolSetting(ShareName, "ShouldUseChromaprintAtDiscover", true);
        public static bool ShouldUseChromaprintAtDiscover
        {
            get => _shouldUseChromaprintAtDiscover.Value;
            set => _shouldUseChromaprintAtDiscover.Value = value;
        }
        
        
        private static Setting<int> _canUseNetwork = new IntSetting(ShareName, "canUseNetwork", (int)CanUseNetworkState.None);
        public static CanUseNetworkState CanUseNetwork
        {
            get => (CanUseNetworkState)_canUseNetwork.Value;
            set => _canUseNetwork.Value = (int)value;
        }
        
        //TODO: set to false
        private static Setting<bool> _canUseWan = new BoolSetting(ShareName, "canUseWan", true);
        public static bool CanUseWan
        {
            get => _canUseWan.Value;
            set => _canUseWan.Value = value;
        }
        
        private static Setting<int> _wanPort = new IntSetting(ShareName, "wanPort", 8010);
        public static int WanPort
        {
            get => _wanPort.Value;
            set
            {
                _wanPort.Value = value switch
                {
                    < 1024 => 1024,
                    > 65535 => 65535,
                    _ => value
                };
            }
        }

        //TODO: you don't use default download action, remove?
        private static Setting<int> _defaultDownloadAction = new IntSetting(ShareName, "defaultDownloadAction", (int)DownloadActions.DownloadOnly);
        public static DownloadActions DefaultDownloadAction
        {
            get => (DownloadActions)_defaultDownloadAction.Value;
            set => _defaultDownloadAction.Value = (int)value;
        }
        
        private static Setting<int> _checkUpdates = new IntSetting(ShareName, "checkUpdates", (int)AutoUpdate.NoState);
        public static AutoUpdate CheckUpdates
        {
            get => (AutoUpdate)_checkUpdates.Value;
            set => _checkUpdates.Value = (int)value;
        }
    }
}