using System.Runtime.CompilerServices;
using Xamarin.Essentials;

namespace Ass_Pain
{
    internal static class SettingsManager
    {
        private const string ShareName = "AssPainSharedPreferences";


        private static readonly Setting<bool> shouldUseChromaprintAtDownload = new BoolSetting(ShareName, "ShouldUseChromaprintAtDownload", false);
        public static bool ShouldUseChromaprintAtDownload
        {
            get => shouldUseChromaprintAtDownload.Value;
            set => shouldUseChromaprintAtDownload.Value = value;
        }
        
        private static readonly Setting<bool> shouldUseMusicBrainzAtDownload = new BoolSetting(ShareName, "ShouldUseMusicBrainzAtDownload", false);
        public static bool ShouldUseMusicBrainzAtDownload
        {
            get => shouldUseMusicBrainzAtDownload.Value;
            set => shouldUseMusicBrainzAtDownload.Value = value;
        }
        
        private static readonly Setting<bool> shouldUseChromaprintAtDiscover = new BoolSetting(ShareName, "ShouldUseChromaprintAtDiscover", false);
        public static bool ShouldUseChromaprintAtDiscover
        {
            get => shouldUseChromaprintAtDiscover.Value;
            set => shouldUseChromaprintAtDiscover.Value = value;
        }
        
        private static readonly Setting<bool> shouldUseMusicBrainzAtDiscover = new BoolSetting(ShareName, "ShouldUseMusicBrainzAtDiscover", false);
        public static bool ShouldUseMusicBrainzAtDiscover
        {
            get => shouldUseMusicBrainzAtDiscover.Value;
            set => shouldUseMusicBrainzAtDiscover.Value = value;
        }
    }
    
    internal abstract class Setting<T>
    {
        protected string ShareName { get; }
        protected string Key { get; }
        protected T DefaultValue { get; }
        public abstract T Value { get; set; }
        protected Setting(string shareName, string key, T defaultValue)
        {
            ShareName = shareName;
            Key = key;
            DefaultValue = defaultValue;
        }
    }
    internal class StringSetting : Setting<string>
    {
        public StringSetting(string shareName, string key, string defaultValue) : base(shareName, key, defaultValue) {}

        public override string Value
        {
            get => Preferences.Get(Key, DefaultValue, ShareName);
            set => Preferences.Set(Key, value, ShareName);
        }
    }
    internal class BoolSetting : Setting<bool>
    {
        public BoolSetting(string shareName, string key, bool defaultValue) : base(shareName, key, defaultValue) {}

        public override bool Value
        {
            get => Preferences.Get(Key, DefaultValue, ShareName);
            set => Preferences.Set(Key, value, ShareName);
        }
    }

}