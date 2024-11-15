namespace MWP.DatatypesAndExtensions
{
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
#if ANDROID
            get => Preferences.Get(Key, DefaultValue, ShareName);
            set => Preferences.Set(Key, value, ShareName);
#elif LINUX
            get;
            set;
#endif
        }
    }
    internal class BoolSetting : Setting<bool>
    {
        public BoolSetting(string shareName, string key, bool defaultValue) : base(shareName, key, defaultValue) {}

        public override bool Value
        {
#if ANDROID
            get => Preferences.Get(Key, DefaultValue, ShareName);
            set => Preferences.Set(Key, value, ShareName);
#elif LINUX
            get;
            set;
#endif
        }
    }
    
    internal class IntSetting : Setting<int>
    {
        public IntSetting(string shareName, string key, int defaultValue) : base(shareName, key, defaultValue) {}

        public override int Value
        {
#if ANDROID
            get => Preferences.Get(Key, DefaultValue, ShareName);
            set => Preferences.Set(Key, value, ShareName);
#elif LINUX
            get;
            set;
#endif
        }
    }
}