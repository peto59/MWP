using Android.App;
using Android.Content;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Java.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MWP.BackEnd.Player;
using static Android.Renderscripts.Sampler;

namespace MWP
{
    public class StateHandler
    {
        public static Random Rng = new Random();
        public CancellationTokenSource cts = new CancellationTokenSource();
        public AppCompatActivity view;
        public MainActivity mainActivity;
        public List<int> NotificationIDs = new List<int>();
        public Dictionary<long, (int?, int)> SessionIdToPlaylistOrderMapping = new Dictionary<long, (int?, int)>();

        public List<(IPAddress ipAddress, DateTime lastSeen, string hostname)> AvailableHosts =
            new List<(IPAddress ipAddress, DateTime lastSeen, string hostname)>();
        
        
        
        //----------Downloader Callback resolution helpers---------
        internal SongSelectionDialogActions songSelectionDialogAction = SongSelectionDialogActions.None;
        internal readonly AutoResetEvent FileEvent = new AutoResetEvent(true);
        internal readonly AutoResetEvent ResultEvent = new AutoResetEvent(false);
        //-----------Discovery synchronization event------------------
        internal readonly AutoResetEvent FileListGenerationEvent = new AutoResetEvent(true);
        


        public List<Song> Songs = new List<Song>();
        public List<Artist> Artists = new List<Artist>();
        public List<Album> Albums = new List<Album>();

        public bool ProgTimeState
        {
            get; set;
        }





        ///<summary>
        ///Sets view to current screen's view
        ///</summary>
        public void SetView(AppCompatActivity new_view)
        {
            view = new_view;
        }



        // public void setQueue(ref List<string> x)
        // {
        //     queue = x;
        // }

        /*public void setIndex(ref int x)
        {
            index = x;
        }*/
    }
}