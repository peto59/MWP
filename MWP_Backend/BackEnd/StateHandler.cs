﻿using System.Net;
using MWP_Backend.DatatypesAndExtensions;
using MWP;
using MWP.BackEnd.Network;
using MWP.DatatypesAndExtensions;
using MWP.UIBinding;
using Random = System.Random;

namespace MWP.BackEnd
{
    /// <summary>
    /// Global handler of application states
    /// </summary>
    public static class StateHandler
    {
        /// <summary>
        /// Global random number generator
        /// </summary>
        public static readonly Random Rng = new Random();
        /// <summary>
        /// Cancellation token for all song progress bars
        /// </summary>
        public static CancellationTokenSource SongProgressCts = new CancellationTokenSource();
        public static APIThrottler Throttler = new APIThrottler();
        #if ANDROID
        /// <summary>
        /// Current view
        /// </summary>
        public AppCompatActivity? view;
        /// <summary>
        /// Main Activity instance
        /// </summary>
        public MainActivity mainActivity;
        #endif
        /// <summary>
        /// List of all notification IDs
        /// </summary>
        public static readonly List<int> NotificationIDs = new List<int>();
        public static readonly Dictionary<long, (int?, int)> SessionIdToPlaylistOrderMapping = new Dictionary<long, (int?, int)>();

        /// <summary>
        /// Whether NetworkManager Lister was Launched
        /// </summary>
        public static bool launchedNetworkManagerListener = false;

        /// <summary>
        /// List of live hosts on network
        /// </summary>
        public static readonly List<(IPAddress ipAddress, DateTime lastSeen, string hostname)> AvailableHosts =
            new List<(IPAddress ipAddress, DateTime lastSeen, string hostname)>();

        internal static readonly Dictionary<string, UserAcceptedState> OneTimeSendStates = new Dictionary<string, UserAcceptedState>();
        internal static readonly Dictionary<string, List<Song>> OneTimeReceiveSongs = new Dictionary<string, List<Song>>();
        
        
        
        //----------Downloader Callback resolution helpers---------
        internal static SongSelectionDialogActions songSelectionDialogAction = SongSelectionDialogActions.None;
        internal static readonly AutoResetEvent FileEvent = new AutoResetEvent(true);
        internal static readonly AutoResetEvent ResultEvent = new AutoResetEvent(false);
        //-----------Discovery synchronization event------------------
        internal static readonly AutoResetEvent FileListGenerationEvent = new AutoResetEvent(true);
        internal static readonly ManualResetEvent FileListGenerated = new ManualResetEvent(false);
        internal static readonly ManualResetEvent StartDiscoveringEvent = new ManualResetEvent(true);
        

        //---------------------------------------------------------
        
        //----------Custom callbacks for internal control---------
        /// <summary>
        /// Binder for all functions requiring Share Fragment Refresh event
        /// </summary>
        public static event Action? OnShareFragmentRefresh;

        /// <summary>
        /// Share Fragment Refresh event invocation
        /// </summary>
        public static void TriggerShareFragmentRefresh()
        {
            OnShareFragmentRefresh?.Invoke();
        }
        
        /// <summary>
        /// Binder for all functions requiring Share Fragment Refresh event
        /// </summary>
        public static event Action<(string oldTitle, Song song)>? OnTagManagerFragmentRefresh;
        
        /// <summary>
        /// Share Fragment Refresh event invocation
        /// </summary>
        public static void TriggerTagManagerFragmentRefresh(string oldTitle, Song song)
        {
            OnTagManagerFragmentRefresh?.Invoke((oldTitle, song));
        }
        //---------------------------------------------------------
        
        /// <summary>
        /// Whether to show remaining or elapsed time on sing progress bars
        /// </summary>
        public static bool ProgTimeState
        {
            get; set;
        }

        /// <summary>
        /// All <see cref="Song"/>s
        /// </summary>
        public static readonly List<Song> Songs = new List<Song>();
        /// <summary>
        /// All <see cref="Artist"/>s
        /// </summary>
        public static readonly List<Artist> Artists = new List<Artist>();
        /// <summary>
        /// All <see cref="Album"/>s
        /// </summary>
        public static readonly List<Album> Albums = new List<Album>();

        public static UIStateHandler? UiStateHandler = null;

#if ANDROID
        ///<summary>
        ///Sets view to current screen's view
        ///</summary>
        public void SetView(AppCompatActivity newView)
        {
            view = newView;
        }
#endif
    }
}