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
using Android.Graphics.Drawables;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.Graphics;
#if DEBUG
using Ass_Pain.Helpers;
#endif
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Chip;

namespace Ass_Pain
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class youtube : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
       

        DrawerLayout drawer;
        WebView webView;

        private static FloatingActionButton _previous;
        private static FloatingActionButton _next;
        private static TextView _accept;
        private static TextView _reject;

        private static BottomSheetDialog _bottomDialog;
        private static ImageView _songImage;
        private static TextView _songName;
        private static TextView _songArtist;
        private static TextView _songAlbum;

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

            webView = FindViewById<WebView>(Resource.Id.webview);
            webView.Settings.JavaScriptEnabled = true;
            webView.SetWebViewClient(new HelloWebViewClient());
            webView.LoadUrl("https://www.youtube.com/");
            
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
            Button musicBrainz = FindViewById<Button>(Resource.Id.music_brainz_btn);
            Button downloadCasual = FindViewById<Button>(Resource.Id.download_basic_btn);

            if (downloadCasual != null) downloadCasual.Visibility = ViewStates.Invisible;
            if (mp4 != null) mp4.Visibility = ViewStates.Invisible;
            if (musicBrainz != null) musicBrainz.Visibility = ViewStates.Invisible;

            if (BlendMode.Multiply != null)
            {
                mp4?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
                musicBrainz?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
                downloadCasual?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
                
            }
            bool visiState = false;
            download_popup_show?.Background?.SetColorFilter(
                new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
            );
            download_popup_show.Click += delegate(object sender, EventArgs args)
            {
                if (visiState)
                {
                    if (downloadCasual != null) downloadCasual.Visibility = ViewStates.Invisible;
                    if (mp4 != null) mp4.Visibility = ViewStates.Invisible;
                    if (musicBrainz != null) musicBrainz.Visibility = ViewStates.Invisible;
                    visiState = false;
                }
                else
                    Downloader.Download(sender, args, webView.Url, DownloadActions.DownloadOnly);
            };

            download_popup_show.LongClick += delegate(object sender, View.LongClickEventArgs args)
            {
                if (!visiState)
                {
                    if (downloadCasual != null) downloadCasual.Visibility = ViewStates.Visible;
                    if (mp4 != null) mp4.Visibility = ViewStates.Visible;
                    if (musicBrainz != null) musicBrainz.Visibility = ViewStates.Visible;
                    visiState = true;
                }
                else
                {
                    if (downloadCasual != null) downloadCasual.Visibility = ViewStates.Invisible;
                    if (mp4 != null) mp4.Visibility = ViewStates.Invisible;
                    if (musicBrainz != null) musicBrainz.Visibility = ViewStates.Invisible;
                }

            };

            if (downloadCasual != null) downloadCasual.Click += delegate(object sender, EventArgs args) { Downloader.Download(sender, args, webView.Url, DownloadActions.DownloadOnly); };
            if (mp4 != null) mp4.Click += delegate(object sender, EventArgs args) { Downloader.Download(sender, args, webView.Url, DownloadActions.Downloadmp4); };
            if (musicBrainz != null) musicBrainz.Click += delegate(object sender, EventArgs args) { Downloader.Download(sender, args, webView.Url, DownloadActions.DownloadWithMbid); };

            SongSelectionDialog("dd", "dd", "dd", "dd", false, false);

        }
        
        /// <summary>
        /// pop up for customizing downloaded song, choosing which image or name of song you want to use to save
        /// the song
        /// </summary>
        /// <param name="songNameIn"></param>
        /// <param name="songArtistIn"></param>
        /// <param name="songAlbumIn"></param>
        /// <param name="imgUrl"></param>
        /// <param name="forw"></param>
        /// <param name="back"></param>
        public static void SongSelectionDialog(string songNameIn, string songArtistIn, string songAlbumIn, string imgUrl, bool forw, bool back)
        {

            _bottomDialog = new BottomSheetDialog(MainActivity.stateHandler.view);
            LayoutInflater ifl = LayoutInflater.From(MainActivity.stateHandler.view);
            View view = ifl?.Inflate(Resource.Layout.song_download_selection_dialog, null);
            if (view != null) _bottomDialog.SetContentView(view);
            
            if (BlendMode.Multiply != null)
            {
                _previous = _bottomDialog?.FindViewById<FloatingActionButton>(Resource.Id.previous_download);
                _previous?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
                _next = _bottomDialog?.FindViewById<FloatingActionButton>(Resource.Id.next_download);
                _next?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
            }

            if (!forw) { if (_next != null) _next.Visibility = ViewStates.Invisible; }
            else { if (_next != null) _next.Visibility = ViewStates.Visible; }
            
            if (!back) { if (_previous != null) _previous.Visibility = ViewStates.Invisible; }
            else { if (_previous != null) _previous.Visibility = ViewStates.Visible; }

            
            _accept = _bottomDialog?.FindViewById<TextView>(Resource.Id.accept_download);
            _reject = _bottomDialog?.FindViewById<TextView>(Resource.Id.reject_download);

            _songImage = _bottomDialog?.FindViewById<ImageView>(Resource.Id.to_download_song_image);
            _songName = _bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_name);
            _songArtist = _bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_artist);
            _songAlbum = _bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_album);
            
            if (_songArtist != null) _songArtist.Text = songArtistIn;
            if (_songAlbum != null) _songAlbum.Text = songAlbumIn;
            if (_songName != null) _songName.Text = songNameIn;

            _previous.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Previous;
                MainActivity.stateHandler.ResultEvent.Set();
            };
            _next.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Next;
                MainActivity.stateHandler.ResultEvent.Set();
            };
            
            _accept.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Accept;
                MainActivity.stateHandler.ResultEvent.Set();
            };
            _reject.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Cancel;
                MainActivity.stateHandler.ResultEvent.Set();
            };

            _bottomDialog?.Show();


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
            else if (webView.CanGoBack())
            {
                webView.GoBack();
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