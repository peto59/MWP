/*
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
        private static TextView _originalArtist;
        private static TextView _originalTitle;

        private static ProgressBar _SSImageLoading;

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


            FloatingActionButton downloadPopupShow = FindViewById<FloatingActionButton>(Resource.Id.fab);
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
            if (BlendMode.Multiply != null)
                downloadPopupShow?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
            if (downloadPopupShow != null)
            {
                downloadPopupShow.Click += delegate(object sender, EventArgs args)
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

                downloadPopupShow.LongClick += delegate(object sender, View.LongClickEventArgs args)
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
            }

            if (downloadCasual != null) downloadCasual.Click += delegate(object sender, EventArgs args)
            {
                Downloader.Download(sender, args, webView.Url, DownloadActions.DownloadOnly);
            };
            if (mp4 != null) mp4.Click += delegate(object sender, EventArgs args)
            {
                Downloader.Download(sender, args, webView.Url, DownloadActions.Downloadmp4);
            };
            if (musicBrainz != null) musicBrainz.Click += delegate(object sender, EventArgs args)
            {
                Downloader.Download(sender, args, webView.Url, DownloadActions.DownloadWithMbid);
            };

        }


        /// <summary>
        /// Update already existing popup, it won't close the instance
        /// </summary>
        /// <param name="songNameIn"></param>
        /// <param name="songArtistIn"></param>
        /// <param name="songAlbumIn"></param>
        /// <param name="imgArr"></param>
        /// <param name="originalAuthor"></param>
        /// <param name="originalTitle"></param>
        public static void UpdateSsDialog(string songNameIn, string songArtistIn, string songAlbumIn, byte[] imgArr, string originalAuthor, string originalTitle,  bool forw, bool back)
        {
            if (_bottomDialog != null && _bottomDialog.IsShowing)
            {
                TextView new_label = _bottomDialog.FindViewById<TextView>(Resource.Id.SS_new_label);
                new_label.Visibility = ViewStates.Visible;
                
                if (!forw) { if (_next != null) _next.Visibility = ViewStates.Gone; }
                else { if (_next != null) _next.Visibility = ViewStates.Visible; }
            
                if (!back) { if (_previous != null) _previous.Visibility = ViewStates.Gone; }
                else { if (_previous != null) _previous.Visibility = ViewStates.Visible; }
                
                _SSImageLoading.Visibility = ViewStates.Gone;
                _songImage.Visibility = ViewStates.Visible;
                using Bitmap img = BitmapFactory.DecodeByteArray(imgArr, 0, imgArr.Length);
                _songImage.SetImageBitmap(img);
                if (_songArtist != null) _songArtist.Text = songArtistIn;
                if (_songAlbum != null) _songAlbum.Text = songAlbumIn;
                if (_songName != null) _songName.Text = songNameIn;
                if (_originalTitle != null) _originalTitle.Text = originalTitle;
                if (_originalArtist != null) _originalArtist.Text = originalAuthor;
                
                
            }
            else
            {
                SongSelectionDialog(songNameIn, songArtistIn, songAlbumIn, imgArr, originalAuthor, originalTitle, forw,
                    back);
            }
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
        public static void SongSelectionDialog(string songNameIn, string songArtistIn, string songAlbumIn, byte[] imgArr, string originalAuthor, string originalTitle, bool forw, bool back)
        {
    
            _bottomDialog = new BottomSheetDialog(MainActivity.stateHandler.view);
            _bottomDialog.SetCanceledOnTouchOutside(false);
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

            LinearLayout.LayoutParams SSFLoatingButtons = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            if (forw && back)
            {
                SSFLoatingButtons.SetMargins(1, 1, 1, 1);
                if (_next != null) _next.LayoutParameters = SSFLoatingButtons;
                if (_previous != null) _previous.LayoutParameters = SSFLoatingButtons;
            }
            else
            {
                SSFLoatingButtons.SetMargins(20, 20, 20, 20);
                if (_next != null) _next.LayoutParameters = SSFLoatingButtons;
                if (_previous != null) _previous.LayoutParameters = SSFLoatingButtons;
                
                if (!forw) { if (_next != null) _next.Visibility = ViewStates.Gone; }
                else { if (_next != null) _next.Visibility = ViewStates.Visible; }
                
                if (!back) { if (_previous != null) _previous.Visibility = ViewStates.Gone; }
                else { if (_previous != null) _previous.Visibility = ViewStates.Visible; }
            }

            

            
            _accept = _bottomDialog?.FindViewById<TextView>(Resource.Id.accept_download);
            _reject = _bottomDialog?.FindViewById<TextView>(Resource.Id.reject_download);

            _songImage = _bottomDialog?.FindViewById<ImageView>(Resource.Id.to_download_song_image);
            _songName = _bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_name);
            _songArtist = _bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_artist);
            _songAlbum = _bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_album);
            _originalArtist = _bottomDialog?.FindViewById<TextView>(Resource.Id.SS_original_title);
            _originalTitle = _bottomDialog?.FindViewById<TextView>(Resource.Id.SS_orignal_artist);

            _SSImageLoading = _bottomDialog?.FindViewById<ProgressBar>(Resource.Id.SS_image_loading);
            LinearLayout.LayoutParams SSLoadingImageParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent
            );
            SSLoadingImageParams.SetMargins(50, 50, 50, 50);
            
            if (_songArtist != null) _songArtist.Text = songArtistIn;
            if (_songAlbum != null) _songAlbum.Text = songAlbumIn;
            if (_songName != null) _songName.Text = songNameIn;
            if (_originalTitle != null) _originalTitle.Text = originalTitle;
            if (_originalArtist != null) _originalArtist.Text = originalAuthor;

            _previous.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Previous;
                MainActivity.stateHandler.ResultEvent.Set();
            };
            _next.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Next;
                MainActivity.stateHandler.ResultEvent.Set();
                _songImage.SetImageResource(Resource.Color.mtrl_btn_transparent_bg_color);
                _songImage.Visibility = ViewStates.Gone;
                _SSImageLoading.Visibility = ViewStates.Visible;
                _SSImageLoading.LayoutParameters = SSLoadingImageParams;
                
                _songArtist.Text = "";
                _songAlbum.Text = "";
                _songName.Text = "";
                _originalTitle.Text = "";
                _originalArtist.Text = "";
                
                _next.Visibility = ViewStates.Gone;
                _previous.Visibility = ViewStates.Gone;

                TextView new_label = _bottomDialog.FindViewById<TextView>(Resource.Id.SS_new_label);
                new_label.Visibility = ViewStates.Invisible;
            };
            
            _accept.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Accept;
                MainActivity.stateHandler.ResultEvent.Set();
                _bottomDialog?.Hide();
            };
            _reject.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Cancel;
                MainActivity.stateHandler.ResultEvent.Set();
                _bottomDialog?.Hide();
            };
            
            //Picasso.Get().Load(imgUrl).Placeholder(Resource.Drawable.no_repeat).Error(Resource.Drawable.shuffle2).Into(_songImage);
            using Bitmap img = BitmapFactory.DecodeByteArray(imgArr, 0, imgArr.Length);
            _songImage.SetImageBitmap(img);
            //Glide.With(MainActivity.stateHandler.view).Load(imgUrl).Error(Resource.Drawable.shuffle2).Into(_songImage);

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
*/