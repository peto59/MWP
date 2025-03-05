#if ANDROID
using System;
using Android.OS;
using Java.Lang;
using MWP_Backend.BackEnd.Helpers;
using MWP_Backend.DatatypesAndExtensions;
using MWP.DatatypesAndExtensions;
using Exception = System.Exception;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Player
{
    internal class MediaSessionCallback : Android.Support.V4.Media.Session.MediaSessionCompat.Callback
    {
        private readonly MediaServiceBinder binder;

        public MediaSessionCallback(IBinder bind)
        {
            if (bind is MediaServiceBinder tmp)
            {
                binder = tmp;
            }
            else
            {
                throw new IllegalArgumentException("Ja tieto nully nenavidim");
            }
            
        }

        public override void OnPlay()
        {
#if DEBUG
            MyConsole.WriteLine("OnPlay");
#endif
            binder.Service.Play();
            //OnPlayImpl();
            base.OnPlay();
        }

        public override void OnSkipToQueueItem(long id)
        {
#if DEBUG
            MyConsole.WriteLine("OnSkipToQueueItem");
#endif
            if (id != binder.Service.QueueObject.Index)
            {
                if (binder.Service.QueueObject.SetIndex(id))
                {
                    binder.Service.Play(true);
                }
            }
            base.OnSkipToQueueItem(id);
        }

        public override void OnSeekTo(long pos)
        {
#if DEBUG
            MyConsole.WriteLine("OnSeekTo");
            MyConsole.WriteLine($"POSTION: {pos}");
#endif

            binder.Service.SeekTo((int)pos);
            //OnSeekToImpl(pos);
            base.OnSeekTo(pos);
        }

        public override void OnPlayFromMediaId(string? mediaId, Bundle? extras)
        {
            if (mediaId == null)
            {
                base.OnPlayFromMediaId(mediaId, extras);
                return;
            }
#if DEBUG
            MyConsole.WriteLine($"OnPlayFromMediaId mediaId {mediaId}");
#endif
            MediaType mediaType = (MediaType)(mediaId[0] - '0');
#if DEBUG
            MyConsole.WriteLine($"MediaType {mediaType}");

#endif
            mediaId = mediaId[1..];
#if DEBUG
            MyConsole.WriteLine($"mediaId {mediaId}");
#endif

            switch (mediaType)
            {
                case MediaType.Song:
                    if (mediaId == MyMediaBrowserService.MySongsPlayAll)
                    {
                        binder.Service.GenerateQueue(MainActivity.StateHandler.Songs);
                    }
                    else if (mediaId == MyMediaBrowserService.MySongsShuffle)
                    {
                        binder.Service.GenerateQueue(MainActivity.StateHandler.Songs, null, false);
                        binder.Service.Shuffle(true);
                        binder.Service.Play();
                    }
                    else
                    {
                        binder.Service.GenerateQueue(Song.FromId(mediaId));
                    }
                    break;
                case MediaType.ThisPlayAll:
                    MediaType mediaTypePlayAll = (MediaType)(mediaId[0] - '0');
                    mediaId = mediaId[1..];
                    if (mediaTypePlayAll == MediaType.Album)
                    {
                        binder.Service.GenerateQueue(Album.FromId(mediaId));
                    }else if (mediaTypePlayAll == MediaType.Artist)
                    {
                        binder.Service.GenerateQueue(Artist.FromId(mediaId));
                    }
                    break;
                case MediaType.ThisShufflePlay:
                    MediaType mediaTypeShuffle = (MediaType)(mediaId[0] - '0');
                    mediaId = mediaId[1..];
                    if (mediaTypeShuffle == MediaType.Album)
                    {
                        binder.Service.GenerateQueue(Album.FromId(mediaId), null, false);
                        binder.Service.Shuffle(true);
                        binder.Service.Play();
                    }else if (mediaTypeShuffle == MediaType.Artist)
                    {
                        binder.Service.GenerateQueue(Artist.FromId(mediaId), null, false);
                        binder.Service.Shuffle(true);
                        binder.Service.Play();
                    }
                    break;
                case MediaType.Album:
                case MediaType.Artist:
                default:
                    throw new ArgumentOutOfRangeException();
            }
            base.OnPlayFromMediaId(mediaId, extras);
        }

        public override void OnPause()
        {
#if DEBUG
            MyConsole.WriteLine("OnPause");
#endif
            binder.Service.Pause();
            //OnPauseImpl();
            base.OnPause();
        }

        public override void OnStop()
        {
#if DEBUG
            MyConsole.WriteLine("OnStop");
#endif
            binder.Service.Stop();
            //OnStopImpl();
            base.OnStop();
        }

        public override void OnSkipToNext()
        {
#if DEBUG
            MyConsole.WriteLine("OnSkipToNext");
#endif
            binder.Service.NextSong();
            //OnSkipToNextImpl();
            base.OnSkipToNext();
        }

        public override void OnSkipToPrevious()
        {
#if DEBUG
            MyConsole.WriteLine("OnSkipToPrevious");
#endif
            binder.Service.PreviousSong();
            //OnSkipToPreviousImpl();
            base.OnSkipToPrevious();
        }

        public override void OnCustomAction(string? action, Bundle? extras)
        {
#if DEBUG
            MyConsole.WriteLine("OnCustomAction");
#endif
            try
            {
                switch (action)
                {
                    case "loop":
                        binder.Service.ToggleLoop((int)binder.Service.QueueObject.LoopState + 1);
                        break;
                    case "shuffle":
                        binder.Service.Shuffle(!binder.Service.QueueObject.IsShuffled);
                        break;
                    default:
                        throw new ArgumentException("Must use loop or shuffle as action argument");
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                //throw;
            }
            //OnCustomActionImpl(action, extras);
            base.OnCustomAction(action, extras);
        }

        public override void OnPlayFromSearch(string? query, Bundle? extras)
        {
#if DEBUG
            MyConsole.WriteLine("OnPlayFromSearch");
#endif

            //OnPlayFromSearchImpl(query, extras);
            base.OnPlayFromSearch(query, extras);
        }
        
    }
}
#endif