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
        
        private const string CHANNEL_ID          = "local_notification_channel";
        private const string CHANNEL_NAME        = "Notifications";
        private const string CHANNEL_DESCRIPTION = "description";
        private const int    SUMARRY_ID          = 1147;
        private const string GROUP_KEY           = "com.android.ass_pain.DOWNNLOAD_GROUP";

        private bool isSingle = true;
        private int videoCount = 1;
        private int songFinalCount = 0;
        private string currentSongTitle;
        private string[] currentSongTitles;
        private int[] playlistNotifIDS;

        NotificationCompat.Builder notificationBuilder;
        private NotificationManagerCompat manager;

        public DownloadNotification()
        {
            NOTIFICATION_ID = RANDOM_ID();
            create_notification_channel();
        }

        public DownloadNotification(int cnt) : this()
        {
            if(cnt == 0)
                return;

            isSingle = false;
            videoCount = cnt;
            manager = NotificationManagerCompat.From(AndroidApp.Context);

            if (!isSingle)
            {
                currentSongTitles = new string[cnt];
                playlistNotifIDS = new int[cnt];
                
                Notification summaryNotification = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
                    .SetContentTitle("Song Donwload Summary")
                    .SetSmallIcon(Resource.Drawable.ic_menu_camera)
                    .SetContentText(songFinalCount + " / " + videoCount)
                    .SetGroup(GROUP_KEY)
                    .SetGroupSummary(true)
                    .Build();
                manager.Notify(SUMARRY_ID, summaryNotification);
            }

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

        private int RANDOM_ID()
        {
            int randomId = StateHandler.Rng.Next(10000);
            while (MainActivity.stateHandler.NotificationIDs.Contains(randomId))
            {
                randomId = StateHandler.Rng.Next(10000);
            }

            return randomId;
        }
        
        
        /*
         * stage 1
         * progress bar
         */
        private void stage1_song(Progress<double> progress, string title)
        {
            this.currentSongTitle = title;

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
            if (poradieVPlayliste != null) this.currentSongTitles[(int)poradieVPlayliste] = title;
            if (poradieVPlayliste != null) this.playlistNotifIDS[(int)poradieVPlayliste] = RANDOM_ID();
            progress.ProgressChanged += delegate(object sender, double d)
            {
                if (poradieVPlayliste != null)
                {
                    notificationBuilder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
                        .SetSmallIcon(
                            Resource.Drawable.ic_menu_camera
                        )
                        .SetContentTitle("Initializing")
                        .SetContentText(currentSongTitles[(int)poradieVPlayliste])
                        .SetGroup(GROUP_KEY)
                        .SetShowWhen(false);
                    notificationBuilder.SetProgress(100, (int)(d * 100), false);
                    manager.Notify(playlistNotifIDS[(int)poradieVPlayliste], notificationBuilder.Build());
                }
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
            if (poradieVPlayliste != null)
            {
#if DEBUG
                Helpers.MyConsole.WriteLine(poradieVPlayliste + " " + currentSongTitles[(int)poradieVPlayliste] + " " + playlistNotifIDS[(int)poradieVPlayliste]);
#endif
                
                notificationBuilder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
                    .SetSmallIcon(
                        Resource.Drawable.ic_menu_camera
                    )
                    .SetContentTitle("Downloading")
                    .SetContentText(currentSongTitles[(int)poradieVPlayliste])
                    .SetGroup(GROUP_KEY)
                    .SetShowWhen(false);
                notificationBuilder.SetProgress(100, percentage, false);
                manager.Notify(playlistNotifIDS[(int)poradieVPlayliste], notificationBuilder.Build());
            }
            
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
            manager.Notify(NOTIFICATION_ID, notificationBuilder.Build());
        }
        private void stage3_playlist(int? poradieVPlayliste)
        {
            if (poradieVPlayliste != null)
            {
                notificationBuilder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
                    .SetSmallIcon(
                        Resource.Drawable.ic_menu_camera
                    )
                    .SetContentTitle("Processing...")
                    .SetContentText(currentSongTitles[(int)poradieVPlayliste])
                    .SetGroup(GROUP_KEY)
                    .SetShowWhen(false);
                manager.Notify(playlistNotifIDS[(int)poradieVPlayliste], notificationBuilder.Build());
            }
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
                .SetOngoing(false)
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
        /// stage 1 of song downloading, initialization progress bar
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