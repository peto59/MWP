using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Widget;
using Java.Lang;
using MWP.BackEnd.Player;
using Bitmap = System.Drawing.Bitmap;
using Color = Android.Graphics.Color;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP
{
    /// <summary>
    /// Trieda slúžiaca na definovanie miniaplikácie widget. Je natívne poskitnutá frameworkom Xamarin. My ju už len override-ujeme.
    /// Používateľ má prístup k miniaplikácii v zozname widget-ov zariadenia používateľa. Widget MWP je označený ikonkou aplikácie.
    /// Po vytvorení widget-u ma pouźívateľ dostupný widget o veľkosti 4x1, čiže na celú širku obrazovku a 1 na výšku. V rámvi miniaplikácie
    /// používateľ smie kontrolovať a ovládať stav prehrávania skladieb.
    /// </summary>
    [BroadcastReceiver(Label = "Music Widget", Exported = false)]
    [IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/musicwidget_provider")]
    public class MusicWidget : AppWidgetProvider
    {
        private static readonly string WIDGET_SHUFFLE_TAG = "WIDGET_SHUFFLE_TAG";
        
        private static readonly string WIDGET_PREVIOUS_TAG = "WIDGET_PEVIOUS_TAG";
        private static readonly string WIDGET_PLAY_TAG = "WIDGET_PLAY_CLICK";
        private static readonly string WIDGET_NEXT_TAG = "WIDGET_NEXT_TAG";

        private static readonly string WIDGET_REPEAT_TAG = "WIDGET_REPEAT_TAG";

        /*
         * PendingIntent-y slúžiace na získanie události stlačenia tlačidla v rámci miniaplikacie 
         */
        private static PendingIntent? _shuffleIntent;
        private static PendingIntent? _previousIntent;
        private static PendingIntent? _playIntent;
        private static PendingIntent? _nextIntent;
        private static PendingIntent? _repeatIntent;

        
        /// <inheritdoc />
        public override void OnUpdate(Context? context, AppWidgetManager? manager, int[]? widgetIds)
        {
            if (context != null)
            {
                var me = new ComponentName(context, Class.FromType(typeof(MusicWidget)).Name);
                manager?.UpdateAppWidget(me, BuildRemoteView(context, widgetIds));
            }
        }
       
        /// <summary>
        /// Keďže miniaplikácia widget je aplikácia spustená nezávisle od hlavnej aplikácie MWP, k jednotlivým elementom musíme
        /// pristupovať prostredníctvom pozadia aplikácie. pomocou kontextu aplikácie získavame RemoteViews objekt obsahujúci
        /// elementy nachádzajúce sa vo widget-e. Ten následne predávame do metód ako SetTextViewText kde sa iniciualizujú texty
        /// TextView elementov z pozadia aplikácie, a metódy RegisterClicks ktorá slúži na inicializáciu potrebných PendingIntent-ov. 
        /// </summary>
        /// <param name="context">kontext pozadia aplikácie</param>
        /// <param name="widgetIds">Id jedno</param>
        /// <returns></returns>
        private RemoteViews BuildRemoteView(Context context, int[]? widgetIds)
        {
            var widgetView = new RemoteViews(context.PackageName, Resource.Layout.music_widget_layout);
            
            
            SetTextViewText(widgetView);
            RegisterClicks(context, widgetIds, widgetView);
            
            // vytvorenie zaobleného obrázka aktuálne prebiehajúcej skladby 
            Android.Graphics.Bitmap? squared = WidgetServiceHandler.CropBitmapToSquare(
                MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Image ??
                new Song("No Name", new DateTime(), "Default").Image, false);
            
            /*
             * Upravenie zaoblenia obrázka na základe veľkosti a rozlíšenia obrázka
             */
            int roundedSize;
            if (MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Image.Height == 360)
            {
                roundedSize = 50;
            }
            else
            {
                roundedSize = 90;
            }

            if (squared != null)
                widgetView.SetImageViewBitmap(Resource.Id.widgetImage,
                    WidgetServiceHandler.GetRoundedCornerBitmap(
                        squared,
                        roundedSize
                    )
                );

            // widgetView.SetInt(Resource.Id.widgetBackground, "setBackgroundColor", Color.Black);

            return widgetView;
        }

        /// <summary>
        /// Metóda slúži na nastavenie názvu skladby a názvu interpreta na príslušné TextView elementy.
        /// </summary>
        /// <param name="widgetView">RemoteViews objekt slúžiaci na získanie elementov z widgetu</param>
        private void SetTextViewText(RemoteViews widgetView)
        {
            widgetView.SetTextViewText(Resource.Id.widgetSongTitle, MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Title ?? "No Name");
            widgetView.SetTextViewText(Resource.Id.widgetSongArtist, MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Artist.Title ?? "No Artist");
        }
        
        /// <summary>
        /// Táto metóda registruje kliknutia pre ovládacie prvky widgetu.
        /// Vytvára nový intent pre triedu MusicWidget a nastavuje akciu pre aktualizáciu widgetu a pridáva identifikátory widgetov ako extra údaje.
        /// Následne získava pending intenty pre rôzne akcie, ako je zamiešanie, predošlá, prehrávanie, ďalšia a opakovanie.
        /// Tieto pending intenty sú priradené k odpovedajúcim tlačidlám na widgetView.
        /// Okrem toho sa registruje kliknutie na otvorenie aplikácie na kliknutie na pozadie widgetu.
        /// Nastavuje sa vhodný pendingIntentFlags na základe verzie Androidu.
        /// </summary>
        /// <param name="context">Kontext pozadia aplikácie</param>
        /// <param name="widgetIds">identifikátory widgetov (voliteľné)</param>
        /// <param name="widgetView">RemoteViews objekt slúžiaci na získanie elementov z widgetu</param>
        private void RegisterClicks(Context context, int[]? widgetIds, RemoteViews widgetView)
        {
            var intent = new Intent(context, typeof(MusicWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, widgetIds);
            
            // handle button clicks
            _shuffleIntent = GetPendingSelfIntent(context, WIDGET_SHUFFLE_TAG);
            _previousIntent = GetPendingSelfIntent(context, WIDGET_PREVIOUS_TAG);
            _playIntent = GetPendingSelfIntent(context, WIDGET_PLAY_TAG);
            _nextIntent = GetPendingSelfIntent(context, WIDGET_NEXT_TAG);
            _repeatIntent = GetPendingSelfIntent(context, WIDGET_REPEAT_TAG);
            
            
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetShuffleButton, _shuffleIntent);
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetPreviousButton, _previousIntent);
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetPlayButton, _playIntent);
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetNextButton, _nextIntent);
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetRepeatButton, _repeatIntent);
            
            
            /*
             * register music widget click for opening application on widget layout click
             */
            var pendingIntentFlags = (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                : PendingIntentFlags.UpdateCurrent;
            
            Intent mainAcitivtyIntent = new Intent(context, typeof(MainActivity));
            PendingIntent? pendingIntent = PendingIntent.GetActivity(context, 0, mainAcitivtyIntent, pendingIntentFlags);
            widgetView.SetOnClickPendingIntent(Resource.Id.widgetBackground, pendingIntent);
        }

        private PendingIntent? GetPendingSelfIntent(Context context, string action)
        {
            var intent = new Intent(context, typeof(MusicWidget));
            intent.SetAction(action);
            
            var pendingIntentFlags = (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                : PendingIntentFlags.UpdateCurrent;
            
            return PendingIntent.GetBroadcast(context, 0, intent, pendingIntentFlags);
        }


        /// <summary>
        /// Spustenie príslušnej události na základe typu príjmutého PendingIntent-u
        /// </summary>
        /// <param name="context"></param>
        /// <param name="intent"></param>
        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (WIDGET_PLAY_TAG.Equals(intent?.Action))
            {
                if (MainActivity.ServiceConnection.Connected)
                {
                    if (MainActivity.ServiceConnection.Binder?.Service.IsPlaying ?? false)
                    {
                        MainActivity.ServiceConnection.Binder?.Service.Pause();
                    }
                    else
                    {
                        MainActivity.ServiceConnection.Binder?.Service.Play();
                    }
                }
            }
            else if (WIDGET_PREVIOUS_TAG.Equals(intent?.Action))
            {
                MainActivity.ServiceConnection.Binder?.Service.PreviousSong();
            }
            else if (WIDGET_NEXT_TAG.Equals(intent?.Action))
            {
                MainActivity.ServiceConnection.Binder?.Service.NextSong();
            }
            else if (WIDGET_SHUFFLE_TAG.Equals(intent?.Action))
            {
                WidgetServiceHandler.SetShuffleButton();
                MainActivity.ServiceConnection.Binder?.Service.Shuffle(!MainActivity.ServiceConnection.Binder?.Service.QueueObject.IsShuffled ?? false);
            }
            else if (WIDGET_REPEAT_TAG.Equals(intent?.Action))
            {
                switch (MainActivity.ServiceConnection.Binder?.Service.QueueObject.LoopState ?? LoopState.None)
                {
                    case LoopState.None:
                        WidgetServiceHandler.SetRepeatButton(LoopState.All);
                        MainActivity.ServiceConnection.Binder?.Service.ToggleLoop(LoopState.All);
                        break;
                    case LoopState.All:
                        WidgetServiceHandler.SetRepeatButton(LoopState.Single);
                        MainActivity.ServiceConnection.Binder?.Service.ToggleLoop(LoopState.Single);
                        break;
                    case LoopState.Single:
                        WidgetServiceHandler.SetRepeatButton(LoopState.None);
                        MainActivity.ServiceConnection.Binder?.Service.ToggleLoop(LoopState.None);
                        break;
                }
            }


        }

      
        
    }
}