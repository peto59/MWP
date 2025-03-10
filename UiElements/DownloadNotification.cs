using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.Core.App;
using MWP.BackEnd;
//using static Android.Renderscripts.ScriptGroup;
using AndroidApp = Android.App.Application;

#if DEBUG
using MWP.Helpers;
#endif

namespace MWP
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
            manager = NotificationManagerCompat.From(AndroidApp.Context);
            notificationBuilder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID);
        }

        public DownloadNotification(int cnt) : this()
        {
            if(cnt == 0)
                return;

            isSingle = false;
            videoCount = cnt;

            if (!isSingle)
            {
                currentSongTitles = new string[cnt+1];
                playlistNotifIDS = new int[cnt+1];
                
                Notification summaryNotification = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
                    .SetContentTitle("Song Donwload Summary")
                    .SetSmallIcon(Resource.Drawable.download)
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

            NotificationManager? managerLocal = (NotificationManager?)AndroidApp.Context.GetSystemService(Context.NotificationService);
            managerLocal?.CreateNotificationChannel(channel);
        }

        private static int RANDOM_ID()
        {
            int randomId = StateHandler.Rng.Next(10000);
            while (MainActivity.StateHandler.NotificationIDs.Contains(randomId))
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
            currentSongTitle = title;

            progress.ProgressChanged += delegate(object _, double d)
            {
                notificationBuilder
                    .SetSmallIcon(
                        Resource.Drawable.download
                    )
                    .SetContentTitle("Initializing")
                    .SetContentText(currentSongTitle)
                    .SetShowWhen(false);
                notificationBuilder.SetProgress(100, (int)(d * 100), false);
                manager.Notify(NOTIFICATION_ID, notificationBuilder.Build());
            };
            

        }
        private void stage1_playlist(Progress<double> progress, string title, int? poradieVPlayliste = null)
        {
            if (poradieVPlayliste != null) currentSongTitles[(int)poradieVPlayliste] = title;
            if (poradieVPlayliste != null) playlistNotifIDS[(int)poradieVPlayliste] = RANDOM_ID();
            progress.ProgressChanged += delegate(object _, double d)
            {
                if (poradieVPlayliste != null)
                {
                    notificationBuilder
                        .SetSmallIcon(
                            Resource.Drawable.download
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
            notificationBuilder
                .SetContentTitle("Downloading")
                .SetContentText(currentSongTitle)
                .SetOngoing(true)
                .SetShowWhen(false);
            notificationBuilder.SetProgress(100, percentage, false);
            manager.Notify(NOTIFICATION_ID, notificationBuilder.Build());
        }
        private void stage2_playlist(int percentage, int? poradieVPlayliste)
        {
            if (poradieVPlayliste != null)
            {
                notificationBuilder
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
            notificationBuilder
                .SetContentTitle("Processing...")
                .SetContentText(currentSongTitle)
                .SetShowWhen(false)
                .SetProgress(0, 0, false);
            manager.Notify(NOTIFICATION_ID, notificationBuilder.Build());
        }
        private void stage3_playlist(int? poradieVPlayliste)
        {
            if (poradieVPlayliste != null)
            {
                notificationBuilder
                    .SetContentTitle("Processing...")
                    .SetContentText(currentSongTitles[(int)poradieVPlayliste])
                    .SetGroup(GROUP_KEY)
                    .SetProgress(0, 0, false)
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
            notificationBuilder
                .SetSmallIcon(Resource.Drawable.download)
                .SetContentText(currentSongTitle)
                .SetOngoing(false)
                .SetShowWhen(false)
                .SetProgress(0, 0, false);
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
            if (poradieVPlayliste != null)
            {
                notificationBuilder
                    .SetContentText(currentSongTitles[(int)poradieVPlayliste])
                    .SetGroup(GROUP_KEY)
                    .SetShowWhen(false)
                    .SetProgress(0, 0, false);

                if (success)
                {
                    notificationBuilder.SetContentTitle("Success");
                    manager.Notify(playlistNotifIDS[(int)poradieVPlayliste], notificationBuilder.Build());
                }
                else
                {
                    notificationBuilder.SetContentTitle("Failed");
                    manager.Notify(playlistNotifIDS[(int)poradieVPlayliste], notificationBuilder.Build());
                }

            }
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
        /// <param name="percentage"></param>
        /// <param name="poradieVPlayliste"></param>
        public void Stage2(int percentage, int? poradieVPlayliste = null)
        {
            if (!isSingle)
            {
                if(poradieVPlayliste == null)
                    return;
                stage2_playlist(percentage, poradieVPlayliste);
            }
            else
            {
                stage2_song(percentage);
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
        /// stage 4 is final message for user to notify him about song download completion
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