using System;
using System.Runtime.CompilerServices;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Navigation;
using Google.Android.Material.Snackbar;
using Android.Webkit;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Xamarin.Essentials;
using Android.Widget;
using Android.Graphics;
using AndroidX.Core.App;
using Ass_Pain.Helpers;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Chip;

namespace Ass_Pain
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class youtube : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
       

        DrawerLayout drawer;
        WebView web_view;

        private static FloatingActionButton previous;
        private static FloatingActionButton next;
        private static TextView accept;
        private static TextView reject;

        private static BottomSheetDialog bottomDialog;
        private static ImageView songImage;
        private static TextView songName;
        private static TextView songArtist;
        private static TextView songAlbum;

        SensorSpeed speed = SensorSpeed.Game;
        
        Android.Widget.Button download;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_youtube);
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

        



            // side bar where are song controlls
            side_player.populate_side_bar(this);
            MainActivity.stateHandler.SetView(this);
           

            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            web_view = FindViewById<WebView>(Resource.Id.webview);
            web_view.Settings.JavaScriptEnabled = true;
            web_view.SetWebViewClient(new HelloWebViewClient());
            web_view.LoadUrl("https://www.youtube.com/");
            
            try
            {
                if (Accelerometer.IsMonitoring)
                    Accelerometer.Stop();
                else
                    Accelerometer.Start(speed);
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Feature not supported on device
                Toast error = new Toast(this);
                error.SetText("Feature not supported on device");
                error.Show();
            }
            catch (Exception ex)
            {
                // Other error has occurred.
                Toast error = new Toast(this);
                error.SetText("Other error has occurred");
                error.Show();
            }

           

            Accelerometer.ShakeDetected += acc_shaked;


            FloatingActionButton download_popup_show = FindViewById<FloatingActionButton>(Resource.Id.fab);
            Button mp4 = FindViewById<Button>(Resource.Id.mp4_btn);
            Button music_brainz = FindViewById<Button>(Resource.Id.music_brainz_btn);
            Button download_casual = FindViewById<Button>(Resource.Id.download_basic_btn);

            download_casual.Visibility = ViewStates.Invisible;
            mp4.Visibility = ViewStates.Invisible;
            music_brainz.Visibility = ViewStates.Invisible;
            
            mp4.Background.SetColorFilter(Color.Rgb(255, 76, 41), PorterDuff.Mode.Multiply);
            music_brainz.Background.SetColorFilter(Color.Rgb(255, 76, 41), PorterDuff.Mode.Multiply);
            download_casual.Background.SetColorFilter(Color.Rgb(255, 76, 41), PorterDuff.Mode.Multiply);
            
            bool visi_state = false;
            download_popup_show.Background.SetColorFilter(Color.Rgb(255, 76, 41), PorterDuff.Mode.Multiply);
            download_popup_show.Click += delegate(object sender, EventArgs args)
            {
                if (visi_state)
                {
                    download_casual.Visibility = ViewStates.Invisible;
                    mp4.Visibility = ViewStates.Invisible;
                    music_brainz.Visibility = ViewStates.Invisible;
                    visi_state = false;
                }
                else
                    Downloader.Download(sender, args, web_view.Url, DownloadActions.DownloadOnly);
            };

            download_popup_show.LongClick += delegate(object sender, View.LongClickEventArgs args)
            {
                if (!visi_state)
                {
                    download_casual.Visibility = ViewStates.Visible;
                    mp4.Visibility = ViewStates.Visible;
                    music_brainz.Visibility = ViewStates.Visible;
                    visi_state = true;
                }
                else
                {
                    download_casual.Visibility = ViewStates.Invisible;
                    mp4.Visibility = ViewStates.Invisible;
                    music_brainz.Visibility = ViewStates.Invisible;
                }

            };

            download_casual.Click += delegate(object sender, EventArgs args) { Downloader.Download(sender, args, web_view.Url, DownloadActions.DownloadOnly); };
            mp4.Click += delegate(object sender, EventArgs args) { Downloader.Download(sender, args, web_view.Url, DownloadActions.Downloadmp4); };
            music_brainz.Click += delegate(object sender, EventArgs args) { Downloader.Download(sender, args, web_view.Url, DownloadActions.DownloadWithMbid); };

            SongSelectionDialog("dd", "dd", "dd", "dd");

        }

        /// <summary>
        /// pop up for customizing downloaded song, choosing which image or name of song you want to use to save
        /// the song
        /// </summary>
        /// <param name="songNameIn"></param>
        /// <param name="songArtistIn"></param>
        /// <param name="songAlbumIn"></param>
        /// <param name="imgUrl"></param>
        public static void SongSelectionDialog(string songNameIn, string songArtistIn, string songAlbumIn, string imgUrl)
        {
            int retVal = 16;
            
            bottomDialog = new BottomSheetDialog(MainActivity.stateHandler.view);
            LayoutInflater ifl = LayoutInflater.From(MainActivity.stateHandler.view);
            View view = ifl?.Inflate(Resource.Layout.song_download_selection_dialog, null);
            if (view != null) bottomDialog.SetContentView(view);

            previous = bottomDialog?.FindViewById<FloatingActionButton>(Resource.Id.previous_download);
            previous?.SetBackgroundColor(Android.Graphics.Color.ParseColor("#ff4d29"));
            next = bottomDialog?.FindViewById<FloatingActionButton>(Resource.Id.next_download);
            next?.SetBackgroundColor(Android.Graphics.Color.ParseColor("#ff4d29"));

            accept = bottomDialog?.FindViewById<TextView>(Resource.Id.accept_download);
            reject = bottomDialog?.FindViewById<TextView>(Resource.Id.reject_download);

            songImage = bottomDialog?.FindViewById<ImageView>(Resource.Id.to_download_song_image);
            songName = bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_name);
            songArtist = bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_artist);
            songAlbum = bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_album);1
            
            if (songArtist != null) songArtist.Text = songArtistIn;
            if (songAlbum != null) songAlbum.Text = songAlbumIn;
            if (songName != null) songName.Text = songNameIn;

            previous.Click += delegate(object sender, EventArgs args) {  };

            if (bottomDialog != null) bottomDialog.Show();


        }

        void acc_shaked(object sender, EventArgs e)
        {
            
            Toast error = new Toast(this);
            error.SetText("shaked");
            error.Show();

            int down_vis = 0;
            if (down_vis == 0)
            {
                down_vis = 1;
                download.Visibility = ViewStates.Visible;
            }
            else
            {
                down_vis = 0;
                download.Visibility = ViewStates.Invisible;
            }

        }

        public override void OnBackPressed()
        {
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else if (web_view.CanGoBack())
            {
                web_view.GoBack();
            }
            else
            {
                base.OnBackPressed();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View)sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            int id = item.ItemId;

            if (id == Resource.Id.nav_camera) // home
            {
                Intent intent = new Intent(this, typeof(AllSongs));
                intent.PutExtra("link_author", "");
                StartActivity(intent);
            }
            else if (id == Resource.Id.nav_gallery) // equalizer
            {
                Intent intent = new Intent(this, typeof(equalizer));
                intent.PutExtra("link_author", "");
                StartActivity(intent);
            }
            else if (id == Resource.Id.nav_slideshow) // youtube
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else if (id == Resource.Id.nav_share) // share
            {
                Intent intent = new Intent(this, typeof(share));
                StartActivity(intent);
            }


            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}