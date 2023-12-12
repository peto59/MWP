using System;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Widget;
using Java.Lang;
using MWP.Helpers;

namespace MWP
{
    /// <inheritdoc />
    [BroadcastReceiver(Label = "Music Widget", Exported = false)]
    [IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/musicwidget_provider")]
    public class MusicWidget : AppWidgetProvider
    {
        public static readonly string WIDGET_BUTTON_TAG = "WIDGET_BUTTON_CLICK";
        
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
            
            return widgetView;
        }

        private void SetTextViewText(RemoteViews widgetView)
        {
            widgetView.SetTextViewText(Resource.Id.widgetText, "HelloFromWidget");
            
        }
        
        private void RegisterClicks(Context context, int[]? widgetIds, RemoteViews widgetView)
        {
            var intent = new Intent(context, typeof(MusicWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, widgetIds);
            
            // handle button click
            // widgetView.SetOnClickPendingIntent(Resource.Id.widgetButton, GetPendingSelfIntent(context, WIDGET_BUTTON_TAG));
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

            if (WIDGET_BUTTON_TAG.Equals(intent?.Action))
            {
                Toast.MakeText(context, "Hello from button", ToastLength.Long)?.Show();
#if DEBUG
                MyConsole.WriteLine("Widget button clicked");
#endif
            }
            
            
        }
        
        
        
    }
}