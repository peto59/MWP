using System;
using System.Collections.Generic;
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

        public static List<(string name, Func<bool> read, Action<bool> write, string? remark)> GetBoolSettings()
        {
            return new List<(string name, Func<bool> read, Action<bool> write, string? remark)> {
                ("Can use network", () => CanUseNetwork == CanUseNetworkState.Allowed, (val) =>
                {
                    CanUseNetwork = val
                        ? CanUseNetworkState.Allowed
                        : CanUseNetworkState.Rejected;
                }, "Enabling this will allow other devices on network to see your device"),
                
                ("Check for updates", () => CheckUpdates == AutoUpdate.Requested, (val) =>
                {
                    CheckUpdates = val
                        ? AutoUpdate.Requested
                        : AutoUpdate.Forbidden;
                }, null),
            };
        }

        public static List<(string name, Func<int> read, Action<int> write, Dictionary<string, int>? mapping, string? remark)> GetIntSettings()
        {
            return new List<(string name, Func<int> read, Action<int> write, Dictionary<string, int>? mapping, string? remark)> {
                ("Can use network", () => (int)CanUseNetwork, (val) => { CanUseNetwork = (CanUseNetworkState)val; },
                    new Dictionary<string, int> {
                        {"Allowed", (int)CanUseNetworkState.Allowed},
                        {"Rejected", (int)CanUseNetworkState.Rejected}
                    }, "Warning: this is test message"
                ),
                
            };
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
        private static Setting<bool> _canUseWan = new BoolSetting(ShareName, "canUseWan", false);
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