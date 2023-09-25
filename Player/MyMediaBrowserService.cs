using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using AndroidX.Media;
using Ass_Pain.BackEnd;
using Ass_Pain.Helpers;

namespace Ass_Pain
{
    [Service(Exported = true)]
    [IntentFilter(new[] { "android.media.browse.MediaBrowserService" })]
    public class MyMediaBrowserService : MediaBrowserServiceCompat
    {
        private static readonly string MY_MEDIA_ROOT_ID = "media_root_id";
        private static readonly MediaServiceConnection ServiceConnection = new MediaServiceConnection();
      
        public override void OnCreate() {
            base.OnCreate();
            Intent serviceIntent = new Intent(this, typeof(MediaService));
            if (!BindService(serviceIntent, ServiceConnection, Bind.Important))
            {
#if DEBUG
                MyConsole.WriteLine("Cannot connect to MediaService");
#endif
            }

            if (ServiceConnection.Connected)
            {
                SessionToken = ServiceConnection.Binder?.Service.Session.SessionToken;
            }
            
            if (MainActivity.stateHandler.Songs.Count == 0)
            {
                LoadFiles();
            }
        }

        private static void LoadFiles()
        {
            if (MainActivity.stateHandler.Songs.Count == 0)
            {
                new Thread(() => {
                    FileManager.DiscoverFiles(true);
                    if (MainActivity.stateHandler.Songs.Count < FileManager.GetSongsCount())
                    {
                        MainActivity.stateHandler.Songs = new List<Song>();
                        MainActivity.stateHandler.Artists = new List<Artist>();
                        MainActivity.stateHandler.Albums = new List<Album>();
                    
                        MainActivity.stateHandler.Artists.Add(new Artist("No Artist", "Default"));
                        FileManager.GenerateList(FileManager.MusicFolder);
                    }

                    if (MainActivity.stateHandler.Songs.Count != 0)
                    {
                        MainActivity.stateHandler.Songs = MainActivity.stateHandler.Songs.Order(SongOrderType.ByDate);
                    }
                }).Start();
            }
        }

        private static bool ValidateClient(string clientPackageName, int clientUid)
        {
            bool returnVal = true; //TODO: back to false
            returnVal &= clientUid == Process.SystemUid;
            return returnVal;
            //TODO: add logic
        }
        
        public override BrowserRoot? OnGetRoot(string clientPackageName, int clientUid, Bundle? rootHints)
        {
            if (!ValidateClient(clientPackageName, clientUid))
            {
                return null;
            }
            return new BrowserRoot(MY_MEDIA_ROOT_ID, null);

        }
        
        public override void OnLoadChildren(string parentId, Result result)
        {
            if (MainActivity.stateHandler.Songs.Count == 0)
            {
                LoadFiles();
            }
            
            List<MediaBrowserCompat.MediaItem?> mediaItems = new List<MediaBrowserCompat.MediaItem?>();
            if (parentId == MY_MEDIA_ROOT_ID)
            {
                //TODO: does this default behaviour make sense?
                mediaItems.AddRange(MainActivity.stateHandler.Artists.Select(artist => artist.ToMediaItem()));
            }
            else
            {
                
            }
            JavaList<MediaBrowserCompat.MediaItem?> javaMediaItems = new JavaList<MediaBrowserCompat.MediaItem?>(mediaItems);
            result.SendResult(javaMediaItems);

        }
    }
}