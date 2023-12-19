using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Widget;
using Java.Lang;
using MWP.BackEnd.Player;
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
        private static readonly string WIDGET_SHUFFLE_TAG = "WIDGET_SHUFFLE_TAG";
        
        private static readonly string WIDGET_PREVIOUS_TAG = "WIDGET_PEVIOUS_TAG";
        private static readonly string WIDGET_PLAY_TAG = "WIDGET_PLAY_CLICK";
        private static readonly string WIDGET_NEXT_TAG = "WIDGET_NEXT_TAG";

        private static readonly string WIDGET_REPEAT_TAG = "WIDGET_REPEAT_TAG";

        
        /// <inheritdoc />
        public override void OnUpdate(Context? context, AppWidgetManager? manager, int[]? widgetIds)
        {
            if (context != null)
            {
                var me = new ComponentName(context, Class.FromType(typeof(MusicWidget)).Name);
                manager?.UpdateAppWidget(me, BuildRemoteView(context, widgetIds));
            }
        }
       

        private RemoteViews BuildRemoteView(Context context, int[]? widgetIds)
        {
            var widgetView = new RemoteViews(context.PackageName, Resource.Layout.music_widget_layout);
            
            
            SetTextViewText(widgetView);
            RegisterClicks(context, widgetIds, widgetView);
            
            
            widgetView.SetImageViewBitmap(Resource.Id.widgetImage,
                WidgetServiceHandler.GetRoundedCornerBitmap(
                    MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Image ?? new Song("No Name", new DateTime(), "Default").Image, 
                    120
                )
                
            );
            
            
            return widgetView;
        }

        private void SetTextViewText(RemoteViews widgetView)
        {
            widgetView.SetTextViewText(Resource.Id.widgetSongTitle, MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Title ?? "No Name");
            widgetView.SetTextViewText(Resource.Id.widgetSongArtist, MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Artist.Title ?? "No Artist");
        }
        
        private void RegisterClicks(Context context, int[]? widgetIds, RemoteViews widgetView)
        {
            var intent = new Intent(context, typeof(MusicWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, widgetIds);
            
            // handle button clicks
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetShuffleButton, GetPendingSelfIntent(context, WIDGET_SHUFFLE_TAG));
            
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetPreviousButton, GetPendingSelfIntent(context, WIDGET_PREVIOUS_TAG));
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetPlayButton, GetPendingSelfIntent(context, WIDGET_PLAY_TAG));
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetNextButton, GetPendingSelfIntent(context, WIDGET_NEXT_TAG));
            
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetRepeatButton, GetPendingSelfIntent(context, WIDGET_REPEAT_TAG));
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

            if (WIDGET_PLAY_TAG.Equals(intent?.Action))
            {
                if (MainActivity.ServiceConnection.Connected)
                {
                    if (MainActivity.ServiceConnection.Binder?.Service.IsPlaying ?? false)
                    {
                        MainActivity.ServiceConnection.Binder?.Service.Pause();
                    }
                    else
                    {
                        MainActivity.ServiceConnection.Binder?.Service.Play();
                    }
                }
            }
            else if (WIDGET_PREVIOUS_TAG.Equals(intent?.Action))
            {
                MainActivity.ServiceConnection.Binder?.Service.PreviousSong();
            }
            else if (WIDGET_NEXT_TAG.Equals(intent?.Action))
            {
                MainActivity.ServiceConnection.Binder?.Service.NextSong();
            }
            else if (WIDGET_SHUFFLE_TAG.Equals(intent?.Action))
            {
                WidgetServiceHandler.SetShuffleButton();
                MainActivity.ServiceConnection.Binder?.Service.Shuffle(!MainActivity.ServiceConnection.Binder?.Service.QueueObject.IsShuffled ?? false);
            }
            else if (WIDGET_REPEAT_TAG.Equals(intent?.Action))
            {
                switch (MainActivity.ServiceConnection.Binder?.Service.QueueObject.LoopState ?? LoopState.None)
                {
                    case LoopState.None:
                        WidgetServiceHandler.SetRepeatButton(LoopState.All);
                        MainActivity.ServiceConnection.Binder?.Service.ToggleLoop(LoopState.All);
                        break;
                    case LoopState.All:
                        WidgetServiceHandler.SetRepeatButton(LoopState.Single);
                        MainActivity.ServiceConnection.Binder?.Service.ToggleLoop(LoopState.Single);
                        break;
                    case LoopState.Single:
                        WidgetServiceHandler.SetRepeatButton(LoopState.None);
                        MainActivity.ServiceConnection.Binder?.Service.ToggleLoop(LoopState.None);
                        break;
                }
            }


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

            if (WIDGET_PLAY_TAG.Equals(intent?.Action))
            {
                if (MainActivity.ServiceConnection.Connected)
                {
                    if (MainActivity.ServiceConnection.Binder?.Service.IsPlaying ?? false)
                    {
                        MainActivity.ServiceConnection.Binder?.Service.Pause();
                    }
                    else
                    {
                        MainActivity.ServiceConnection.Binder?.Service.Play();
                    }
                }
            }
            else if (WIDGET_PREVIOUS_TAG.Equals(intent?.Action))
            {
                MainActivity.ServiceConnection.Binder?.Service.PreviousSong();
            }
            else if (WIDGET_NEXT_TAG.Equals(intent?.Action))
            {
                MainActivity.ServiceConnection.Binder?.Service.NextSong();
            }

            
        }
        
        
        
    }
}