using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Android.Graphics;
using AndroidX.AppCompat.App;
using MWP.DatatypesAndExtensions;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd
{
    /// <summary>
    /// Global handler of application states
    /// </summary>
    public class StateHandler
    {
        /// <summary>
        /// Global random number generator
        /// </summary>
        public static readonly Random Rng = new Random();
        /// <summary>
        /// Cancellation token for all song progress bars
        /// </summary>
        public CancellationTokenSource SongProgressCts = new CancellationTokenSource();
        /// <summary>
        /// Current view
        /// </summary>
        public AppCompatActivity? view;
        /// <summary>
        /// Main Activity instance
        /// </summary>
        public MainActivity mainActivity;
        public List<int> NotificationIDs = new List<int>();
        public Dictionary<long, (int?, int)> SessionIdToPlaylistOrderMapping = new Dictionary<long, (int?, int)>();

        /// <summary>
        /// List of live hosts on network
        /// </summary>
        public List<(IPAddress ipAddress, DateTime lastSeen, string hostname)> AvailableHosts =
            new List<(IPAddress ipAddress, DateTime lastSeen, string hostname)>();
        
        
        
        //----------Downloader Callback resolution helpers---------
        internal SongSelectionDialogActions songSelectionDialogAction = SongSelectionDialogActions.None;
        internal readonly AutoResetEvent FileEvent = new AutoResetEvent(true);
        internal readonly AutoResetEvent ResultEvent = new AutoResetEvent(false);
        //-----------Discovery synchronization event------------------
        internal readonly AutoResetEvent FileListGenerationEvent = new AutoResetEvent(true);
        

        //---------------------------------------------------------
        
        //----------Custom callbacks for internal control---------
        /// <summary>
        /// Binder for all functions requiring Share Fragment Refresh event
        /// </summary>
        public static event Action OnShareFragmentRefresh;

        /// <summary>
        /// Share Fragment Refresh event invocation
        /// </summary>
        public static void TriggerShareFragmentRefresh()
        {
            OnShareFragmentRefresh.Invoke();
        }
        
        /// <summary>
        /// Binder for all functions requiring Share Fragment Refresh event
        /// </summary>
        public static event Action<(string oldTitle, Song song)> OnTagManagerFragmentRefresh;
        
        /// <summary>
        /// Share Fragment Refresh event invocation
        /// </summary>
        public static void TriggerTagManagerFragmentRefresh(string oldTitle, Song song)
        {
            OnTagManagerFragmentRefresh.Invoke((oldTitle, song));
        }
        //---------------------------------------------------------
        
        /// <summary>
        /// Whether to show remaining or elapsed time on sing progress bars
        /// </summary>
        public bool ProgTimeState
        {
            get; set;
        }

        /// <summary>
        /// All <see cref="MWP.Song"/>s
        /// </summary>
        public List<Song> Songs = new List<Song>();
        /// <summary>
        /// All <see cref="MWP.Artist"/>s
        /// </summary>
        public List<Artist> Artists = new List<Artist>();
        /// <summary>
        /// All <see cref="MWP.Album"/>s
        /// </summary>
        public List<Album> Albums = new List<Album>();

        ///<summary>
        ///Sets view to current screen's view
        ///</summary>
        public void SetView(AppCompatActivity newView)
        {
            view = newView;
        }
    }
}