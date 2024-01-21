using System;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Widget;
using Java.Lang;
using MWP.BackEnd.Player;
using Math = Java.Lang.Math;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP
{
    public static class WidgetServiceHandler
    {
        private static AppWidgetManager? appWidgetManager;
        private static RemoteViews remoteViews;
        private static ComponentName thisWidget;

        public static void Init(Context? context)
        {
            if (context != null)
            {
                appWidgetManager = AppWidgetManager.GetInstance(context);
                remoteViews = new RemoteViews(context.PackageName, Resource.Layout.music_widget_layout);
                thisWidget = new ComponentName(context, Class.FromType(typeof(MusicWidget)).Name);
            }
        }


        public static Bitmap? createSquaredBitmap(Bitmap srcBmp) {
            int dim = Math.Max(srcBmp.Width, srcBmp.Height);
            Bitmap? dstBmp = Bitmap.CreateBitmap(dim, dim, Bitmap.Config.Argb8888);

            if (dstBmp != null)
            {
                Canvas canvas = new Canvas(dstBmp);
                canvas.DrawColor(Color.White);
                canvas.DrawBitmap(srcBmp, (dim - srcBmp.Width) / 2, (dim - srcBmp.Height) / 2, null);
            }

            return dstBmp;
        }
        
        public static Bitmap GetRoundedCornerBitmap(Bitmap bitmap, int roundPixelSize)
        {
            Bitmap output = Bitmap.CreateBitmap(bitmap.Width, bitmap.Height, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas(output);

            Paint paint = new Paint();
            Rect rect = new Rect(0, 0, bitmap.Width, bitmap.Height);
            RectF rectF = new RectF(rect);
            float roundPx = roundPixelSize;

            paint.AntiAlias = true;
            canvas.DrawRoundRect(rectF, roundPx, roundPx, paint);
            paint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.SrcIn));
            canvas.DrawBitmap(bitmap, rect, rect, paint);

            return output;
        }
        
        public static Bitmap? CropBitmapToSquare(Bitmap bitmap, bool recycle) {
            #if DEBUG
            MyConsole.WriteLine($"{bitmap.Height}");
            #endif
            
            int width = bitmap.Width;
            int height = bitmap.Height;

            Bitmap? result = null;/*w  w  w. j  av  a2s . co m*/
            if (height > width) {
                result = Bitmap.CreateBitmap(bitmap, 0, height / 2 - width / 2,
                    width, width);
            } else {
                result = Bitmap.CreateBitmap(bitmap, width / 2 - height / 2, 0,
                    height, height);
            }
            if (recycle) {
                bitmap.Recycle();
            }
            return result;
        }

        public static void SetPlayButton()
        {
            remoteViews.SetInt(Resource.Id.widgetPlayButton, "setBackgroundResource", Resource.Drawable.play);
            appWidgetManager?.UpdateAppWidget(thisWidget, remoteViews);
        }
        public static void SetPauseButton()
        {
            remoteViews.SetInt(Resource.Id.widgetPlayButton, "setBackgroundResource", Resource.Drawable.pause_fill1_wght200_grad200_opsz48);
            appWidgetManager?.UpdateAppWidget(thisWidget, remoteViews);
        }

        public static void SetShuffleButton()
        {
            remoteViews?.SetInt(Resource.Id.widgetShuffleButton, "setBackgroundResource",
                MainActivity.ServiceConnection.Binder?.Service.QueueObject.IsShuffled ?? false 
                ? Resource.Drawable.shuffle_off
                : Resource.Drawable.shuffle_on
            );
        }

        public static void SetRepeatButton(LoopState state)
        {
            switch (state)
            {
                case LoopState.None:
                    remoteViews?.SetInt(Resource.Id.widgetRepeatButton, "setBackgroundResource",
                        Resource.Drawable.no_repeat);
                    break;
                case LoopState.All:
                    remoteViews?.SetInt(Resource.Id.widgetRepeatButton, "setBackgroundResource",
                        Resource.Drawable.repeat);
                    break;
                case LoopState.Single:
                    remoteViews?.SetInt(Resource.Id.widgetRepeatButton, "setBackgroundResource",
                        Resource.Drawable.repeat_one);
                    break;
            }
        }

        public static void UpdateWidgetViews()
        {

            Bitmap? sq = CropBitmapToSquare(MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Image ??
                                             new Song("No Name", new DateTime(), "Default").Image, false);

            int roundedSize;
            if (MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Image.Height == 360)
            {
                roundedSize = 50;
            }
            else
            {
                roundedSize = 90;
            }
            
            if (sq != null)
                remoteViews.SetImageViewBitmap(Resource.Id.widgetImage,
                    GetRoundedCornerBitmap(sq, roundedSize)
                );

            remoteViews.SetTextViewText(Resource.Id.widgetSongTitle, MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Title ?? "No Name");
            remoteViews.SetTextViewText(Resource.Id.widgetSongArtist, MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Artist.Title ?? "No Artist");
            appWidgetManager?.UpdateAppWidget(thisWidget, remoteViews);
        }
        
    }
}