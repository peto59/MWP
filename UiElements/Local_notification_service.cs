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
        // Konštanty slúžiace na vytvorenie notifikačného kanálu unikátneho pre hudobnú notifikáciu
        private const string ChannelId = "local_notification_channel";
        private const string ChannelName = "Media Notification";
        private const string ChannelDescription = "Notification with media controls";

        private int notification_id = 1;
        public int NotificationId
        {
            get { return notification_id; }
        }

        private bool is_channel_init = false;
        private bool is_created = false;
        public bool IsCreated
        {
            get { return is_created; }
        }
        
        // NotificationBuilder slúži na vytvorenie samotnej notifikácie. Obsahuje nastavenia a vlastnosti naotifikácie ktoré nastavujeme
        NotificationCompat.Builder notificationBuilder;
        // Notification objekt obsahuje samotný už vytvorenú (postavenú) notifikáciu
        private Notification notification;
        // Notifikácia ktorá je presná kópia notification, s jediným rozdielom, že má getter a tak sa k nej pristupuje z pozadia aplikácie
        public Notification Notification
        {
            get { return notification; }
        }

        // NotificationManager slúži na vyobrazenie samotnej notification do notofikačného kanálu 
        NotificationManagerCompat manager;

        /// <summary>
        /// Táto metóda slúži na vyvolanie upozornenia na prehrávanie médií. Najprv sa skontroluje verzia SDK a prípadne sa vytvorí nový stav notifikácie.
        /// Potom sa pridávajú akcie na notifikáciu v závislosti od stavu prehrávania a nastavení opakovania.
        /// Akcia na zamiešanie sa pridá s odpovedajúcou ikonou a intentom pre vysielanie udalostí.
        /// Ak sú dostupné, pridávajú sa akcie pre predošlú a ďalšiu skladbu s príslušnými intentmi.
        /// Podľa stavu opakovania sa pridávajú akcie s odpovedajúcimi ikonami pre žiadne opakovanie, opakovanie všetkých a opakovanie jednej skladby.
        /// Notifikácia sa nakoniec zostaví a zobrazí správa o tom, že notifikácia bola znovu načítaná.
        /// </summary>
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

        /// <summary>
        /// Táto metóda slúži na vytvorenie notifikácie ovládania hudby.
        /// Ak notifikácia už bola vytvorená, metóda sa okamžite vráti.
        /// Ak ešte nebol inicializovaný kanál notifikácií, zavolá sa metóda na vytvorenie kanálu.
        /// Potom sa vytvorí intent pre otvorenie hlavného menu aplikácie.
        /// Následne sa vytvorí nová inštancia NotificationCompat.Builder s potrebnými parametrami, ako je ikona a intent slúžiaci na otvorenie aplikácie pri kliknutí na notifikáciu.
        /// Pre zobrazenie ovládania médií sa použije MediaStyle so zadanou MediaSession. Nakoniec sa notifikácia zostaví a zobrazí.
        /// </summary>
        /// <param name="token">token aktuálnej relácie prehrávania</param>
        public void song_control_notification(MediaSessionCompat.Token token)
        {
            if (is_created)
                return;

            if (!is_channel_init)
            {
                create_notification_channel();
            }
            
            Intent songsIntent = new Intent(AndroidApp.Context, typeof(MainActivity)).PutExtra("action", "openDrawer");
            
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


        /// <summary>
        /// Deštrukcia notifikácie a nastavenie stavu premennej existencie notifikácie
        /// </summary>
        public void destroy_song_control()
        {
            if (!is_created) return;
            manager.Cancel(notification_id);
            is_created = false;
        }
    }
}