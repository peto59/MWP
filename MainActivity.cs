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
using System.Net.Http;
using Newtonsoft.Json;
using AndroidX.AppCompat.Graphics.Drawable;
using Android.Widget;
using System.Threading;
using System.Text.Json;
using AngleSharp.Html;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.Content.PM;
using Android.Media;
using Android.Provider;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Ass_Pain.BackEnd.Network;
using Octokit;
using Xamarin.Essentials;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Application = Android.App.Application;
using FileMode = System.IO.FileMode;
using FileProvider = AndroidX.Core.Content.FileProvider;
using Stream = System.IO.Stream;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain
{
    
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, Description = "@string/app_description")]
    //[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "text/plain")]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        // public static Slovenska_prostituka player = new Slovenska_prostituka();
        public static NetworkManager nm = new NetworkManager();
        public static APIThrottler throttler = new APIThrottler();
        public static MyBroadcastReceiver receiver;
        public static StateHandler stateHandler = new StateHandler();
        public static readonly MediaServiceConnection ServiceConnection = new MediaServiceConnection();
        private const int ActionInstallPermissionRequestCode = 10356;
        private const int ActionPermissionsRequestCode = 13256;
        
        

        DrawerLayout drawer;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Finish();   
            Platform.Init(this, savedInstanceState);
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
            stateHandler.SetView(this);
            receiver = new MyBroadcastReceiver();
            RegisterReceiver(receiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));
            
            VersionTracking.Track();

            if (SettingsManager.CheckUpdates == AutoUpdate.NoState)
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle("Automatic Updates");
                builder.SetMessage("Would you like to enable automatic updates?");

                builder.SetPositiveButton("Yes", (sender, args) =>
                {
                    SettingsManager.CheckUpdates = AutoUpdate.Requested;
                });

                builder.SetNegativeButton("No", (sender, args) =>
                {
                    SettingsManager.CheckUpdates = AutoUpdate.Forbidden;
                });

                AlertDialog dialog = builder.Create();
                dialog.Show();
            }

            //rest of the stuff that was here is in AfterReceivingPermissions()

            // notififcations
            //Local_notification_service notif_service = new Local_notification_service();
            //notif_service.song_control_notification();
            //new Thread(() => { Thread.Sleep(1500); Downloader.SearchAPI(); }).Start();

            
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == ActionInstallPermissionRequestCode)
            {
#if DEBUG
                MyConsole.WriteLine($"resultCode: {resultCode}");       
#endif
                InstallUpdate($"{FileManager.PrivatePath}/update.apk");
            }
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

        /// <inheritdoc />
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        /// <inheritdoc />
        protected override void OnDestroy()
        {
            UnregisterReceiver(receiver);
            base.OnDestroy();
        }

        /// <inheritdoc />
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            return id == Resource.Id.action_settings || base.OnOptionsItemSelected(item);
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

        /// <inheritdoc />
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
#if DEBUG
            MyConsole.WriteLine("OnRequestPermissionsResult Result");
