using Android.App;
using Android.Content;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Systems;
using Android.Views;
using Android.Widget;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using MWP.Helpers;
using AndroidApp = Android.App.Application;
#if DEBUG
#endif

namespace MWP
{
    class MediaSessionCallback : Android.Support.V4.Media.Session.MediaSessionCompat.Callback
    {
        public override void OnPlay()
        {
#if DEBUG
            MyConsole.WriteLine("OnPlay");
#endif
            AndroidApp.Context.StartService(
                            new Intent(MediaService.ActionPlay, null, AndroidApp.Context, typeof(MediaService))
                        );
            //OnPlayImpl();
            base.OnPlay();
        }

        public override void OnSkipToQueueItem(long id)
        {
#if DEBUG
            MyConsole.WriteLine("OnSkipToQueueItem");
#endif
            //OnSkipToQueueItemImpl(id);
            base.OnSkipToQueueItem(id);
        }

        public override void OnSeekTo(long pos)
        {
#if DEBUG
            MyConsole.WriteLine("OnSeekTo");
            MyConsole.WriteLine($"POSTION: {pos}");
#endif
            AndroidApp.Context.StartService(
                            new Intent(MediaService.ActionSeekTo, null, AndroidApp.Context, typeof(MediaService))
                            .PutExtra("millis", (int)pos)
                        );
            //OnSeekToImpl(pos);
            base.OnSeekTo(pos);
        }

        public override void OnPlayFromMediaId(string mediaId, Bundle extras)
        {
#if DEBUG
            MyConsole.WriteLine("OnPlayFromMediaId");
#endif
            //OnPlayFromMediaIdImpl(mediaId, extras);
            base.OnPlayFromMediaId(mediaId, extras);
        }

        public override void OnPause()
        {
#if DEBUG
            MyConsole.WriteLine("OnPause");
#endif
            AndroidApp.Context.StartService(
                            new Intent(MediaService.ActionPause, null, AndroidApp.Context, typeof(MediaService))
                        );
            //OnPauseImpl();
            base.OnPause();
        }

        public override void OnStop()
        {
#if DEBUG
            MyConsole.WriteLine("OnStop");
#endif
            AndroidApp.Context.StartService(
                            new Intent(MediaService.ActionStop, null, AndroidApp.Context, typeof(MediaService))
                        );
            //OnStopImpl();
            base.OnStop();
        }

        public override void OnSkipToNext()
        {
#if DEBUG
            MyConsole.WriteLine("OnSkipToNext");
#endif
            AndroidApp.Context.StartService(
                            new Intent(MediaService.ActionNextSong, null, AndroidApp.Context, typeof(MediaService))
                        );
            //OnSkipToNextImpl();
            base.OnSkipToNext();
        }

        public override void OnSkipToPrevious()
        {
#if DEBUG
            MyConsole.WriteLine("OnSkipToPrevious");
#endif
            AndroidApp.Context.StartService(
                            new Intent(MediaService.ActionPreviousSong, null, AndroidApp.Context, typeof(MediaService))
                        );
            //OnSkipToPreviousImpl();
            base.OnSkipToPrevious();
        }

        public override void OnCustomAction(string action, Bundle extras)
        {
#if DEBUG
            MyConsole.WriteLine("OnCustomAction");
#endif
            try
            {
                switch (action)
                {
                    case "loop":
                        MainActivity.ServiceConnection.Binder?.Service?.ToggleLoop((int)MainActivity.ServiceConnection.Binder.Service.QueueObject.LoopState + 1);
                        break;
                    case "shuffle":
                        MainActivity.ServiceConnection.Binder?.Service?.Shuffle(!MainActivity.ServiceConnection.Binder.Service.QueueObject.IsShuffled);
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

        public override void OnPlayFromSearch(string query, Bundle extras)
        {
#if DEBUG
            MyConsole.WriteLine("OnPlayFromSearch");
#endif

            //OnPlayFromSearchImpl(query, extras);
            base.OnPlayFromSearch(query, extras);
        }
        
    }
}