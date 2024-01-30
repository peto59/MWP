using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Java.Util;
using MWP.BackEnd;
using Fragment = AndroidX.Fragment.App.Fragment;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;
using Orientation = Android.Widget.Orientation;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP
{
    /// <inheritdoc />
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class SongPickerFragment : Fragment
    {
        private readonly Context context;
        private readonly float scale;
        private readonly Typeface? font;
        private readonly AssetManager assets;
        private RelativeLayout? mainLayout;
        private TextView? songPickerButton;

        private Dictionary<string, LinearLayout?> lazyBuffer;
        private ObservableDictionary<string, Bitmap>? lazyImageBuffer;

        private Dictionary<string, Song> selectedSongs;

        /// <inheritdoc />
        public SongPickerFragment(Context context, AssetManager assets)
        {
            this.assets = assets;
            this.context = context;
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
            if (context.Resources is { DisplayMetrics: not null }) scale = context.Resources.DisplayMetrics.Density;
            
            lazyImageBuffer = new ObservableDictionary<string, Bitmap>();
            selectedSongs = new Dictionary<string, Song>();
        }
        
        /// <inheritdoc />
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.song_picker_fragment, container, false);
            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.song_picker_layout_main);

            /* Získanie komoponentov z XML súboru a nastavenie fontov*/
            TextView? songPickerTitle = view?.FindViewById<TextView>(Resource.Id.song_picker_title);
            songPickerButton = view?.FindViewById<TextView>(Resource.Id.song_picker_share_button);
            if (songPickerButton != null) songPickerButton.Typeface = font;
            if (songPickerTitle != null) songPickerTitle.Typeface = font;

            /* získanie listu pre vykreslenie jednotlivých skladieb */
            LinearLayout? songList = view?.FindViewById<LinearLayout>(Resource.Id.song_picker_song_list);


            if (songPickerButton != null)
                songPickerButton.Click += delegate
                {
                    foreach (var sel in selectedSongs)
                    {
#if DEBUG
                        MyConsole.WriteLine($"selected name: {sel.Key}");
#endif
                    }
                };


            /*
             * Každá skladba ktorá je načítaná vo vlákne mimo hlavného vlákna UI je pridaná do ObservableDictionary,
             * čo spôsobi zavolanie tejto metódy v prípade pridania nového elementu, ktorá načíta posledný načítaný obrázok
             * do užívateľského prostredia k prislúchajúcemu políčku v liste.
             */
            if (lazyImageBuffer != null)
                lazyImageBuffer.ValueChanged += (_, _) =>
                {
                    ((Activity)context).RunOnUiThread(() => {
                        string last = lazyImageBuffer.Items.Keys.Last();

                        LinearLayout? child = lazyBuffer?[last] ?? new LinearLayout(context);
                        UIRenderFunctions.LoadSongImageFromBuffer(child, lazyImageBuffer, assets);
                    });
                };
            
            
            /*
             * Načítavanie jednotlivých políčok pre každú skladbu na základe listu získaného
             * z pozadia aplikácie.
             */
            List<Song> songs = MainActivity.StateHandler.Songs;
            lazyBuffer = new Dictionary<string, LinearLayout?>();
            
            foreach (var song in songs)
            {
                LinearLayout? lnIn = CreateSongTile(song);
                if (lazyBuffer.TryAdd(song.Title, lnIn))
                {
                    songList?.AddView(lnIn);
                }
            }
            
            /*
             * Vytvorenie nového vlákna a začatie načítavania obrázkov pre skladby na poazdí aplikácie.
             */
            Task.Run(async () =>
            {
                MainActivity.StateHandler.FileListGenerated.WaitOne();
                await UIRenderFunctions.LoadSongImages(MainActivity.StateHandler.Songs, lazyImageBuffer, UIRenderFunctions.LoadImageType.SONG);
                UIRenderFunctions.FillImageHoles(context, lazyBuffer, lazyImageBuffer, assets);
            });
            
            return view; 
        }
        
        /*
         * Metóda slúžiaca na vygenerovanie LinearLayout komponentu s obrázkom a názvom sklaby pre vykreslenie jednotlivých
         * skladieb do listu skladieb. Príjma názov sklaby a vracia samotný LinearLayout už so všetkými potrebnými nastaveniami a komponentmi. 
         */
        private LinearLayout? CreateSongTile(Song song)
        {
            /*
             * Vytváranie Linearlayout pre tvorbu jedného riadka v liste skladieb.
             * LinearLayout s horizontálnou orientáciou bude obsahovať ImageView pre obrázok a TextView pre názov skladby
             */
            LinearLayout? lnIn = new LinearLayout(context);
            lnIn.Orientation = Orientation.Horizontal;
            lnIn.SetBackgroundResource(Resource.Drawable.rounded_primaryColor);

            LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            lnInParams.SetMargins(50, 50, 50, 50);
            lnIn.LayoutParameters = lnInParams;

            bool selected = false;
            lnIn.Click += delegate
            {
                if (!selected)
                {
                    lnIn.SetBackgroundResource(Resource.Drawable.rounded_button);
                    selected = true;
                    selectedSongs.Add(song.Title, song);
                }
                else
                {
                    lnIn.SetBackgroundResource(Resource.Drawable.rounded_primaryColor);
                    selected = false;
                    selectedSongs.Remove(song.Title);
                    
                    if (songPickerButton != null) songPickerButton.Visibility = ViewStates.Visible;
                }
                
            };
            
            /*
             * Vytváranie ImageView elementu pre orbázok skaldby, zatiaľ je prázdny.
             * Obrázok sa nahráva v rámci Lazy Loadingu
             */
            ImageView songThumbnail = new ImageView(context);
            LinearLayout.LayoutParams songThumbnailParams = new LinearLayout.LayoutParams(
                (int)(150 * scale + 0.5f), (int)(100 * scale + 0.5f)
            );
            songThumbnailParams.SetMargins(50, 50, 50, 50);
            songThumbnail.LayoutParameters = songThumbnailParams;
            
            lnIn.AddView(songThumbnail);
            
            /*
             * TextView komponent pre názov sklaby. TextView má nastavenú nulovú šírku z dôvodu nastavenia
             * Weight layout parametru (váhy) pre automatické uprispôsobenie šírky vzhľadom k obrázku.
             */
            TextView name = new TextView(context);
            name.Text = song.Title;
            name.TextSize = 15;
            name.Typeface = font;
            name.SetTextColor(Color.White);
            name.TextAlignment = TextAlignment.Center;
            name.SetForegroundGravity(GravityFlags.Center);

            LinearLayout.LayoutParams lnNameParams = new LinearLayout.LayoutParams(
                0,
                ViewGroup.LayoutParams.WrapContent
            );
            lnNameParams.Weight = 1;
            lnNameParams.SetMargins(50, 50, 50, 50);

            name.LayoutParameters = lnNameParams;

            lnIn.SetGravity(GravityFlags.Center);
            lnIn.SetHorizontalGravity(GravityFlags.Center);
            lnIn.AddView(name);
            
            return lnIn;
        }


        private void EnableShareOnChange()
        {
#if DEBUG
            MyConsole.WriteLine("sakk alele jooj");
#endif
            if (selectedSongs.Count == 0)
                if (songPickerButton != null) songPickerButton.Visibility = ViewStates.Gone;
            else
                if (songPickerButton != null) songPickerButton.Visibility = ViewStates.Visible;
            
        }

     
        
 

    }
}