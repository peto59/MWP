using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Icu.Number;
using Android.OS;
using Android.Support.V4.Media.Session;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using System;
using System.IO;
using System.Runtime.Remoting.Contexts;
using Xamarin.Essentials;
using static Android.Renderscripts.ScriptGroup;
using AndroidApp = Android.App.Application;

namespace Ass_Pain
{
    public class Local_notification_service
    {
        private const string CHANNEL_ID = "local_notification_channel";
        private const string CHANNEL_NAME = "Notifications";
        private const string CHANNEL_DESCRIPTION = "description";

        private int notification_id = -1;
        private const string TITLE_KEY = "title";
        private const string MESSAGE_KEY = "message";

        private bool is_channel_init = false;
        private bool is_created = false;

        NotificationManagerCompat manager;

        private void create_notification_channel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            var channel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.Default)
            {
                Description = CHANNEL_DESCRIPTION
            };

            NotificationManager manager = (NotificationManager)AndroidApp.Context.GetSystemService(AndroidApp.NotificationService);
            manager.CreateNotificationChannel(channel);
        }


        public void push_notification()
        {
            if (!is_channel_init)
            {
                create_notification_channel();
            }

            Intent intent = new Intent(AndroidApp.Context, typeof(MainActivity));
            intent.PutExtra(TITLE_KEY, "title");
            intent.PutExtra(MESSAGE_KEY, "message");
            intent.AddFlags(ActivityFlags.ClearTop);

            notification_id++;

            PendingIntent pending = PendingIntent.GetActivity(AndroidApp.Context, notification_id, intent, PendingIntentFlags.OneShot);
            NotificationCompat.Builder notification_builder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
                .SetSmallIcon(
                    Resource.Drawable.ic_menu_camera
                )
                .SetContentTitle("title")
                .SetContentText("message")
                .SetAutoCancel(true)
                .SetContentIntent(pending);


            NotificationManagerCompat manager = NotificationManagerCompat.From(AndroidApp.Context);
            manager.Notify(notification_id, notification_builder.Build());

        }

        private Bitmap get_current_song_image()
        {
            Bitmap image = null;
            TagLib.File tagFile;

            try
            {
                Console.WriteLine($"now playing: {MainActivity.stateHandler.NowPlaying}");
                tagFile = TagLib.File.Create(
                    MainActivity.stateHandler.NowPlaying
                );
                MemoryStream ms = new MemoryStream(tagFile.Tag.Pictures[0].Data.Data);
                image = BitmapFactory.DecodeStream(ms);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine($"Doesnt contain image: {MainActivity.stateHandler.NowPlaying}");
            }

            if (image == null)
            {
                image = BitmapFactory.DecodeStream(AndroidApp.Context.Assets.Open("music_placeholder.png"));
            }

            return image;
        }

        public void song_control_notification(MediaSessionCompat.Token token)
        {
            if (is_created)
                return;

            if (!is_channel_init)
            {
                create_notification_channel();
            }

            Bitmap current_song_image = get_current_song_image();
          
            NotificationCompat.Builder notification_builder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
              .SetSmallIcon(
                  Resource.Drawable.ic_menu_camera
              )
              .SetContentTitle(FileManager.GetSongTitle(MainActivity.stateHandler.NowPlaying))
              .SetContentText(FileManager.GetSongArtist(MainActivity.stateHandler.NowPlaying)[0])
              .SetLargeIcon(
                    current_song_image
               )
              .AddAction(Resource.Drawable.previous, "Previous", null)
              .AddAction(Resource.Drawable.play, "play", null)
              .AddAction(Resource.Drawable.next, "next", null)
              .SetStyle(new AndroidX.Media.App.NotificationCompat.MediaStyle().SetMediaSession(token))
              .SetDefaults((int)NotificationDefaults.Sound | (int)NotificationDefaults.Vibrate);

            

            manager = NotificationManagerCompat.From(AndroidApp.Context);
            manager.Notify(notification_id, notification_builder.Build());
            is_created = true;
        }


        public void destroy_song_control()
        {
            if (is_created)
            {
                manager.Cancel(notification_id);
                is_created = false;
            }
        }
    }
}