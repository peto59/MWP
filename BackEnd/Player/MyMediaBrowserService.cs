using System;
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
using AndroidX.Media.Utils;
using MWP.BackEnd;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP
{
    [Service(Exported = true)]
    [IntentFilter(new[] { "android.media.browse.MediaBrowserService" })]
    public class MyMediaBrowserService : MediaBrowserServiceCompat
    {
        private const string MY_MEDIA_ROOT_ID = "media_root_id";
        //private static readonly MediaServiceConnection ServiceConnection = new MediaServiceConnection();
      
        public override void OnCreate() {
            base.OnCreate();
            while (!MainActivity.ServiceConnection.Connected)
            {
            #if DEBUG
                MyConsole.WriteLine("Waiting for service");
            #endif
                Thread.Sleep(25);
            }
            if (MainActivity.ServiceConnection.Binder != null) 
                SessionToken = MainActivity.ServiceConnection.Binder.Service.Session.SessionToken;
#if DEBUG
            else
                MyConsole.WriteLine("Empty binder");
#endif

            if (MainActivity.stateHandler.Songs.Count == 0)
            {
                LoadFiles();
            }
        }

        private static void LoadFiles()
        {
            if (MainActivity.stateHandler.Songs.Count == 0)
            {
#if DEBUG
                MyConsole.WriteLine("Generating list");
#endif
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
            returnVal |= clientUid == Process.SystemUid;
#if DEBUG
            MyConsole.WriteLine($"returnval: {returnVal}");
#endif
            return returnVal;
            //TODO: add logic
        }

        public override void OnCustomAction(string action, Bundle? extras, Result result)
        {
            base.OnCustomAction(action, extras, result);
        }

        public override BrowserRoot? OnGetRoot(string clientPackageName, int clientUid, Bundle? rootHints)
        {
            if (!ValidateClient(clientPackageName, clientUid))
            {
#if DEBUG
                MyConsole.WriteLine("OnGetRoot returning null");
#endif
                return null;
            }
#if DEBUG
            MyConsole.WriteLine("OnGetRoot returning BrowserRoot");
#endif
            Bundle extras = new Bundle();
            extras.PutInt(MediaConstants.DescriptionExtrasKeyContentStyleBrowsable, MediaConstants.DescriptionExtrasValueContentStyleGridItem);
            extras.PutInt(MediaConstants.DescriptionExtrasKeyContentStylePlayable, MediaConstants.DescriptionExtrasValueContentStyleListItem);
            extras.PutBoolean(MediaConstants.BrowserServiceExtrasKeySearchSupported, true);
            return new BrowserRoot(MY_MEDIA_ROOT_ID, extras);

        }
        
        public override void OnLoadChildren(string parentId, Result result)
        {
            /*if (MainActivity.stateHandler.Songs.Count == 0)
            {
                LoadFiles();
            }*/
#if DEBUG
            MyConsole.WriteLine("OnLoadChildren");
#endif
            
            List<MediaBrowserCompat.MediaItem?> mediaItems = new List<MediaBrowserCompat.MediaItem?>();
            if (parentId == MY_MEDIA_ROOT_ID)
            {
                //TODO: does this default behaviour make sense?
                mediaItems.AddRange(MainActivity.stateHandler.Artists.Select(artist => artist.ToMediaItem()));
            }
            else
            {
                MediaType mediaType = (MediaType)(parentId[0] - '0');
#if DEBUG
                MyConsole.WriteLine($"MediaType {mediaType}");
#endif
                parentId = parentId[1..];
#if DEBUG
                MyConsole.WriteLine($"parentId {parentId}");
#endif
                switch (mediaType)
                {
                    case MediaType.Album:
                        List<Album> albums = MainActivity.stateHandler.Albums.Search(parentId);
                        if (albums.Count > 0)
                        {
                            mediaItems.AddRange(albums[0].Songs.Select(song => song.ToMediaItem()));
                        }
#if DEBUG
                        else
                        {
                            MyConsole.WriteLine($"No albums found for {parentId}");
                        }
#endif
                        break;
                    case MediaType.Artist:
                        List<Artist> artists = MainActivity.stateHandler.Artists.Search(parentId);
                        if (artists.Count > 0)
                        {
                            mediaItems.AddRange(artists[0].Albums.Where(alb => alb.Title != "Uncategorized").Select(alb => alb.ToMediaItem()));
                            Album? album = artists[0].Albums.FirstOrDefault(alb => alb.Title == "Uncategorized");
                            if (album != null)
                            {
                                mediaItems.AddRange(album.Songs.Select(song => song.ToMediaItem()));
                            }
                        }
#if DEBUG
                        else
                        {
                            MyConsole.WriteLine($"No artists found for {parentId}");
                        }
#endif
                        break;
                    case MediaType.Song:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            JavaList<MediaBrowserCompat.MediaItem?> javaMediaItems = new JavaList<MediaBrowserCompat.MediaItem?>(mediaItems);
            result.SendResult(javaMediaItems);

        }

        public override void OnLoadItem(string? itemId, Result result)
        {
            base.OnLoadItem(itemId, result);
        }

        public override void OnSearch(string query, Bundle? extras, Result result)
        {
            base.OnSearch(query, extras, result);
        }

        ~MyMediaBrowserService()
        {
            //ServiceConnection.Dispose();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            //ServiceConnection.Dispose();
            base.Dispose(disposing);
        }
    }
}