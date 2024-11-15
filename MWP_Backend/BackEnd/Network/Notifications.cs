#if ANDROID
using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using AndroidX.Core.App;
using MWP_Backend.BackEnd;
using MWP_Backend.BackEnd.Helpers;
using MWP.Helpers;
using AndroidApp = Android.App.Application;

namespace MWP.BackEnd.Network
{
    /// <summary>
    /// Manages notifications for NetworkManager and receives broadcasts from said notifications
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { ConnectionAccepted, ConnectionRejected })]
    public class Notifications : BroadcastReceiver
    {
        private readonly int notificationId = -1;
        
        private const string CHANNEL_ID_LOW_IMPORTANCE = "network_notification_channel";
        private const string CHANNEL_ID_HIGH_IMPORTANCE = "network_notification_channel_priority";
        private const string CHANNEL_NAME_LOW_IMPORTANCE = "Network Notifications";
        private const string CHANNEL_NAME_HIGH_IMPORTANCE = "Priority Network Notifications";
        private const string CHANNEL_DESCRIPTION_LOW_IMPORTANCE = "Notification for network activity";
        private const string CHANNEL_DESCRIPTION_HIGH_IMPORTANCE = "Notification for high priority network activity";

        private readonly NotificationTypes notificationType;

        private readonly NotificationCompat.Builder? notificationBuilder;
        private readonly NotificationManagerCompat manager = NotificationManagerCompat.From(AndroidApp.Context);

        private readonly NotificationImportance notificationImportance;
        private readonly string? channelId;
        private readonly string? channelName;
        private readonly string? channelDescription;
        private readonly bool enableVibrations;

        private const string ConnectionAccepted = "ConnectionAccepted";
        private const string ConnectionRejected = "ConnectionRejected";

        /// <summary>
        /// Default constructor for system
        /// </summary>
        public Notifications()
        {
            //ignored
            //Needed for system for broadcast receiver
        }

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
            
            notificationId = RandomId();
            CreateNotificationChannel();
            notificationBuilder = new NotificationCompat.Builder(AndroidApp.Context, channelId);

            
            IntentFilter intentFilter = new IntentFilter();
            intentFilter.AddAction(ConnectionAccepted);
            intentFilter.AddAction(ConnectionRejected);
            AndroidApp.Context.RegisterReceiver(this, intentFilter);
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
            notificationBuilder?
                .SetSmallIcon(
                    Resource.Mipmap.ic_launcher_round
                )
                .SetLargeIcon(
                    BitmapFactory.DecodeResource(AndroidApp.Context.Resources, Resource.Mipmap.ic_launcher_round)
                )
                .SetShowWhen(false)
                .SetOngoing(true);
            switch (notificationType)
            {
                case NotificationTypes.OneTimeSend:
                case NotificationTypes.Sync:
                    notificationBuilder?
                        .SetContentTitle($"Connected to {remoteHostname}")
                        .SetAutoCancel(false)
                        .SetSilent(true);
                    break;
                case NotificationTypes.OneTimeReceive:
                    Intent intentOpenMainActivity = new Intent(AndroidApp.Context, typeof(MainActivity));
                    intentOpenMainActivity.PutExtra("NotificationAction", "ShowConnectionStatus");
                    intentOpenMainActivity.PutExtra("RemoteHostname", remoteHostname);
                    PendingIntent? pendingIntentOpenMainActivity = PendingIntent.GetActivity(AndroidApp.Context, notificationId, intentOpenMainActivity, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                    
                    Intent intentConnectionAccepted = new Intent(AndroidApp.Context, typeof(Notifications));
                    intentConnectionAccepted.SetAction(ConnectionAccepted);
                    intentConnectionAccepted.PutExtra("RemoteHostname", remoteHostname);
                    intentConnectionAccepted.PutExtra("NotificationId", notificationId);
                    PendingIntent? pendingIntentConnectionAccepted = PendingIntent.GetBroadcast(AndroidApp.Context, notificationId, intentConnectionAccepted, PendingIntentFlags.Immutable);
                    
                    Intent intentConnectionRejected = new Intent(AndroidApp.Context, typeof(Notifications));
                    intentConnectionRejected.SetAction(ConnectionRejected);
                    intentConnectionRejected.PutExtra("RemoteHostname", remoteHostname);
                    intentConnectionRejected.PutExtra("NotificationId", notificationId);
                    PendingIntent? pendingIntentConnectionRejected = PendingIntent.GetBroadcast(AndroidApp.Context, notificationId, intentConnectionRejected, PendingIntentFlags.Immutable);
                    
                    notificationBuilder?
                        .SetContentTitle($"{remoteHostname} wants to connect to your device")
                        .SetContentIntent(pendingIntentOpenMainActivity)
                        .SetAutoCancel(true)
                        .SetSilent(false)
                        .AddAction(Resource.Drawable.play, "Accept", pendingIntentConnectionAccepted)
                        .AddAction(Resource.Drawable.cross, "Reject", pendingIntentConnectionRejected);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (notificationBuilder != null) manager.Notify(notificationId, notificationBuilder.Build());
        }
        
        internal void Stage1Update(string remoteHostname, int songCount)
        {
            switch (notificationType)
            {
                case NotificationTypes.OneTimeSend:
                case NotificationTypes.Sync:
                    return;
                case NotificationTypes.OneTimeReceive:
                    Intent intent = new Intent(AndroidApp.Context, typeof(MainActivity));
                    intent.PutExtra("NotificationAction", "ShowSongList");
                    intent.PutExtra("RemoteHostname", remoteHostname);
                    PendingIntent? pendingIntent = PendingIntent.GetActivity(AndroidApp.Context, notificationId, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                    notificationBuilder?
                        .SetContentTitle($"{remoteHostname} wants to send you {songCount} songs")
                        .SetContentIntent(pendingIntent)
                        .SetShowWhen(false)
                        .SetOngoing(true)
                        .SetAutoCancel(true)
                        .SetSilent(false)
                        .ClearActions();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (notificationBuilder != null) manager.Notify(notificationId, notificationBuilder.Build());
        }

        internal void Stage2(ConnectionState connectionState)
        {
            notificationBuilder?
                .SetShowWhen(false)
                .SetAutoCancel(false)
                .SetOngoing(true)
                .SetSilent(true)
                .ClearActions()
                .SetSmallIcon(
                    Resource.Mipmap.ic_launcher_round
                )
                .SetLargeIcon(
                    BitmapFactory.DecodeResource(AndroidApp.Context.Resources, Resource.Mipmap.ic_launcher_round)
                );
            switch (notificationType)
            {
                case NotificationTypes.OneTimeSend:
                    notificationBuilder?
                        .SetContentTitle($"Sending songs to {connectionState.remoteHostname}")
                        .SetProgress(connectionState.oneTimeSendCount, connectionState.oneTimeSentCount, false);
                    break;
                case NotificationTypes.Sync:
                    notificationBuilder?
                        .SetContentTitle($"Syncing with {connectionState.remoteHostname}")
                        .SetProgress(connectionState.TotalSyncCount, connectionState.SyncCount, false);
                    break;
                case NotificationTypes.OneTimeReceive:
                    notificationBuilder?
                        .SetContentTitle($"Receiving songs from {connectionState.remoteHostname}")
                        .SetProgress(connectionState.oneTimeReceiveCount, connectionState.oneTimeReceivedCount, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (notificationBuilder != null) manager.Notify(notificationId, notificationBuilder.Build());
        }
        
        internal void Stage2Update(ConnectionState connectionState)
        {
            switch (notificationType)
            {
                case NotificationTypes.OneTimeSend:
                    notificationBuilder?
                        .SetProgress(connectionState.oneTimeSendCount, connectionState.oneTimeSentCount, false);
                    break;
                case NotificationTypes.Sync:
                    notificationBuilder?
                        .SetProgress(connectionState.TotalSyncCount, connectionState.SyncCount, false);
                    break;
                case NotificationTypes.OneTimeReceive:
                    notificationBuilder?
                        .SetProgress(connectionState.oneTimeReceiveCount, connectionState.oneTimeReceivedCount, false)
                        .ClearActions();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (notificationBuilder != null) manager.Notify(notificationId, notificationBuilder.Build());
        }

        internal void Stage3(bool succeeded, ConnectionState connectionState)
        {
            notificationBuilder?
                .SetProgress(0, 0, false)
                .SetShowWhen(false)
                .SetAutoCancel(true)
                .SetOngoing(false)
                .SetSilent(true)
                .ClearActions()
                .SetSmallIcon(
                    Resource.Mipmap.ic_launcher_round
                )
                .SetLargeIcon(
                    BitmapFactory.DecodeResource(AndroidApp.Context.Resources, Resource.Mipmap.ic_launcher_round)
                );
            
            notificationBuilder?
                .SetContentTitle(succeeded
                    ? $"Finished transfer with {connectionState.remoteHostname}"
                    : $"Transfer with {connectionState.remoteHostname} failed");
            
            switch (notificationType)
            {
                case NotificationTypes.OneTimeSend:
                    if (connectionState.oneTimeSentCount < connectionState.oneTimeSendCount)
                    {
                        notificationBuilder?
                        .SetContentText($"Sent {connectionState.oneTimeSentCount}/{connectionState.oneTimeSendCount} songs");
                    }
                    break;
                case NotificationTypes.Sync:
                    if (connectionState.syncSentCount < connectionState.SyncSendCount || connectionState.syncReceiveCount < connectionState.syncReceivedCount)
                    {
                        notificationBuilder?
                        .SetContentText(
                            $"Sent {connectionState.syncSentCount}/{connectionState.SyncSendCount} songs {System.Environment.NewLine} Received {connectionState.syncReceiveCount}/{connectionState.syncReceivedCount} songs");
                    }
                    break;
                case NotificationTypes.OneTimeReceive:
                    if (connectionState.oneTimeSentCount < connectionState.oneTimeSendCount)
                    {
                        notificationBuilder?
                        .SetContentText($"Received {connectionState.oneTimeSentCount}/{connectionState.oneTimeSendCount} songs");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (notificationBuilder != null) manager.Notify(notificationId, notificationBuilder.Build());
        }

        /// <inheritdoc />
        public override void OnReceive(Context? context, Intent? intent)
        {
#if DEBUG
            if (intent == null)
            {
                MyConsole.WriteLine("Notification No Intent");
            }
#endif
            string? remoteHostname;
            int notifId;
            switch (intent?.Action)
            {
                case ConnectionAccepted:
                    remoteHostname = intent.GetStringExtra("RemoteHostname");
                    notifId = intent.GetIntExtra("NotificationId", -1);
                    if (remoteHostname != null && notifId != -1 && notifId != notificationId)
                    {
                        StateHandler.OneTimeSendStates[remoteHostname] = UserAcceptedState.ConnectionAccepted;
                        NotificationManagerCompat mgr = NotificationManagerCompat.From(AndroidApp.Context);
                        mgr.Cancel(notifId);
                        NotificationCompat.Builder notifBuilder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID_HIGH_IMPORTANCE);
                        notifBuilder
                            .SetSmallIcon(
                                Resource.Mipmap.ic_launcher_round
                            )
                            .SetLargeIcon(
                                BitmapFactory.DecodeResource(AndroidApp.Context.Resources, Resource.Mipmap.ic_launcher_round)
                            )
                            .SetShowWhen(false)
                            .SetOngoing(true)
                            .SetAutoCancel(false)
                            .SetSilent(true)
                            .SetContentTitle($"Connected to {remoteHostname}");
                        mgr.Notify(notifId, notifBuilder.Build());
                    }
#if DEBUG
                    else
                    {
                        MyConsole.WriteLine("Notification No Action");
                        
                    }
#endif
                    break;
                case ConnectionRejected:
                    remoteHostname = intent.GetStringExtra("RemoteHostname");
                    notifId = intent.GetIntExtra("NotificationId", -1);
                    if (remoteHostname != null && notifId != -1 && notifId != notificationId)
                    {
                        StateHandler.OneTimeSendStates[remoteHostname] = UserAcceptedState.Cancelled;
                        NotificationManagerCompat mgr = NotificationManagerCompat.From(AndroidApp.Context);
                        mgr.Cancel(notifId);
                        NotificationCompat.Builder notifBuilder = new NotificationCompat.Builder(AndroidApp.Context, CHANNEL_ID_HIGH_IMPORTANCE);
                        notifBuilder
                            .SetSmallIcon(
                                Resource.Mipmap.ic_launcher_round
                            )
                            .SetLargeIcon(
                                BitmapFactory.DecodeResource(AndroidApp.Context.Resources, Resource.Mipmap.ic_launcher_round)
                            )
                            .SetShowWhen(false)
                            .SetOngoing(true)
                            .SetAutoCancel(false)
                            .SetSilent(true)
                            .SetContentTitle($"Connected to {remoteHostname}");
                        mgr.Notify(notifId, notifBuilder.Build());
                    }
#if DEBUG
                    else
                    {
                        MyConsole.WriteLine("Notification No Action");
                    }
#endif
                    break;
                default:
#if DEBUG
                    MyConsole.WriteLine("Unknown action");
#endif
                    break;
            }
        }
        
        private void ReleaseUnmanagedResources()
        {
            AndroidApp.Context.UnregisterReceiver(this);
            MainActivity.StateHandler.NotificationIDs.Remove(notificationId);
        }

        /// <summary>
        /// Releases memory
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resource</param>
        protected override void Dispose(bool disposing)
        {
#if DEBUG
            MyConsole.WriteLine("Disposing notification object");
#endif
            ReleaseUnmanagedResources();
            if (disposing)
            {
                notificationBuilder?.Dispose();
                manager.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
#endif