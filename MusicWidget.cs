using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Widget;
using Java.Lang;
using MWP.BackEnd;
using MWP.BackEnd.Player;
using AndroidApp = Android.App.Application;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP
{
    /// <inheritdoc />
    [BroadcastReceiver(Label = "Music Widget", Exported = false)]
    [IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/musicwidget_provider")]
    public class MusicWidget : AppWidgetProvider
    {
        private static readonly string WIDGET_NEXT_TAG = "WIDGET_NEXT_TAG";
        private static readonly string WIDGET_PREVIOUS_TAG = "WIDGET_PEVIOUS_TAG";
        private static readonly string WIDGET_PLAY_TAG = "WIDGET_PLAY_CLICK";
        private static MediaServiceConnection? ServiceConnection = new MediaServiceConnection();


        /// <inheritdoc />
        public override void OnUpdate(Context? context, AppWidgetManager? manager, int[]? widgetIds)
        {
            if (context == null) return;
            new Thread(() =>
            {
                Looper.Prepare();
                Intent serviceIntent = new Intent(Application.Context, typeof(MediaService));
                ServiceConnection ??= new MediaServiceConnection();
                while (!ServiceConnection.Connected)
                {
                    if (!context.BindService(serviceIntent, ServiceConnection, Bind.Important | Bind.AutoCreate))
                    {
#if DEBUG
                        MyConsole.WriteLine("Cannot connect to MediaService");
#endif
                    }
#if DEBUG
                    MyConsole.WriteLine("Waiting for service");
#endif
                    Thread.Sleep(25);
                }
            }).Start();
            FileManager.LoadFiles();
            
            ComponentName me = new ComponentName(context, Class.FromType(typeof(MusicWidget)).Name);
            manager?.UpdateAppWidget(me, BuildRemoteView(context, widgetIds));
        }
       

        private RemoteViews BuildRemoteView(Context context, int[]? widgetIds)
        {
            RemoteViews widgetView = new RemoteViews(context.PackageName, Resource.Layout.music_widget_layout);
            
            
            SetTextViewText(widgetView);
            RegisterClicks(context, widgetIds, widgetView);
            
            
            widgetView.SetImageViewBitmap(Resource.Id.widgetImage,
                WidgetServiceHandler.GetRoundedCornerBitmap(
                    ServiceConnection?.Binder?.Service.QueueObject.Current.Image ?? new Song("No Name", new DateTime(), "Default").Image, 
                    120
                )
            );
            return widgetView;
        }

        private void SetTextViewText(RemoteViews widgetView)
        {
            widgetView.SetTextViewText(Resource.Id.widgetSongTitle, ServiceConnection?.Binder?.Service.QueueObject.Current.Title ?? "No Name");
            widgetView.SetTextViewText(Resource.Id.widgetSongArtist, ServiceConnection?.Binder?.Service.QueueObject.Current.Artist.Title ?? "No Artist");
        }
        
        private void RegisterClicks(Context context, int[]? widgetIds, RemoteViews widgetView)
        {
            var intent = new Intent(context, typeof(MusicWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, widgetIds);
            
            // handle button clicks
            /*widgetView.SetOnClickPendingIntent(Resource.Id.widgetPlayButton, PendingIntent.GetBroadcast(AndroidApp.Context, 0, new Intent(MyMediaBroadcastReceiver.TOGGLE_PLAY, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver)), PendingIntentFlags.Mutable));
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetPreviousButton, PendingIntent.GetBroadcast(AndroidApp.Context, 0, new Intent(MyMediaBroadcastReceiver.PREVIOUS_SONG, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver)), PendingIntentFlags.Mutable));
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetNextButton, PendingIntent.GetBroadcast(AndroidApp.Context, 0, new Intent(MyMediaBroadcastReceiver.NEXT_SONG, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver)), PendingIntentFlags.Mutable));*/
            
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetNextButton, GetPendingSelfIntent(context, WIDGET_NEXT_TAG));
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetPreviousButton, GetPendingSelfIntent(context, WIDGET_PREVIOUS_TAG));
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetPlayButton, GetPendingSelfIntent(context, WIDGET_PLAY_TAG));
        }
        
        private PendingIntent? GetPendingSelfIntent(Context context, string action)
        {
            var intent = new Intent(context, typeof(MusicWidget));
            intent.SetAction(action);

            var pendingIntentFlags = (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable
                : PendingIntentFlags.UpdateCurrent;

            return PendingIntent.GetBroadcast(context, 0, intent, pendingIntentFlags);
        }


        /// <inheritdoc />
        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            StateHandler.MissingFilesWaiter.WaitOne();
            if (WIDGET_PLAY_TAG.Equals(intent?.Action))
            {
                if (ServiceConnection?.Connected ?? false)
                {
                    if (ServiceConnection.Binder?.Service.IsPlaying ?? false)
                    {
                        ServiceConnection.Binder?.Service.Pause();
                    }
                    else
                    {
                        ServiceConnection.Binder?.Service.Play();
                    }
                }
            }
            else if (WIDGET_PREVIOUS_TAG.Equals(intent?.Action))
            {
                ServiceConnection?.Binder?.Service.PreviousSong();
            }
            else if (WIDGET_NEXT_TAG.Equals(intent?.Action))
            {
                ServiceConnection?.Binder?.Service.NextSong();
            }


        }
    }
}