#endif
            
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

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

        private void RequestMyPermission()
        {
            string[] permissionsLocation =  {
                //Android.Manifest.Permission.WriteExternalStorage,
                //Android.Manifest.Permission.ReadExternalStorage,
                Android.Manifest.Permission.ForegroundService
            };

            bool exitFlag = permissionsLocation.Aggregate(true, (current, perm) => current & ContextCompat.CheckSelfPermission(this, perm) == (int)Permission.Granted);

            if (exitFlag && Android.OS.Environment.IsExternalStorageManager)
            {
                AfterReceivingPermissions();
                return;
            }
            
            Snackbar.Make(FindViewById<DrawerLayout>(Resource.Id.drawer_layout), "Storage access is required for storing and playing songs", Snackbar.LengthIndefinite)
                    .SetAction("OK", _ =>
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
                        RequestPermissions(permissionsLocation, ActionPermissionsRequestCode);
                    }).Show();
        }

        private void AfterReceivingPermissions()
        {
            if (SettingsManager.CheckUpdates == AutoUpdate.Requested)
            {
                CheckUpdates();
            }
            string privatePath = Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
            string path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic)?.AbsolutePath;

            if (!Directory.Exists(path))
            {
#if DEBUG
                MyConsole.WriteLine("Creating " + $"{path}");
#endif
                if (path != null) Directory.CreateDirectory(path);
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
            
            DirectoryInfo di = new DirectoryInfo($"{FileManager.PrivatePath}/tmp/");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
#if DEBUG
                MyConsole.WriteLine($"Deleting {file}");
#endif
            }
            
            //new Thread(() => { nm.Listener(); }).Start();
            //test
            new Thread(() => {
                FileManager.DiscoverFiles();
                if (stateHandler.Songs.Count < FileManager.GetSongs().Count)
                {
                    stateHandler.Songs = new List<Song>();
                    stateHandler.Artists = new List<Artist>();
                    stateHandler.Albums = new List<Album>();
                    
                    stateHandler.Artists.Add(new Artist("No Artist", "Default"));
                    FileManager.GenerateList(FileManager.MusicFolder);
                }
                stateHandler.Songs = stateHandler.Songs.Order(SongOrderType.ByDate);
                RunOnUiThread(() => side_player.populate_side_bar(this));
                
                //stateHandler.Artists = stateHandler.Artists.Distinct().ToList();
                //stateHandler.Albums = stateHandler.Albums.Distinct().ToList();
                //stateHandler.Songs = stateHandler.Songs.Distinct().ToList();
                
            }).Start();
            
            
            Intent serviceIntent = new Intent(this, typeof(MediaService));
            
            StartForegroundService(serviceIntent);
            if (!BindService(serviceIntent, ServiceConnection, Bind.Important))
            {
#if DEBUG
                MyConsole.WriteLine("Cannot connect to MediaService");
#endif
            }
        }

        private async void CheckUpdates()
        {
            string currentVersionString = VersionTracking.CurrentBuild;
            string owner = "peto59";
            string repoName = "Ass_Pain";
#if DEBUG
            MyConsole.WriteLine("Checking for updates!");
            MyConsole.WriteLine($"Current version: {currentVersionString}");
#endif      
            GitHubClient client = new GitHubClient(new ProductHeaderValue("AssPain"));
            IReadOnlyList<Release> releases = await client.Repository.Release.GetAll(owner, repoName);
            Release release = releases.First(r => r.TagName == "latest");
            
            int.TryParse(release.Name, out int remoteVersion);
            int.TryParse(currentVersionString, out int currentVersion);
            if (remoteVersion <= currentVersion) return;
            
            
            AlertDialog.Builder builder = new AlertDialog.Builder(stateHandler.view);
            builder.SetTitle("New Update");
            builder.SetMessage("Would you like to download this update?");

            builder.SetPositiveButton("Yes", (sender, args) =>
            {
                _ = Task.Run(async () =>
                {
                    ReleaseAsset asset = await client.Repository.Release.GetAsset(owner, repoName, release.Assets.First(a => a.Name == "com.companyname.ass_pain-Signed.apk").Id);
                    GetUpdate(asset.BrowserDownloadUrl);
                });
                
            });

            builder.SetNegativeButton("No", (sender, args) =>
            {
            });

            AlertDialog dialog = builder.Create();
            dialog.Show();
            
        }

        private async void GetUpdate(string downloadUrl)
        {
            using HttpClient httpClient = new HttpClient();
            await File.WriteAllBytesAsync($"{FileManager.PrivatePath}/update.apk", await httpClient.GetByteArrayAsync(downloadUrl));
            
#if DEBUG
         MyConsole.WriteLine("Downloaded. Starting install!");   
#endif
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                if (PackageManager == null) return;
                if (!PackageManager.CanRequestPackageInstalls())
                {
                    RunOnUiThread(() =>
                    {
                        AlertDialog.Builder builder = new AlertDialog.Builder(stateHandler.view);
                        builder.SetTitle("Permissions needed!");
                        builder.SetMessage("We need permission to install apps to update. Would you like to grant it now?");

                        builder.SetPositiveButton("Yes", (sender, args) =>
                        {
                            // Request the permission
                            Intent installPermissionIntent = new Intent(Settings.ActionManageUnknownAppSources, Android.Net.Uri.Parse("package:" + PackageName));
                            StartActivityForResult(installPermissionIntent, ActionInstallPermissionRequestCode);
                
                        });

                        builder.SetNegativeButton("No", (sender, args) =>
                        {
                        });

                        AlertDialog dialog = builder.Create();
                        dialog.Show();
                    });
                }
                else
                {
                    InstallUpdate($"{FileManager.PrivatePath}/update.apk");
                }
                
            }
            else
            {
                InstallUpdate($"{FileManager.PrivatePath}/update.apk");
            }
        }

        private void InstallUpdate(string path)
        {
            string authority = $"{Application.Context.PackageName}.fileprovider";
            Java.IO.File apkFile = new Java.IO.File(path);
            Android.Net.Uri apkUri = FileProvider.GetUriForFile(this, authority, apkFile);
            
            Intent installIntent = new Intent(Intent.ActionView);
            installIntent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
            installIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
            installIntent.AddFlags(ActivityFlags.NewTask);
            StartActivity(installIntent);
        }
    }
}

