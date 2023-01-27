using Android.App;
using Android.Content;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using AndroidApp = Android.App.Application;

namespace Ass_Pain
{
    class MediaSessionCallback : Android.Support.V4.Media.Session.MediaSessionCompat.Callback
    {
        public Action OnPlayImpl { get; set; }

        public Action<long> OnSkipToQueueItemImpl { get; set; }

        public Action<long> OnSeekToImpl { get; set; }

        public Action<string, Bundle> OnPlayFromMediaIdImpl { get; set; }

        public Action OnPauseImpl { get; set; }

        public Action OnStopImpl { get; set; }

        public Action OnSkipToNextImpl { get; set; }

        public Action OnSkipToPreviousImpl { get; set; }

        public Action<string, Bundle> OnCustomActionImpl { get; set; }

        public Action<string, Bundle> OnPlayFromSearchImpl { get; set; }

        public override void OnPlay()
        {
            Console.WriteLine("OnPlay");
            OnPlayImpl();
        }

        public override void OnSkipToQueueItem(long id)
        {
            Console.WriteLine("OnSkipToQueueItem");
            OnSkipToQueueItemImpl(id);
        }

        public override void OnSeekTo(long pos)
        {
            Console.WriteLine("OnSeekTo");
            Console.WriteLine($"POSTION: {pos}");
            AndroidApp.Context.StartService(
                            new Intent(MediaService.ActionSeekTo, null, AndroidApp.Context, typeof(MediaService))
                            .PutExtra("millis", (int)pos)
                        );
            //OnSeekToImpl(pos);
        }

        public override void OnPlayFromMediaId(string mediaId, Bundle extras)
        {
            Console.WriteLine("OnPlayFromMediaId");
            OnPlayFromMediaIdImpl(mediaId, extras);
        }

        public override void OnPause()
        {
            Console.WriteLine("OnPause");
            OnPauseImpl();
        }

        public override void OnStop()
        {
            Console.WriteLine("OnStop");
            OnStopImpl();
        }

        public override void OnSkipToNext()
        {
            Console.WriteLine("OnSkipToNext");
            OnSkipToNextImpl();
        }

        public override void OnSkipToPrevious()
        {
            Console.WriteLine("OnSkipToPrevious");
            OnSkipToPreviousImpl();
        }

        public override void OnCustomAction(string action, Bundle extras)
        {
            Console.WriteLine("OnCustomAction");
            OnCustomActionImpl(action, extras);
        }

        public override void OnPlayFromSearch(string query, Bundle extras)
        {
            Console.WriteLine("OnPlayFromSearch");
            OnPlayFromSearchImpl(query, extras);
        }
    }
}