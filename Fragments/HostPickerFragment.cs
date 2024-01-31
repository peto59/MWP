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
    public class HostPickerFragment : Fragment
    {
        private readonly Context context;
        private readonly float scale;
        private readonly Typeface? font;
        private readonly AssetManager assets;
        private RelativeLayout? mainLayout;
        
        private ImageView? confirmPickBg;
        private TextView? confirmPick;
        private TextView? cancelPick;
        private string selectedString;

        private SongPickerFragment songPickerFragment;

        /// <inheritdoc />
        public HostPickerFragment(Context context, AssetManager assets)
        {
            this.assets = assets;
            this.context = context;
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
            if (context.Resources is { DisplayMetrics: not null }) scale = context.Resources.DisplayMetrics.Density;
            selectedString = "";
        }
        
        /// <inheritdoc />
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.host_picker_fragment, container, false);
            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.host_picker_layout_main);

            /* Získanie komoponentov z XML súboru a nastavenie fontov*/
            confirmPickBg = view?.FindViewById<ImageView>(Resource.Id.host_picker_slide_bar_bg);
            confirmPick = view?.FindViewById<TextView>(Resource.Id.host_picker_continue);
            cancelPick = view?.FindViewById<TextView>(Resource.Id.host_picker_cancel);

            if (confirmPick != null) confirmPick.Typeface = font;
            if (cancelPick != null) cancelPick.Typeface = font;
            TextView? songPickerTitle = view?.FindViewById<TextView>(Resource.Id.host_picker_title);
            if (songPickerTitle != null) songPickerTitle.Typeface = font;

            /* získanie listu pre vykreslenie jednotlivých hostov */
            LinearLayout? hostList = view?.FindViewById<LinearLayout>(Resource.Id.host_picker_song_list);


            if (confirmPick != null)
                confirmPick.Click += delegate
                {
                    songPickerFragment = new SongPickerFragment(context, assets, selectedString);
                    var fragmentTransaction = ParentFragmentManager.BeginTransaction();
                    
                    fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, songPickerFragment);
                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit(); 
                };
            if (cancelPick != null)
                cancelPick.Click += delegate { ParentFragmentManager.PopBackStack(); };

            /*
             * Načítavanie jednotlivých políčok pre každú skladbu na základe listu získaného
             * z pozadia aplikácie. Pre každé políčko je vytvorený nový LinearLayout na ktorý následne
             * pridávam Click event v ktorom prechádzam každé políčko, a nastvaujem farbu pozadia iba na políčko na ktoré používateľ klikol,
             * zabezpečuje sa tak výber iba jedného políčka
             */
            List<string> hosts = new List<string>{"host1 meno #1", "host2 meno #2"};
            Dictionary<string, LinearLayout> tiles = new Dictionary<string, LinearLayout>(); 
            
            foreach (var host in hosts)
            {
                LinearLayout? lnIn = CreateSongTile(host);

                if (lnIn != null)
                {

                    tiles.Add(host, lnIn);
                    lnIn.Click += (sender, args) =>
                    {
                        
                        LinearLayout? currentLin = sender as LinearLayout;
                        foreach (var tile in tiles)
                        {
                            tile.Value.SetBackgroundResource(tile.Value == currentLin
                                ? Resource.Drawable.rounded_button
                                : Resource.Drawable.rounded_primaryColor);

                            if (tile.Value == currentLin) selectedString = tile.Key;
                        }
                        
                        if (confirmPickBg != null)
                            confirmPickBg.Visibility = selectedString == "" ? ViewStates.Gone : ViewStates.Visible;
                        if (confirmPick != null)
                            confirmPick.Visibility = selectedString == "" ? ViewStates.Gone : ViewStates.Visible;
                        if (cancelPick != null)
                            cancelPick.Visibility = selectedString == "" ? ViewStates.Gone : ViewStates.Visible;
                    };
                }
                hostList?.AddView(lnIn);
            } 
        
            
            return view; 
        }
        
       
        
        
        /*
         * Metóda slúžiaca na vygenerovanie LinearLayout komponentu s obrázkom a názvom sklaby pre vykreslenie jednotlivých
         * skladieb do listu skladieb. Príjma názov sklaby a vracia samotný LinearLayout už so všetkými potrebnými nastaveniami a komponentmi. 
         */
        private LinearLayout? CreateSongTile(string host)
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
            lnInParams.SetMargins(50, 0, 50, 50);
            lnIn.LayoutParameters = lnInParams;
            
            
            /*
             * TextView komponent pre názov Hosta.
             */
            TextView name = new TextView(context);
            name.Text = host;
            name.TextSize = 15;
            name.Typeface = font;
            name.SetTextColor(Color.White);
            name.TextAlignment = TextAlignment.Center;
            name.SetForegroundGravity(GravityFlags.Center);

            LinearLayout.LayoutParams lnNameParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
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


      
     
        
 

    }
}