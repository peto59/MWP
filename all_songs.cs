using System;
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
using Android.Graphics;
using Android.Widget;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Android.Service.Autofill;
using Android.Icu.Number;
using Org.Apache.Http.Conn;
// using Com.Arthenica.Ffmpegkit;
using Android.Drm;
using AngleSharp.Html.Dom;
using Newtonsoft.Json;
using System.Threading;
using Org.Apache.Http.Authentication;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Android.Graphics.Drawables;
using Android.Graphics.Text;
using Android.Util;
using Java.Lang;
using Color = Android.Graphics.Color;
using Debug = System.Diagnostics.Debug;
using Exception = System.Exception;
using Math = System.Math;
using Object = System.Object;
#if DEBUG
using Ass_Pain.Helpers;
#endif


namespace Ass_Pain
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AllSongs : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        DrawerLayout drawer;
        
        (bool is_auth, string auth) inAuthor = (false, "");
        
    
        
        /*
         * Fragments init
         */
        private SongsFragment songsFragment;
        private SongsFragment PlaylistsFragment;
        private AlbumAuthorFragment AlbumsFragment;
        private AlbumFragment albumFragment;
        private AuthorFragment authorFragment;
        private PlaylistsFragment playlistsFragment;
        private PlaylistFragment playlistFragment;
        
        
        /// <summary>
        /// 
        /// </summary>
        public enum FragmentType
        {
            AlbumFrag,
            AuthorFrag,
            PlaylistFrag
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_all_songs);

            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            // get intent sent from side bar to navigate author
            /*
            if (Intent != null)
            {
                int intentAuthor = Intent.GetIntExtra("link_author", 0);
                if (intentAuthor != 0)
                    populate_grid(0.2f, MainActivity.stateHandler.Artists.First(a => a.GetHashCode() == intentAuthor));
                string action = Intent.GetStringExtra("action");
                if (action == "openDrawer")
                    drawer.OpenDrawer(GravityCompat.Start);
            } */

            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(
                this,
                drawer, 
                toolbar, 
                Resource.String.navigation_drawer_open,
                Resource.String.navigation_drawer_close
            );

            drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            side_player.populate_side_bar(this);
            MainActivity.stateHandler.SetView(this);

            

            // -=-=-=-=-=
            Button author = FindViewById<Button>(Resource.Id.author);
            Button allSongs = FindViewById<Button>(Resource.Id.all_songs);
            Button playlists = FindViewById<Button>(Resource.Id.playlists);
            if (BlendMode.Multiply != null)
            {
                author?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
                allSongs?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
                playlists?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
                
            }
            
            /*
             * Fragment defining
             */
            songsFragment = new SongsFragment(this);
            AlbumsFragment = new AlbumAuthorFragment(this);
            albumFragment = new AlbumFragment(this);
            authorFragment = new AuthorFragment(this);
            playlistsFragment = new PlaylistsFragment(this);
            playlistFragment = new PlaylistFragment(this);
            
            /*
             * Button bar
             */
            int activeFragment = 0;
            if (author != null)
                author.Click += (sender, e) =>
                {
                    var fragmentTransaction = SupportFragmentManager.BeginTransaction();
                    if (activeFragment == 0)
                    {
                        fragmentTransaction.Add(Resource.Id.FragmentLayoutDynamic, AlbumsFragment);
                        activeFragment = 1;
                    }
                    else
                        fragmentTransaction.Replace(Resource.Id.FragmentLayoutDynamic, AlbumsFragment);
                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                };
            if (allSongs != null)
                allSongs.Click += (sender, e) =>
                {
                    var fragmentTransaction = SupportFragmentManager.BeginTransaction();
                    if (activeFragment == 0)
                    {
                        fragmentTransaction.Add(Resource.Id.FragmentLayoutDynamic, songsFragment);
                        activeFragment = 1;
                    }
                    else
                        fragmentTransaction.Replace(Resource.Id.FragmentLayoutDynamic, songsFragment);
                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                };
            if (playlists != null)
                playlists.Click += (sender, e) =>
                {
                    var fragmentTransaction = SupportFragmentManager.BeginTransaction();
                    if (activeFragment == 0)
                    {
                        fragmentTransaction.Add(Resource.Id.FragmentLayoutDynamic, playlistsFragment);
                        activeFragment = 1;
                    }
                    else
                        fragmentTransaction.Replace(Resource.Id.FragmentLayoutDynamic, playlistsFragment);
                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                };

            FloatingActionButton createPlaylist = FindViewById<FloatingActionButton>(Resource.Id.fab);
            if (BlendMode.Multiply != null)
                createPlaylist?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );

            if (createPlaylist != null) createPlaylist.Click += CreatePlaylistPopup;
        }

        /// <summary>
        /// Function for replacing fragment from inside another fragment by calling ((AllSongs)this.Activity).ReplaceFragments()
        /// </summary>
        /// <param name="type">Type of fragment, by which current should be replaced</param>
        /// <param name="title">title of either an album or artist</param>
        public void ReplaceFragments(FragmentType type, string title)
        {
            var fragmentTransaction = SupportFragmentManager.BeginTransaction();
            Bundle bundle = new Bundle();
            bundle.PutString("title", title);
            
            switch (type)
            {
                case FragmentType.AlbumFrag:
                    albumFragment.Arguments = bundle;
                    fragmentTransaction.Replace(Resource.Id.FragmentLayoutDynamic, albumFragment);
                    break;
                case FragmentType.AuthorFrag:
                    authorFragment.Arguments = bundle;
                    fragmentTransaction.Replace(Resource.Id.FragmentLayoutDynamic, authorFragment);
                    break;
                case FragmentType.PlaylistFrag:
                    Console.WriteLine("FRAGMENT RPLACEFRAGMENTS FUNCTOIONNNNNNN " + title);
                    playlistFragment.Arguments = bundle;
                    fragmentTransaction.Replace(Resource.Id.FragmentLayoutDynamic, playlistFragment);
                    break;
            }
            
            fragmentTransaction.AddToBackStack(null);
            fragmentTransaction.Commit();
            
        }
        
        
        

        private void CreatePlaylistPopup(object sender, EventArgs e)
        {
            var font = Typeface.CreateFromAsset(Assets, "WixMadeforText.ttf");

            LayoutInflater ifl = LayoutInflater.From(this);
            View view = ifl?.Inflate(Resource.Layout.new_playlist_popup, null);
            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetView(view);

            TextView dialogTitle = view?.FindViewById<TextView>(Resource.Id.AddPlaylist_title);
            if (dialogTitle != null) dialogTitle.Typeface = font;

            EditText userData = view?.FindViewById<EditText>(Resource.Id.editText);
            if (userData != null)
            {
                userData.Typeface = font;
                alert.SetCancelable(false);


                TextView pButton = view?.FindViewById<TextView>(Resource.Id.AddPlaylist_submit);
                if (pButton != null) pButton.Typeface = font;
                if (pButton != null)
                    pButton.Click += (_, _) =>
                    {
                        FileManager.CreatePlaylist(userData.Text);
                        Toast.MakeText(
                                this, userData.Text + " Created successfully",
                                ToastLength.Short
                            )
                            ?.Show();
                        alert.Dispose();
                    };
            }

            TextView nButton = view?.FindViewById<TextView>(Resource.Id.AddPlaylist_cancel);
            if (nButton != null) nButton.Typeface = font;
         

            Android.App.AlertDialog dialog = alert.Create();
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
            if (nButton != null) nButton.Click += (_, _) => dialog?.Cancel();
            
            
            dialog?.Show();
            

        }


        private void add_alias_popup(string authorN)
        {
#if DEBUG
            MyConsole.WriteLine("popup clicked");
#endif

            LayoutInflater ifl = LayoutInflater.From(this);
            View view = ifl?.Inflate(Resource.Layout.add_alias_popup, null);
            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetView(view);

            Android.App.AlertDialog dialog = alert.Create();

            if (view != null)
            {
                TextView author = view.FindViewById<TextView>(Resource.Id.author_name);
                if (author != null) author.Text = authorN;
            }

            EditText userInput = view.FindViewById<EditText>(Resource.Id.user_author);
            Button sub = view.FindViewById<Button>(Resource.Id.submit_alias);
            if (sub != null)
                sub.Click += delegate
                {
                    if (userInput != null) FileManager.AddAlias(authorN, userInput.Text);
                    dialog?.Hide();
                };

            Button cancel = view.FindViewById<Button>(Resource.Id.cancel_alias);
            if (cancel != null)
                cancel.Click += delegate { dialog?.Hide(); };


            dialog?.Show();
        }

        
        [Obsolete("deprecated")]
        public override void OnBackPressed()
        {
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
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
                if (inAuthor.is_auth)
                    add_alias_popup(FileManager.GetNameFromPath(inAuthor.auth));

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
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else if (id == Resource.Id.nav_gallery) // equalizer
            {
                Intent intent = new Intent(this, typeof(equalizer));
                intent.PutExtra("link_author", "");
                StartActivity(intent);
            }
            else if (id == Resource.Id.nav_slideshow) // youtube
            {
                Intent intent = new Intent(this, typeof(youtube));
                intent.PutExtra("link_author", "");
                StartActivity(intent);
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