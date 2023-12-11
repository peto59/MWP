using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Media;
using AndroidX.Media;
using AndroidX.Media.Utils;
using MWP.DatatypesAndExtensions;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Player
{
    /// <inheritdoc />
    [Service(Exported = true)]
    [IntentFilter(new[] { "android.media.browse.MediaBrowserService" })]
    public class MyMediaBrowserService : MediaBrowserServiceCompat
    {
        private const string MY_MEDIA_ROOT_ID = "media_root_id";
        private const string MY_ARTISTS_ROOT_ID = "artists_root_id";
        private const string MY_ALBUMS_ROOT_ID = "albums_root_id";
        private const string MY_SONGS_ROOT_ID = "songs_root_id";
        private const string MY_PLAYLISTS_ROOT_ID = "playlists_root_id";
        /// <summary>
        /// Media Id to play all songs in <see cref="StateHandler"/>
        /// </summary>
        public static readonly string MySongsPlayAll = $"{MY_SONGS_ROOT_ID}_all";
        /// <summary>
        /// Media Id to play all songs in <see cref="StateHandler"/> on shuffle
        /// </summary>
        public static readonly string MySongsShuffle = $"{MY_SONGS_ROOT_ID}_shuffle";
        //private static readonly MediaServiceConnection ServiceConnection = new MediaServiceConnection();
        
        private static readonly MediaDescriptionCompat? MySongsRootDescription = new MediaDescriptionCompat.Builder()
                        .SetMediaId(MY_SONGS_ROOT_ID)?
                        .SetTitle("Songs")?
                        .SetIconBitmap(MusicBaseClassStatic.MusicImage)?
                        .Build();
        private static readonly MediaDescriptionCompat? MyArtistsRootDescription = new MediaDescriptionCompat.Builder()
                        .SetMediaId(MY_ARTISTS_ROOT_ID)?
                        .SetTitle("Artists")?
                        .SetIconBitmap(MusicBaseClassStatic.ArtistsImage)?
                        .Build();
                
        private static readonly MediaDescriptionCompat? MyAlbumsRootDescription = new MediaDescriptionCompat.Builder()
                        .SetMediaId(MY_ALBUMS_ROOT_ID)?
                        .SetTitle("Albums")?
                        .SetIconBitmap(MusicBaseClassStatic.AlbumsImage)?
                        .Build();
                
        private static readonly MediaDescriptionCompat? MyPlaylistsRootDescription = new MediaDescriptionCompat.Builder()
                        .SetMediaId(MY_PLAYLISTS_ROOT_ID)?
                        .SetTitle("Playlists")?
                        .SetIconBitmap(MusicBaseClassStatic.PlaylistsImage)?
                        .Build();

        /// <inheritdoc />
        public override void OnCreate() {
            base.OnCreate();
            //TODO: actually dies when main activity isn't running
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

            if (MainActivity.StateHandler.Songs.Count == 0)
            {
                LoadFiles();
            }
        }

        private static void LoadFiles()
        {
            if (MainActivity.StateHandler.Songs.Count == 0)
            {
#if DEBUG
                MyConsole.WriteLine("Generating list");
#endif
                new Thread(() => {
                    FileManager.DiscoverFiles(true);
                    if (MainActivity.StateHandler.Songs.Count < FileManager.GetSongsCount())
                    {
                        MainActivity.StateHandler.Songs = new List<Song>();
                        MainActivity.StateHandler.Artists = new List<Artist>();
                        MainActivity.StateHandler.Albums = new List<Album>();
                    
                        MainActivity.StateHandler.Artists.Add(new Artist("No Artist", "Default"));
                        FileManager.GenerateList(FileManager.MusicFolder);
                    }

                    if (MainActivity.StateHandler.Songs.Count != 0)
                    {
                        MainActivity.StateHandler.Songs = MainActivity.StateHandler.Songs.Order(SongOrderType.ByDate);
                    }
                }).Start();
            }
        }

        private static bool ValidateClient(string clientPackageName, int clientUid)
        {
            bool returnVal = true; //TODO: back to false
            returnVal |= clientUid == Process.SystemUid;
#if DEBUG
            MyConsole.WriteLine($"return val: {returnVal}");
#endif
            return returnVal;
            //TODO: add logic
        }

        /// <inheritdoc />
        public override void OnCustomAction(string action, Bundle? extras, Result result)
        {
            base.OnCustomAction(action, extras, result);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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
            switch (parentId)
            {
                case MY_MEDIA_ROOT_ID:
                {
                    if (MySongsRootDescription != null)
                    {
                        MediaBrowserCompat.MediaItem item =
                            new MediaBrowserCompat.MediaItem(MySongsRootDescription, MediaBrowserCompat.MediaItem.FlagBrowsable);
                        mediaItems.Add(item);
                    }
                    if (MyArtistsRootDescription != null)
                    {
                        MediaBrowserCompat.MediaItem item =
                            new MediaBrowserCompat.MediaItem(MyArtistsRootDescription, MediaBrowserCompat.MediaItem.FlagBrowsable);
                        mediaItems.Add(item);
                    }
                    if (MyAlbumsRootDescription != null)
                    {
                        MediaBrowserCompat.MediaItem item =
                            new MediaBrowserCompat.MediaItem(MyAlbumsRootDescription, MediaBrowserCompat.MediaItem.FlagBrowsable);
                        mediaItems.Add(item);
                    }
                    if (MyPlaylistsRootDescription != null)
                    {
                        MediaBrowserCompat.MediaItem item =
                            new MediaBrowserCompat.MediaItem(MyPlaylistsRootDescription, MediaBrowserCompat.MediaItem.FlagBrowsable);
                        mediaItems.Add(item);
                    }
                    break;
                }
                case MY_SONGS_ROOT_ID:
                    MediaDescriptionCompat? songsPlayAll = new MediaDescriptionCompat.Builder()
                        .SetMediaId($"{(byte)MediaType.Song}{MySongsPlayAll}")?
                        .SetTitle("Play All")?
                        .SetIconBitmap(MusicBaseClassStatic.PlayImage)?
                        .Build();
                    if (songsPlayAll != null)
                    {
                        MediaBrowserCompat.MediaItem item =
                            new MediaBrowserCompat.MediaItem(songsPlayAll, MediaBrowserCompat.MediaItem.FlagPlayable);
                        mediaItems.Add(item);
                    }
                    
                    MediaDescriptionCompat? songsShuffle = new MediaDescriptionCompat.Builder()
                        .SetMediaId($"{(byte)MediaType.Song}{MySongsShuffle}")?
                        .SetTitle("Shuffle Play")?
                        .SetIconBitmap(MusicBaseClassStatic.ShuffleImage)?
                        .Build();
                    if (songsShuffle != null)
                    {
                        MediaBrowserCompat.MediaItem item =
                            new MediaBrowserCompat.MediaItem(songsShuffle, MediaBrowserCompat.MediaItem.FlagPlayable);
                        mediaItems.Add(item);
                    }
                    
                    mediaItems.AddRange(MainActivity.StateHandler.Songs.Select(song => song.ToMediaItem()));
                    break;
                case MY_ARTISTS_ROOT_ID:
                    mediaItems.AddRange(MainActivity.StateHandler.Artists.Select(artist => artist.ToMediaItem()));
                    break;
                case MY_ALBUMS_ROOT_ID:
                    mediaItems.AddRange(MainActivity.StateHandler.Albums.Select(album => album.ToMediaItem()));
                    break;
                case MY_PLAYLISTS_ROOT_ID:
                    //TODO: add playlists
                    break;
                default:
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
                            MediaDescriptionCompat? albumsPlayAll = new MediaDescriptionCompat.Builder()
                                .SetMediaId($"{(byte)MediaType.ThisPlayAll}{(byte)MediaType.Album}{parentId}")?
                                .SetTitle("Play All")?
                                .SetIconBitmap(MusicBaseClassStatic.PlayImage)?
                                .Build();
                            if (albumsPlayAll != null)
                            {
                                MediaBrowserCompat.MediaItem item =
                                    new MediaBrowserCompat.MediaItem(albumsPlayAll, MediaBrowserCompat.MediaItem.FlagPlayable);
                                mediaItems.Add(item);
                            }
                    
                            MediaDescriptionCompat? albumsShuffle = new MediaDescriptionCompat.Builder()
                                .SetMediaId($"{(byte)MediaType.ThisShufflePlay}{(byte)MediaType.Album}{parentId}")?
                                .SetTitle("Shuffle Play")?
                                .SetIconBitmap(MusicBaseClassStatic.ShuffleImage)?
                                .Build();
                            if (albumsShuffle != null)
                            {
                                MediaBrowserCompat.MediaItem item =
                                    new MediaBrowserCompat.MediaItem(albumsShuffle, MediaBrowserCompat.MediaItem.FlagPlayable);
                                mediaItems.Add(item);
                            }
                            
                            
                            //List<Album> albums = MainActivity.stateHandler.Albums.Search(parentId);
                            Album album = Album.FromId(parentId);
                            mediaItems.AddRange(album.Songs.Select(song => song.ToMediaItem()));
                            break;
                        case MediaType.Artist:
                            MediaDescriptionCompat? artistsPlayAll = new MediaDescriptionCompat.Builder()
                                .SetMediaId($"{(byte)MediaType.ThisPlayAll}{(byte)MediaType.Artist}{parentId}")?
                                .SetTitle("Play All")?
                                .SetIconBitmap(MusicBaseClassStatic.PlayImage)?
                                .Build();
                            if (artistsPlayAll != null)
                            {
                                MediaBrowserCompat.MediaItem item =
                                    new MediaBrowserCompat.MediaItem(artistsPlayAll, MediaBrowserCompat.MediaItem.FlagPlayable);
                                mediaItems.Add(item);
                            }
                    
                            MediaDescriptionCompat? artistsShuffle = new MediaDescriptionCompat.Builder()
                                .SetMediaId($"{(byte)MediaType.ThisShufflePlay}{(byte)MediaType.Artist}{parentId}")?
                                .SetTitle("Shuffle Play")?
                                .SetIconBitmap(MusicBaseClassStatic.ShuffleImage)?
                                .Build();
                            if (artistsShuffle != null)
                            {
                                MediaBrowserCompat.MediaItem item =
                                    new MediaBrowserCompat.MediaItem(artistsShuffle, MediaBrowserCompat.MediaItem.FlagPlayable);
                                mediaItems.Add(item);
                            }

                            Artist artist = Artist.FromId(parentId);
                            mediaItems.AddRange(artist.Albums.Where(alb => alb.Title != "Uncategorized").Select(alb => alb.ToMediaItem()));
                            Album? alb = artist.Albums.FirstOrDefault(alb => alb.Title == "Uncategorized");
                            if (alb != null)
                            {
                                mediaItems.AddRange(alb.Songs.Select(song => song.ToMediaItem()));
                            }
                            break;
                        case MediaType.ThisPlayAll:
                        case MediaType.ThisShufflePlay:
                        case MediaType.Song:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                }
            }
            JavaList<MediaBrowserCompat.MediaItem?> javaMediaItems = new JavaList<MediaBrowserCompat.MediaItem?>(mediaItems);
            result.SendResult(javaMediaItems);

        }

        /// <inheritdoc />
        public override void OnLoadItem(string? itemId, Result result)
        {
            base.OnLoadItem(itemId, result);
        }

        /// <inheritdoc />
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