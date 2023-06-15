using System;
using Android.App;
using Android.Widget;
using AndroidX.Core.App;
//using static Android.Renderscripts.ScriptGroup;
using AndroidApp = Android.App.Application;

namespace Ass_Pain
{
    public class DownloadNotification
    {
        private int NOTIFICATION_ID { get; }
        private readonly string CHANNEL_ID = "location_notification";

        private bool isSingle;

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

        public DownloadNotification(bool isSingle) : this()
        {
            this.isSingle = isSingle;
            manager = NotificationManagerCompat.From(AndroidApp.Context);

            Progress<double> prog = new Progress<double>();
            stage1_song(prog, "something");
        }

        public void stage1_playlist(Progress<double> progress, string title, int? poradieVPlayliste = null)
        {
            progress.ProgressChanged += delegate(object sender, double d)
            {
                
            };
        }

        public void stage1_song(Progress<double> progress, string title)
        {
            RemoteViews view = new RemoteViews(Application.Context.PackageName,
                Resource.Layout.download_notification_single);
            
            Notification notification = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID)
                .SetSmallIcon(Resource.Drawable.ic_menu_camera)
                .SetCustomContentView(view)
                .Build();
            
            manager.Notify(NOTIFICATION_ID, notification);
        }

        

        public void stage2_song(int precentage)
        {
            
        }
        public void stage2_playlist(int precentage, int? poradieVPlayliste)
        {
            
        }
        
        
        public void stage3_song()
        {
            
        }
        public void stage3_playlist(int? poradieVPlayliste)
        {
            
        }
        
        
        
        public void stage4_song(bool success, string message)
        {
            
        }
        public void stage4_playlist(bool success, string message, int? poradieVPlayliste)
        {
            
        }
        
        
        
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