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
using Android.Webkit;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using AndroidX.AppCompat.Graphics.Drawable;
using Android.Widget;
using System.Threading;
using Newtonsoft.Json;
using System.Text.Json;
using AngleSharp.Html;
using System.Runtime.InteropServices;

namespace Ass_Pain
{


    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        public static Slovenska_prostituka player = new Slovenska_prostituka();

        DrawerLayout drawer;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;



            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.OpenDrawer(GravityCompat.Start);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();


            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);


            side_player.populate_side_bar(this, player);

            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            if (!Directory.Exists($"{path}/music"))
            {
                Console.WriteLine("Creating " + $"{path}/music");
                Directory.CreateDirectory($"{path}/music");
            }

            if (!Directory.Exists($"{path}/tmp"))
            {
                Console.WriteLine("Creating " + $"{path}/tmp");
                Directory.CreateDirectory($"{path}/tmp");
            }

            if (!File.Exists($"{path}/aliases.json"))
            {
                var x = new Dictionary<string, string>
                {
                    { "IRyS Ch. hololive-EN", "IRyS - Topic" }
                };
                File.WriteAllTextAsync($"{path}/aliases.json", JsonConvert.SerializeObject(x));

            }

            if (!File.Exists($"{path}/playlists.json"))
            {
                File.WriteAllTextAsync($"{path}/playlists.json", JsonConvert.SerializeObject(new Dictionary<string, List<string>>()));
            }

            NetworkManager nm = new NetworkManager();
            new Thread(() => { nm.Listener(); }).Start();
        }

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
                Intent intent = new Intent(this, typeof(all_songs));

                StartActivity(intent);

            }
            else if (id == Resource.Id.nav_gallery) // equalizer
            {
                Intent intent = new Intent(this, typeof(equalizer));
              
                StartActivity(intent);
            }
            else if (id == Resource.Id.nav_slideshow) // youtube
            {
                Intent intent = new Intent(this, typeof(youtube));
               
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

