using System;
using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.Core.App;
//using static Android.Renderscripts.ScriptGroup;
using AndroidApp = Android.App.Application;

#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain
{
    public class DownloadNotification
    {
        private int NOTIFICATION_ID { get; }
        
        private const string CHANNEL_ID = "local_notification_channel";
        private const string CHANNEL_NAME = "Notifications";
        private const string CHANNEL_DESCRIPTION = "description";

        private bool isSingle = true;
        private int videoCount = 1;
        private string currentSongTitle;

        NotificationCompat.Builder notificationBuilder;
        private NotificationManagerCompat manager;

        public DownloadNotification()
        {
            int randomId = StateHandler.Rng.Next(10000);
            while (MainActivity.stateHandler.NotificationIDs.Contains(randomId))
            {
                randomId = StateHandler.Rng.Next(10000);
            }
            NOTIFICATION_ID = randomId;
        }

        public DownloadNotification(int cnt) : this()
        {
            if(cnt == 0)
                return;

            isSingle = false;
            videoCount = cnt;
            manager = NotificationManagerCompat.From(AndroidApp.Context);

        }

        public DownloadNotification(bool isSingle) : this()
        {
            this.isSingle = isSingle;
            manager = NotificationManagerCompat.From(AndroidApp.Context);
            
        }

        private void create_notification_channel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            NotificationChannel channel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.Low)
            {
                Description = CHANNEL_DESCRIPTION
            };

            NotificationManager manager = (NotificationManager)AndroidApp.Context.GetSystemService(AndroidApp.NotificationService);
            manager.CreateNotificationChannel(channel);
        }
        
        
        /*
         * stage 1
         * progress bar
         */
        private void stage1_song(Progress<double> progress, string title)
        {
            this.currentSongTitle = title;
            create_notification_channel();
            
            RemoteViews view = new RemoteViews(Application.Context.PackageName,
                Resource.Layout.download_notification_single);

            manager = NotificationManagerCompat.From(AndroidApp.Context);
            
            progress.ProgressChanged += delegate(object sender, double d)
            {
                notificationBuilder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
                    .SetSmallIcon(
                        Resource.Drawable.ic_menu_camera
                    )
                    .SetContentTitle("Initializing")
                    .SetContentText(currentSongTitle)
                    .SetOngoing(true)
                    .SetShowWhen(false);
                notificationBuilder.SetProgress(100, (int)(d * 100), false);
                
#if DEBUG
                Helpers.MyConsole.WriteLine(((int)(d * 100)).ToString());
#endif
                
                // Notification notification = notificationBuilder.Build();
                manager.Notify(NOTIFICATION_ID, notificationBuilder.Build());
            };
            

        }
        private void stage1_playlist(Progress<double> progress, string title, int? poradieVPlayliste = null)
        {
            progress.ProgressChanged += delegate(object sender, double d)
            {
                
            };
        }

        

        /*
         * stage 2
         * progress bar
         */
        private void stage2_song(int percentage)
        {
            notificationBuilder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
                .SetSmallIcon(
                    Resource.Drawable.ic_menu_camera
                )
                .SetContentTitle("Downloading")
                .SetContentText(currentSongTitle)
                .SetOngoing(true)
                .SetShowWhen(false);
            notificationBuilder.SetProgress(100, percentage, false);
                
#if DEBUG
            Helpers.MyConsole.WriteLine(percentage.ToString());
#endif
                
            // Notification notification = notificationBuilder.Build();
            manager.Notify(NOTIFICATION_ID, notificationBuilder.Build());
        }
        private void stage2_playlist(int percentage, int? poradieVPlayliste)
        {
            
        }
        
        
        /*
         * stage 3
         * text
         */
        private void stage3_song()
        {
            notificationBuilder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
                .SetSmallIcon(
                    Resource.Drawable.ic_menu_camera
                )
                .SetContentTitle("Processing...")
                .SetContentText(currentSongTitle)
                .SetOngoing(true)
                .SetShowWhen(false);

            // Notification notification = notificationBuilder.Build();
            manager.Notify(NOTIFICATION_ID, notificationBuilder.Build());
        }
        private void stage3_playlist(int? poradieVPlayliste)
        {
            
        }
        
        
        /*
         * stage 4
         * text
         */
        private void stage4_song(bool success, string message)
        {
            notificationBuilder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
                .SetSmallIcon(
                    Resource.Drawable.ic_menu_camera
                )
                .SetContentText(currentSongTitle)
                .SetOngoing(true)
                .SetShowWhen(false);
            if (success)
            {
                notificationBuilder.SetContentTitle("Success");
                manager.Notify(NOTIFICATION_ID, notificationBuilder.Build());
            }
            else
            {
                notificationBuilder.SetContentTitle("Fail");
                manager.Notify(NOTIFICATION_ID, notificationBuilder.Build());
            }
            
        }
        private void stage4_playlist(bool success, string message, int? poradieVPlayliste)
        {
            
        }
        
        
      
        /// <summary>
        /// stage 1 of song downloading, progress bar
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="title"></param>
        /// <param name="poradieVPlayliste"></param>
        public void Stage1(Progress<double> progress, string title, int? poradieVPlayliste = null)
        {
            if (!isSingle)
            {
                if(poradieVPlayliste == null)
                    return;
                stage1_playlist(progress, title, poradieVPlayliste);
            }
            else
            {
                stage1_song(progress, title);
            }
        }
        /// <summary>
        /// stage 2 of song downloading, progress bar, but with percentage
        /// </summary>
        /// <param name="precentage"></param>
        /// <param name="poradieVPlayliste"></param>
        public void Stage2(int precentage, int? poradieVPlayliste = null)
        {
            if (!isSingle)
            {
                if(poradieVPlayliste == null)
                    return;
                stage2_playlist(precentage, poradieVPlayliste);
            }
            else
            {
                stage2_song(precentage);
            }
        }
        /// <summary>
        /// stage 3 is for song download processing notification text status
        /// </summary>
        /// <param name="poradieVPlayliste"></param>
        public void Stage3(int? poradieVPlayliste = null)
        {
            if (!isSingle)
            {
                if(poradieVPlayliste == null)
                    return;
                stage3_playlist(poradieVPlayliste);
            }
            else
            {
                stage3_song();
            }
        }
        /// <summary>
        /// stage 4 is final message for user to notify him about song download completition
        /// </summary>
        /// <param name="success"></param>
        /// <param name="message"></param>
        /// <param name="poradieVPlayliste"></param>
        public void Stage4(bool success, string message, int? poradieVPlayliste = null)
        {
            if (!isSingle)
            {
                if(poradieVPlayliste == null)
                    return;
                stage4_playlist(success, message, poradieVPlayliste);
            }
            else
            {
                stage4_song(success, message);
            }
        }
        
    }
}