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
using System.Linq;
using Newtonsoft.Json;
using AndroidX.AppCompat.Graphics.Drawable;
using Android.Widget;
using System.Threading;
using System.Text.Json;
using AngleSharp.Html;
using System.Runtime.InteropServices;
using Android.Content.PM;
using Android.Media;
using Android.Provider;
using AndroidX.Core.App;
using AndroidX.Core.Content;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain
{
    
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, Description = "@string/app_description")]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        // public static Slovenska_prostituka player = new Slovenska_prostituka();
        public static NetworkManager nm = new NetworkManager();
        public static APIThrottler throttler = new APIThrottler();
        public static MyBroadcastReceiver receiver;
        public static StateHandler stateHandler = new StateHandler();
        public static readonly MediaServiceConnection ServiceConnection = new MediaServiceConnection();
        
        

        DrawerLayout drawer;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            RequestMyPermission();

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

            //rest of the stuff that was here is in AfterReceivingPermissions()

            // notififcations
            //Local_notification_service notif_service = new Local_notification_service();
            //notif_service.song_control_notification();
            //new Thread(() => { Thread.Sleep(1500); Downloader.SearchAPI(); }).Start();

            
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
                Intent intent = new Intent(this, typeof(youtube));
                intent.PutExtra("link_author", "");
                StartActivity(intent);
            }
            else if (id == Resource.Id.nav_share) // share
            {
                Intent intent = new Intent(this, typeof(share));
                intent.PutExtra("link_author", "");
                StartActivity(intent);
            }
            

            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }
        
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
#if DEBUG
            MyConsole.WriteLine("OnRequestPermissionsResult Result");
#endif
            
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            bool shouldRequestAgain = false;
            
            for(int i = 0; i < permissions.Length; i++)
            {
#if DEBUG
                MyConsole.WriteLine($"Permission {permissions[i]}: {grantResults[i]}");
#endif
                if (grantResults[i] == Permission.Granted) continue;
                shouldRequestAgain = true;
                break;
            }

            if (!Android.OS.Environment.IsExternalStorageManager || shouldRequestAgain)
            {
                RequestMyPermission();
            }
            else
            {
                AfterReceivingPermissions();
            }
        }

        private async void RequestMyPermission()
        {
            string[] permissionsLocation =  {
                Android.Manifest.Permission.WriteExternalStorage,
                Android.Manifest.Permission.ReadExternalStorage,
                Android.Manifest.Permission.ForegroundService
            };

            bool exitFlag = permissionsLocation.Aggregate(true, (current, perm) => current & ContextCompat.CheckSelfPermission(this, perm) == (int)Permission.Granted);

            if (exitFlag && Android.OS.Environment.IsExternalStorageManager)
            {
                AfterReceivingPermissions();
                return;
            }

            const int requestLocationId = 1;
            Snackbar.Make(FindViewById<DrawerLayout>(Resource.Id.drawer_layout), "Storage access is required for storing and playing songs", Snackbar.LengthIndefinite)
                    .SetAction("OK", v =>
                    {
                        if (!Android.OS.Environment.IsExternalStorageManager)
                        {
                            try
                            {
                                Intent intent = new Intent();
                                intent.SetAction(Settings.ActionManageAppAllFilesAccessPermission);
                                Android.Net.Uri uri = Android.Net.Uri.FromParts("package", this.PackageName, null);
                                intent.SetData(uri);
                                StartActivity(intent);
                            }catch(Exception ex)
                            {
#if DEBUG
                                MyConsole.WriteLine(ex.ToString());
#endif
                            }
                        }
                        RequestPermissions(permissionsLocation, requestLocationId);
                    }).Show();
        }

        private void AfterReceivingPermissions()
        {
            string privatePath = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;

            if (!Directory.Exists(path))
            {
#if DEBUG
                MyConsole.WriteLine("Creating " + $"{path}");
#endif
                Directory.CreateDirectory(path);
            }

            if (!Directory.Exists($"{privatePath}/tmp"))
            {
#if DEBUG
                MyConsole.WriteLine("Creating " + $"{privatePath}/tmp");
#endif
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
            new Thread(() => {
                stateHandler.Artists.Add(new Artist("No Artist", "Default"));
                FileManager.DiscoverFiles();
                if (stateHandler.Songs.Count < FileManager.GetSongs().Count)
                {
                    FileManager.GenerateList(FileManager.MusicFolder);
                }

                /*stateHandler.Songs = stateHandler.Songs.OrderBy(song => song.Name).ToList();
                Console.WriteLine("----------------Alphabetically-------------------");
                foreach (var song in stateHandler.Songs)
                {
                    Console.WriteLine(song.ToString());
                }*/
                
                stateHandler.Songs = stateHandler.Songs.Order(SongOrderType.ByDate);
                /*Artist a = new Artist("otestuj ma", "default");
                stateHandler.Artists.Add(a);
                a = new Artist("specialny", "default");
                stateHandler.Artists = stateHandler.Artists.Distinct().ToList();
                stateHandler.Albums = stateHandler.Albums.Distinct().ToList();
                stateHandler.Songs = stateHandler.Songs.Distinct().ToList();*/
                //Console.WriteLine("----------------By Date-------------------");
                /*foreach (var song in stateHandler.Artists)
                {
                    Console.WriteLine(song.ToString());
                }*/
                //Console.WriteLine("----------------Search-------------------");
                /*foreach (var song in stateHandler.Songs.Search("ミ"))
                {
                    Console.WriteLine(song.ToString());
                }*/
                /*foreach (var song in stateHandler.Songs.Search("dark hour"))
                {
                    Console.WriteLine(song.Path);
                }
                try
                {
                    
                    Console.WriteLine($"FINGERPRINT: {FpCalc.InvokeFpCalc(new []{"-length", "16", $"{stateHandler.Songs.Search("dark hour").First().Path}"})}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }*/
            }).Start();
            stateHandler.SetView(this);
            receiver = new MyBroadcastReceiver(this);
            RegisterReceiver(receiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));
            
            Intent serviceIntent = new Intent(this, typeof(MediaService));
            
            /*if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                StartForegroundService(serviceIntent);
            }
            else
            {
                StartService(serviceIntent);
            }*/
            
            StartForegroundService(serviceIntent);
            if (!BindService(serviceIntent, ServiceConnection, Bind.Important))
            {
#if DEBUG
                MyConsole.WriteLine("Cannot connect to MediaService");
#endif
            }
            /*Thread.Sleep(5000);
            ServiceConnection.Binder?.Service?.NextSong();*/
        }
    }
}

