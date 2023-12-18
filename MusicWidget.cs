using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Widget;
using Java.Lang;
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

        
        /// <inheritdoc />
        public override void OnUpdate(Context? context, AppWidgetManager? manager, int[]? widgetIds)
        {
            if (context == null) return;
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

            
            // handle button clicks
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetPlayButton, PendingIntent.GetBroadcast(AndroidApp.Context, 0, new Intent(MyMediaBroadcastReceiver.TOGGLE_PLAY, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver)), PendingIntentFlags.Mutable));
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetPreviousButton, PendingIntent.GetBroadcast(AndroidApp.Context, 0, new Intent(MyMediaBroadcastReceiver.PREVIOUS_SONG, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver)), PendingIntentFlags.Mutable));
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetNextButton, PendingIntent.GetBroadcast(AndroidApp.Context, 0, new Intent(MyMediaBroadcastReceiver.NEXT_SONG, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver)), PendingIntentFlags.Mutable));
        }
    }
}