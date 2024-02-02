using System;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidApp = Android.App.Application;

namespace MWP.BackEnd.Network
{
    internal class Notifications
    {
        private int NotificationId { get; }
        
        private const string CHANNEL_ID_LOW_IMPORTANCE = "network_notification_channel";
        private const string CHANNEL_ID_HIGH_IMPORTANCE = "network_notification_channel_priority";
        private const string CHANNEL_NAME_LOW_IMPORTANCE = "Network Notifications";
        private const string CHANNEL_NAME_HIGH_IMPORTANCE = "Priority Network Notifications";
        private const string CHANNEL_DESCRIPTION_LOW_IMPORTANCE = "Notification for network activity";
        private const string CHANNEL_DESCRIPTION_HIGH_IMPORTANCE = "Notification for high priority network activity";
        //private const int    SUMARRY_ID          = 1147;
        //private const string GROUP_KEY           = "com.android.ass_pain.NETWORK_GROUP";

        private readonly NotificationTypes notificationType;

        private readonly NotificationCompat.Builder notificationBuilder;
        private readonly NotificationManagerCompat manager;

        private readonly NotificationImportance notificationImportance;
        private readonly string channelId;
        private readonly string channelName;
        private readonly string channelDescription;
        private readonly bool enableVibrations;

        internal Notifications(NotificationTypes notificationType)
        {
            this.notificationType = notificationType;
            
            (
                notificationImportance,
                channelId,
                channelName,
                channelDescription,
                enableVibrations
            ) = notificationType switch
            {
                NotificationTypes.OneTimeSend => (NotificationImportance.Low, CHANNEL_ID_LOW_IMPORTANCE, CHANNEL_NAME_LOW_IMPORTANCE, CHANNEL_DESCRIPTION_LOW_IMPORTANCE, false),
                NotificationTypes.OneTimeReceive => (NotificationImportance.High, CHANNEL_ID_HIGH_IMPORTANCE, CHANNEL_NAME_HIGH_IMPORTANCE, CHANNEL_DESCRIPTION_HIGH_IMPORTANCE, true),
                NotificationTypes.Sync => (NotificationImportance.Low, CHANNEL_ID_LOW_IMPORTANCE, CHANNEL_NAME_LOW_IMPORTANCE, CHANNEL_DESCRIPTION_LOW_IMPORTANCE, false),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            NotificationId = RandomId();
            CreateNotificationChannel();
            manager = NotificationManagerCompat.From(AndroidApp.Context);
            notificationBuilder = new NotificationCompat.Builder(AndroidApp.Context, channelId);
        }
        
        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }
            NotificationChannel channel = new NotificationChannel(channelId, channelName, notificationImportance)
            {
                Description = channelDescription,
                LockscreenVisibility = NotificationVisibility.Public
            };
            channel.EnableVibration(enableVibrations);

            NotificationManager? managerLocal = (NotificationManager?)AndroidApp.Context.GetSystemService(Context.NotificationService);
            managerLocal?.CreateNotificationChannel(channel);
        }

        private static int RandomId()
        {
            int randomId = StateHandler.Rng.Next(10000);
            while (MainActivity.StateHandler.NotificationIDs.Contains(randomId))
            {
                randomId = StateHandler.Rng.Next(10000);
            }

            return randomId;
        }
        
        
        internal void Stage1(string remoteHostname)
        {
            notificationBuilder
                //TODO: icon
                .SetSmallIcon(
                    Resource.Drawable.download
                )
                .SetShowWhen(false)
                .SetOngoing(true);
            switch (notificationType)
            {
                case NotificationTypes.OneTimeSend:
                case NotificationTypes.Sync:
                    notificationBuilder
                        .SetContentTitle($"Connected to {remoteHostname}")
                        .SetAutoCancel(false)
                        .SetSilent(true);
                    break;
                case NotificationTypes.OneTimeReceive:
                    notificationBuilder
                        .SetContentTitle($"{remoteHostname} wants to connect to you")
                        //.SetContentIntent()
                        .SetAutoCancel(true)
                        .SetSilent(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            manager.Notify(NotificationId, notificationBuilder.Build());
        }
        
        internal void Stage1Update(string remoteHostname, int songCount)
        {
            switch (notificationType)
            {
                case NotificationTypes.OneTimeSend:
                case NotificationTypes.Sync:
                    return;
                case NotificationTypes.OneTimeReceive:
                    notificationBuilder
                        .SetContentTitle($"{remoteHostname} wants to send you {songCount} songs")
                        //.SetContentIntent()
                        .SetShowWhen(false)
                        .SetOngoing(true)
                        .SetAutoCancel(true)
                        .SetSilent(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            manager.Notify(NotificationId, notificationBuilder.Build());
        }

        internal void Stage2(ConnectionState connectionState)
        {
            notificationBuilder
                .SetShowWhen(false)
                .SetAutoCancel(false)
                .SetOngoing(true)
                .SetSilent(true);
            switch (notificationType)
            {
                case NotificationTypes.OneTimeSend:
                    notificationBuilder
                        .SetContentTitle($"Sending songs to {connectionState.remoteHostname}")
                        .SetProgress(connectionState.oneTimeSendCount, connectionState.oneTimeSentCount, false);
                    break;
                case NotificationTypes.Sync:
                    notificationBuilder
                        .SetContentTitle($"Syncing with {connectionState.remoteHostname}")
                        .SetProgress(connectionState.TotalSyncCount, connectionState.SyncCount, false);
                    break;
                case NotificationTypes.OneTimeReceive:
                    notificationBuilder
                        .SetContentTitle($"Receiving songs from {connectionState.remoteHostname}")
                        .SetProgress(connectionState.oneTimeReceiveCount, connectionState.oneTimeReceivedCount, false);
                        
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            manager.Notify(NotificationId, notificationBuilder.Build());

        }

        internal void Stage3(bool succeeded, ConnectionState connectionState)
        {
            notificationBuilder
                .SetProgress(0, 0, false)
                .SetShowWhen(false)
                .SetAutoCancel(true)
                .SetOngoing(false)
                .SetSilent(true);
            if (succeeded)
            {
                notificationBuilder
                    .SetContentTitle($"Finished transfer with {connectionState.remoteHostname}");
            }
            else
            {
                notificationBuilder
                    .SetContentTitle($"Transfer with {connectionState.remoteHostname} failed");
                switch (notificationType)
                {
                    case NotificationTypes.OneTimeSend:
                        notificationBuilder
                            .SetContentText($"Sent {connectionState.oneTimeSentCount}/{connectionState.oneTimeSendCount} songs");
                        break;
                    case NotificationTypes.Sync:
                        notificationBuilder
                            .SetContentText(
                                $"Sent {connectionState.syncSentCount}/{connectionState.syncSendCount} songs {System.Environment.NewLine} Received {connectionState.syncReceiveCount}/{connectionState.syncReceivedCount} songs");
                        break;
                    case NotificationTypes.OneTimeReceive:
                        notificationBuilder
                            .SetContentText($"Received {connectionState.oneTimeSentCount}/{connectionState.oneTimeSendCount} songs");
                        
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

        }
    }
}