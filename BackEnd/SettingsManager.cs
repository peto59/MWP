﻿namespace Ass_Pain
{
    internal static class SettingsManager
    {
        private const string ShareName = "AssPainSharedPreferences";


        /*private static readonly Setting<bool> shouldUseChromaprintAtDownload = new BoolSetting(ShareName, "ShouldUseChromaprintAtDownload", false);
        public static bool ShouldUseChromaprintAtDownload
        {
            get => shouldUseChromaprintAtDownload.Value;
            set => shouldUseChromaprintAtDownload.Value = value;
        }

        private static readonly Setting<bool> shouldUseChromaprintAtDiscover = new BoolSetting(ShareName, "ShouldUseChromaprintAtDiscover", false);
        public static bool ShouldUseChromaprintAtDiscover
        {
            get => shouldUseChromaprintAtDiscover.Value;
            set => shouldUseChromaprintAtDiscover.Value = value;
        }*/
        
        private static readonly Setting<int> defaultDownloadAction = new IntSetting(ShareName, "defaultDownloadAction", (int)DownloadActions.DownloadOnly);
        public static DownloadActions DefaultDownloadAction
        {
            get => (DownloadActions)defaultDownloadAction.Value;
            set => defaultDownloadAction.Value = (int)value;
        }
    }
}