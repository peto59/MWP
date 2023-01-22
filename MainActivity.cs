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
using System.Text.Json;
using AngleSharp.Html;
using System.Runtime.InteropServices;
using Android.Content.PM;
using Android.Media;
using AndroidX.Core.App;

namespace Ass_Pain
{


    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        public static Slovenska_prostituka player = new Slovenska_prostituka();
        //public static NetworkManager nm = new NetworkManager();

        DrawerLayout drawer;

        public static readonly string CHANNEL_ID = "location_notification";

        private NotificationManagerCompat notification_manager;
        public void push_notification()
        {
            Console.WriteLine("notificated");

            var notification = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetSmallIcon(Resource.Drawable.ic_menu_manage)
                .SetContentTitle("title")
                .SetContentText("yeah this is something");

            notification_manager.Notify(100, notification.Build());

        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            string[] PermissionsLocation =
            {
                Android.Manifest.Permission.ManageExternalStorage,
                Android.Manifest.Permission.WriteExternalStorage,
                Android.Manifest.Permission.ReadExternalStorage
            };


            const int RequestLocationId = 1;
            //string[] permission = { Android.Manifest.Permission.ManageExternalStorage };
            //RequestPermissions(permission, 0);
            if (ShouldShowRequestPermissionRationale(Android.Manifest.Permission.ManageExternalStorage))
            {
                //Explain to the user why we need to read the contacts
                Snackbar.Make(FindViewById<DrawerLayout>(Resource.Id.drawer_layout), "Storage access is required for storing and playing songs", Snackbar.LengthIndefinite)
                        .SetAction("OK", v => RequestPermissions(PermissionsLocation, RequestLocationId))
                        .Show();
                return;
            }
            //Finally request permissions with the list of permissions and Id
            RequestPermissions(PermissionsLocation, RequestLocationId);

            try
            {
                while (!Android.OS.Environment.IsExternalStorageManager)
                {
                    Intent intent = new Intent();
                    intent.SetAction(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                    Android.Net.Uri uri = Android.Net.Uri.FromParts("package", this.PackageName, null);
                    intent.SetData(uri);
                    StartActivity(intent);
                }
            }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                try
                {
                    Intent intent = new Intent();
                    intent.SetAction(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                    Android.Net.Uri uri = Android.Net.Uri.FromParts("package", this.PackageName, null);
                    intent.SetData(uri);
                    StartActivity(intent);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;



            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.OpenDrawer(GravityCompat.Start);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();


            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);


            side_player.populate_side_bar(this);


            string privatePath = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;

            if (!Directory.Exists(path))
            {
                Console.WriteLine("Creating " + $"{path}");
                Directory.CreateDirectory(path);
            }

            if (!Directory.Exists($"{privatePath}/tmp"))
            {
                Console.WriteLine("Creating " + $"{privatePath}/tmp");
                Directory.CreateDirectory($"{privatePath}/tmp");
            }

            if (!File.Exists($"{privatePath}/trusted_hosts.json"))
            {
                File.WriteAllText($"{privatePath}/trusted_hosts.json", JsonConvert.SerializeObject(new List<string>()));
            }

            if (!File.Exists($"{privatePath}/sync_targets.json"))
            {
                File.WriteAllText($"{privatePath}/sync_targets.json", JsonConvert.SerializeObject(new Dictionary<string, List<string>>()));
            }

            if (!File.Exists($"{path}/aliases.json"))
            {
                File.WriteAllTextAsync($"{path}/aliases.json", JsonConvert.SerializeObject(new Dictionary<string, string>()));

            }

            if (!File.Exists($"{path}/playlists.json"))
            {
                File.WriteAllTextAsync($"{path}/playlists.json", JsonConvert.SerializeObject(new Dictionary<string, List<string>>()));
            }


            //new Thread(() => { nm.Listener(); }).Start();
            //new Thread(() => { FileManager.DiscoverFiles(); }).Start();
            player.SetView(this);
            IntentFilter intentFilter = new IntentFilter(AudioManager.ActionAudioBecomingNoisy);
            RegisterReceiver(player, intentFilter);


            // notififcations
            notification_manager = NotificationManagerCompat.From(this);
            push_notification();
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
                Intent intent = new Intent(this, typeof(youtube));
                intent.PutExtra("link_author", "");
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

