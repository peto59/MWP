using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.Media.Session;
using AndroidX.Core.App;
using System;
using MWP.BackEnd.Player;
//using static Android.Renderscripts.ScriptGroup;
using AndroidApp = Android.App.Application;
using Context = Android.Content.Context;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP
{
    public class Local_notification_service
    {
        private const string ChannelId = "local_notification_channel";
        private const string ChannelName = "Notifications";
        private const string ChannelDescription = "description";

        private int notification_id = 1;
        public int NotificationId
        {
            get { return notification_id; }
        }
        private const string TITLE_KEY = "title";
        private const string MESSAGE_KEY = "message";

        private bool is_channel_init = false;
        private bool is_created = false;
        public bool IsCreated
        {
            get { return is_created; }
        }
        NotificationCompat.Builder notificationBuilder;
        private Notification notification;
        public Notification Notification
        {
            get { return notification; }
        }

        NotificationManagerCompat manager;

        public void Notify()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
            {
               
                notificationBuilder.MActions?.Clear();

                notificationBuilder.AddAction(
                    
                    MainActivity.ServiceConnection.Binder?.Service.QueueObject.IsShuffled ?? false ? Resource.Drawable.shuffle_on : Resource.Drawable.shuffle_off, "shuffle",
                      PendingIntent.GetBroadcast(
                          AndroidApp.Context, Convert.ToInt32(MainActivity.ServiceConnection.Binder?.Service.QueueObject.IsShuffled ?? false),
                          new Intent(MyMediaBroadcastReceiver.SHUFFLE, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver))
                          .PutExtra("shuffle", !MainActivity.ServiceConnection.Binder?.Service.QueueObject.IsShuffled ?? false), PendingIntentFlags.Mutable
                      )
                  );

                if(MainActivity.ServiceConnection.Binder?.Service?.QueueObject.ShowPrevious ?? false){
                    notificationBuilder.AddAction(
                        Resource.Drawable.previous, "Previous",
                        PendingIntent.GetBroadcast(AndroidApp.Context, 0, new Intent(MyMediaBroadcastReceiver.PREVIOUS_SONG, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver)), PendingIntentFlags.Mutable)
                    );
                }

                if (MainActivity.ServiceConnection.Binder?.Service?.IsPlaying ?? false)
                {
                    notificationBuilder.AddAction(
                        Resource.Drawable.pause_fill1_wght200_grad200_opsz48, "pause",
                        PendingIntent.GetBroadcast(AndroidApp.Context, 0, new Intent(MyMediaBroadcastReceiver.PAUSE, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver)), PendingIntentFlags.Mutable)
                    );
                }
                else
                {
                    notificationBuilder.AddAction(
                        Resource.Drawable.play, "play",
                        PendingIntent.GetBroadcast(AndroidApp.Context, 0, new Intent(MyMediaBroadcastReceiver.PLAY, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver)), PendingIntentFlags.Mutable)
                    );
                }
                if(MainActivity.ServiceConnection.Binder?.Service?.QueueObject.ShowNext ?? false){
                    notificationBuilder.AddAction(
                        Resource.Drawable.next, "next",
                        PendingIntent.GetBroadcast(AndroidApp.Context, 0, new Intent(MyMediaBroadcastReceiver.NEXT_SONG, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver)), PendingIntentFlags.Mutable)
                    );
                }

                LoopState loopState = MainActivity.ServiceConnection.Binder?.Service?.QueueObject.LoopState ?? LoopState.None;
                switch (loopState)
                {
                    case LoopState.None:
#if DEBUG
                        MyConsole.WriteLine("no_repeat >>>>>>>>");
#endif
                        notificationBuilder.AddAction(
                            Resource.Drawable.no_repeat, "no_repeat",
                            PendingIntent.GetBroadcast(
                                AndroidApp.Context, (int)loopState,
                                new Intent(MyMediaBroadcastReceiver.TOGGLE_LOOP, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver))
                                    .PutExtra("loopState", 1), PendingIntentFlags.Mutable
                            )
                        );
                        break;
                    case LoopState.All:
#if DEBUG
                        MyConsole.WriteLine("repeat >>>>>>>>");
#endif
                        notificationBuilder.AddAction(
                            Resource.Drawable.repeat, "repeat",
                            PendingIntent.GetBroadcast(
                                AndroidApp.Context, (int)loopState,
                                new Intent(MyMediaBroadcastReceiver.TOGGLE_LOOP, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver))
                                    .PutExtra("loopState", 2), PendingIntentFlags.Mutable
                            )
                        );
                        break;
                    case LoopState.Single:
#if DEBUG
                        MyConsole.WriteLine("repeat_one >>>>>>>>");
#endif  
                        notificationBuilder.AddAction(
                            Resource.Drawable.repeat_one, "repeat_one",
                            PendingIntent.GetBroadcast(
                                AndroidApp.Context, (int)loopState,
                                new Intent(MyMediaBroadcastReceiver.TOGGLE_LOOP, null!, AndroidApp.Context, typeof(MyMediaBroadcastReceiver))
                                    .PutExtra("loopState", 0), PendingIntentFlags.Mutable
                            )
                        );
                        break;
                }
                notification = notificationBuilder.Build();
            }
            manager.Notify(notification_id, notification);
#if DEBUG
            MyConsole.WriteLine("NOTIFY RELOAD");
#endif
        }

        private void create_notification_channel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            NotificationChannel channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low)
            {
                Description = ChannelDescription
            };

            NotificationManager? managerLocal = (NotificationManager?)AndroidApp.Context.GetSystemService(Context.NotificationService);
            managerLocal?.CreateNotificationChannel(channel);
        }

        public void song_control_notification(MediaSessionCompat.Token token)
        {
            if (is_created)
                return;

            if (!is_channel_init)
            {
                create_notification_channel();
            }
            
            Intent songsIntent = new Intent(AndroidApp.Context, typeof(MainActivity)).PutExtra("action", "openDrawer");
            /*TaskStackBuilder stackBuilder = TaskStackBuilder.Create(AndroidApp.Context);
            stackBuilder.AddNextIntentWithParentStack(songsIntent);
            PendingIntent songsPendingIntent =
                stackBuilder.GetPendingIntent(0,
                    (int) (PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable));*/


            notificationBuilder = new NotificationCompat.Builder(AndroidApp.Context, ChannelId)
                .SetSmallIcon(
                    Resource.Drawable.music
                )
                .SetShowWhen(false)
                .SetContentIntent(PendingIntent.GetActivity(AndroidApp.Context, 57, songsIntent, PendingIntentFlags.Immutable))
                .SetStyle(new AndroidX.Media.App.NotificationCompat.MediaStyle().SetMediaSession(token));

            manager = NotificationManagerCompat.From(AndroidApp.Context);
            notification = notificationBuilder.Build();
            manager.Notify(notification_id, notification);
            is_created = true;
        }


        public void destroy_song_control()
        {
            if (!is_created) return;
            manager.Cancel(notification_id);
            is_created = false;
        }
    }
}