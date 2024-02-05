using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MWP.BackEnd.Network;
using MWP.DatatypesAndExtensions;
using MWP.Helpers;
using Xamarin.Essentials;

namespace MWP.BackEnd
{
    internal static class SettingsManager
    {
        private const string ShareName = "AssPainSharedPreferences";

        private static string GetDefaultExcludedPaths()
        {
            List<string> paths = new List<string>
            {
                "Android",
                "sound_recorder",
                "Notifications",
                "Recordings",
                "Ringtones",
                "MIUI",
                "Alarms"
            }; 
            return string.Join(';', paths.Select(path => $"{FileManager.Root}{System.IO.Path.DirectorySeparatorChar}{path}").ToArray());
        }
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
                    if (val)
                    {
                            new Thread(NetworkManager.Listener).Start();
                    }
                }, "Enabling this will allow other devices on network to see your device"),
                
                ("Check for updates", () => CheckUpdates == AutoUpdate.Requested, (val) =>
                {
                    CheckUpdates = val
                        ? AutoUpdate.Requested
                        : AutoUpdate.Forbidden;
                }, null),
                
                ("Move and organize songs to Music folder", () => MoveFiles == MoveFilesEnum.Yes, (val) =>
                {
                    MoveFiles = val
                        ? MoveFilesEnum.Yes
                        : MoveFilesEnum.No;
                }, "Will change location of mp3 files to Music folder based on metadata (ID3v2 tags)"),
            };
        }

        public static List<(string name, Func<int> read, Action<int> write, Dictionary<string, int>? mapping, string? remark)> GetIntSettings()
        {
            return new List<(string name, Func<int> read, Action<int> write, Dictionary<string, int>? mapping, string? remark)> {
                ("Discover missing metadata", () => (int)ShouldUseChromaprintAtDiscover, (val) => { ShouldUseChromaprintAtDiscover = (UseChromaprint)val; },
                    new Dictionary<string, int> {
                        {"Disable", (int)UseChromaprint.No},
                        {"Manual", (int)UseChromaprint.Manual},
                        {"Automatic", (int)UseChromaprint.Automatic}
                    }, "Setting to Automatic can produce weird Titles and names"
                ),
                
            };
        }
        
        public static List<(string name, Func<string> read, Action<string> write, string? remark)> GetStringSettings()
        {
            return new List<(string name, Func<string> read, Action<string> write, string? remark)> {
                ("Change device name", () => Hostname, (val) => { Hostname = val; },
                    "Changing this will require you to reset trusted hosts on all devices that communicate with this device and you will need to reset trusted hosts on your device"
                ),
                
            };
        }

        private static void RegisterSettings()
        {
            _hostname = new StringSetting(ShareName, "Hostname", DeviceInfo.Name);
            _moveFiles = new IntSetting(ShareName, "MoveFiles", (int)MoveFilesEnum.None);
            _shouldUseChromaprintAtDownload = new BoolSetting(ShareName, "ShouldUseChromaprintAtDownload", true);
            _shouldUseChromaprintAtDiscover = new IntSetting(ShareName, "ShouldUseChromaprintAtDiscover", (int)UseChromaprint.None);
            _canUseNetwork = new IntSetting(ShareName, "canUseNetwork", (int)CanUseNetworkState.None);
            _defaultDownloadAction = new IntSetting(ShareName, "defaultDownloadAction", (int)DownloadActions.DownloadOnly);
            _checkUpdates = new IntSetting(ShareName, "checkUpdates", (int)AutoUpdate.NoState);
            _excludedPaths = new StringSetting(ShareName, "ShouldUseChromaprintAtDownload", GetDefaultExcludedPaths());
        }
        
        private static Setting<string> _hostname = new StringSetting(ShareName, "Hostname", DeviceInfo.Name);
        public static string Hostname
        {
            get => _hostname.Value;
            set => _hostname.Value = value;
        }
        
        private static Setting<int> _moveFiles = new IntSetting(ShareName, "MoveFiles", (int)MoveFilesEnum.None);
        public static MoveFilesEnum MoveFiles
        {
            get => (MoveFilesEnum)_moveFiles.Value;
            set => _moveFiles.Value = (int)value;
        }
        
        private static Setting<string> _excludedPaths = new StringSetting(ShareName, "ShouldUseChromaprintAtDownload", GetDefaultExcludedPaths());
        public static List<string> ExcludedPaths
        {
            get
            {
#if DEBUG
                MyConsole.WriteLine($"Excluded paths {_excludedPaths.Value}");
#endif
                return _excludedPaths.Value.Split(';').ToList();
            }
            set
            {
#if DEBUG
                MyConsole.WriteLine($"New Excluded paths {string.Join(';', value.ToArray())}");
#endif
                _excludedPaths.Value = string.Join(';', value.ToArray());
                FileManager.ReDiscoverFiles();
            }
        }

        private static Setting<bool> _shouldUseChromaprintAtDownload = new BoolSetting(ShareName, "ShouldUseChromaprintAtDownload", true);
        public static bool ShouldUseChromaprintAtDownload
        {
            get => _shouldUseChromaprintAtDownload.Value;
            set => _shouldUseChromaprintAtDownload.Value = value;
        }

        private static Setting<int> _shouldUseChromaprintAtDiscover = new IntSetting(ShareName, "ShouldUseChromaprintAtDiscover", (int)UseChromaprint.None);
        public static UseChromaprint ShouldUseChromaprintAtDiscover
        {
            get => (UseChromaprint)_shouldUseChromaprintAtDiscover.Value;
            set => _shouldUseChromaprintAtDiscover.Value = (int)value;
        }
        
        
        private static Setting<int> _canUseNetwork = new IntSetting(ShareName, "canUseNetwork", (int)CanUseNetworkState.None);
        public static CanUseNetworkState CanUseNetwork
        {
            get => (CanUseNetworkState)_canUseNetwork.Value;
            set => _canUseNetwork.Value = (int)value;
        }
        
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