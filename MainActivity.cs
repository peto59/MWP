using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Navigation;
using Google.Android.Material.Snackbar;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using System.Text;
using Android.Provider;
using Android.Text;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Ass_Pain.BackEnd;
using Ass_Pain.BackEnd.Network;
using Octokit;
using Xamarin.Essentials;
using Activity = Android.App.Activity;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Application = Android.App.Application;
using Encoding = System.Text.Encoding;
using FileMode = System.IO.FileMode;
using FileProvider = AndroidX.Core.Content.FileProvider;
using Stream = System.IO.Stream;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain
{
    /// <inheritdoc cref="AndroidX.AppCompat.App.AppCompatActivity" />
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, Description = "@string/app_description")]
    //[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "text/plain")]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        // public static Slovenska_prostituka player = new Slovenska_prostituka();
        public static APIThrottler throttler = new APIThrottler();
        private static MyBroadcastReceiver _receiver;
        public static StateHandler stateHandler = new StateHandler();
        public static readonly MediaServiceConnection ServiceConnection = new MediaServiceConnection();
        private const int ActionInstallPermissionRequestCode = 10356;
        private const int ActionPermissionsRequestCode = 13256;
        private Typeface? font;
        
        /*
         * Fragments
         */
        private YoutubeFragment? youtubeFragment;
        private ShareFragment? shareFragment;
        
        private SongsFragment? songsFragment;
        private AlbumAuthorFragment? albumsFragment;
        private PlaylistsFragment? playlistsFragment;
        
        
        bool activeFragment = false;
        
      
        private DrawerLayout? drawer;

        /// <inheritdoc />
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Finish();   
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            Toolbar? toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            if (toolbar != null) SetSupportActionBar(toolbar);
            
            //RequestMyPermission();

            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            
            string action = Intent?.GetStringExtra("action");
            if (action == "openDrawer")
                drawer?.OpenDrawer(GravityCompat.Start);

            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer?.OpenDrawer(GravityCompat.Start);
            drawer?.AddDrawerListener(toggle);
            toggle.SyncState();

            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView?.SetNavigationItemSelectedListener(this);
            
            side_player.populate_side_bar(this, Assets);
            stateHandler.SetView(this);
            _receiver = new MyBroadcastReceiver();
            RegisterReceiver(_receiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));
            
            VersionTracking.Track();
            
            sbyte remainingDialogsBeforeRequestingPermissions = 0;

            if (SettingsManager.CheckUpdates == AutoUpdate.NoState)
            {
                remainingDialogsBeforeRequestingPermissions--;
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle("Automatic Updates");
                builder.SetMessage("Would you like to enable automatic updates?");

                builder.SetPositiveButton("Yes", delegate
                {
                    // ReSharper disable once AccessToModifiedClosure
                    remainingDialogsBeforeRequestingPermissions++;
                    SettingsManager.CheckUpdates = AutoUpdate.Requested;
                    if (remainingDialogsBeforeRequestingPermissions == 0)
                    {
                        RequestMyPermission();
                    }
                });

                builder.SetNegativeButton("No", delegate
                {
                    remainingDialogsBeforeRequestingPermissions++;
                    SettingsManager.CheckUpdates = AutoUpdate.Forbidden;
                    if (remainingDialogsBeforeRequestingPermissions == 0)
                    {
                        RequestMyPermission();
                    }
                });

                AlertDialog dialog = builder.Create();
                dialog.Show();
            }

            if (SettingsManager.CanUseNetwork == CanUseNetworkState.None)
            {
                remainingDialogsBeforeRequestingPermissions--;
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle("Music sharing");
                builder.SetMessage("Would you like to enable network scanning for available devices for music sharing? Warning: this requires precise location access for security reasons.");

                builder.SetPositiveButton("Yes", delegate
                {
                    remainingDialogsBeforeRequestingPermissions++;
                    SettingsManager.CanUseNetwork = CanUseNetworkState.Allowed;
                    if (remainingDialogsBeforeRequestingPermissions == 0)
                    {
                        RequestMyPermission();
                    }
                });

                builder.SetNegativeButton("No", delegate
                {
                    remainingDialogsBeforeRequestingPermissions++;
                    SettingsManager.CanUseNetwork = CanUseNetworkState.Rejected;
                    if (remainingDialogsBeforeRequestingPermissions == 0)
                    {
                        RequestMyPermission();
                    }
                });

                AlertDialog dialog = builder.Create();
                dialog.Show();
            }
            
            if (remainingDialogsBeforeRequestingPermissions == 0)
            {
                RequestMyPermission();
            }

            //rest of the stuff that was here is in AfterReceivingPermissions()

            // notififcations
            //Local_notification_service notif_service = new Local_notification_service();
            //notif_service.song_control_notification();
            //new Thread(() => { Thread.Sleep(1500); Downloader.SearchAPI(); }).Start();

            
            /*
             * Managing Frgamentation
             */
            youtubeFragment = new YoutubeFragment(this);
            shareFragment = new ShareFragment(this, Assets);
            
            songsFragment = new SongsFragment(this, Assets);
            albumsFragment = new AlbumAuthorFragment(this, Assets);
            playlistsFragment = new PlaylistsFragment(this, Assets);
            
            
            /*
             * Font changing and button clicks in Nav Menu
             */
            font = Typeface.CreateFromAsset(Assets, "sulphur.ttf");
            
            TextView? songsNavigationButton = FindViewById<TextView>(Resource.Id.MainNavManuItemSongs);
            TextView? playlistsNavigationButton = FindViewById<TextView>(Resource.Id.MainNavManuItemPlaylists);
            TextView? albumsNavigationButton = FindViewById<TextView>(Resource.Id.MainNavManuItemAlbums);

            TextView? downloadNavigationButton = FindViewById<TextView>(Resource.Id.MainNavManuItemDownload);
            TextView? shareNavigationButton = FindViewById<TextView>(Resource.Id.MainNavManuItemUpload);
            TextView? settingsNavigationButton = FindViewById<TextView>(Resource.Id.MainNavManuItemSettings);

            if (playlistsNavigationButton != null) playlistsNavigationButton.Typeface = font;
            if (albumsNavigationButton != null) albumsNavigationButton.Typeface = font;
            if (songsNavigationButton != null) songsNavigationButton.Typeface = font;
            if (downloadNavigationButton != null) downloadNavigationButton.Typeface = font;
            if (shareNavigationButton != null) shareNavigationButton.Typeface = font;
            if (settingsNavigationButton != null) settingsNavigationButton.Typeface = font;

            if (albumsNavigationButton != null)
                albumsNavigationButton.Click += (_, _) =>
                {
                    var fragmentTransaction = SupportFragmentManager.BeginTransaction();
                    if (!activeFragment)
                    {
                        fragmentTransaction.Add(Resource.Id.MainFragmentLayoutDynamic, albumsFragment);
                        activeFragment = true;
                    }
                    else if (activeFragment)
                        fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, albumsFragment);

                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                    drawer?.CloseDrawer(GravityCompat.Start);
                    Title = "Albums";
                };

            if (playlistsNavigationButton != null)
                playlistsNavigationButton.Click += (_, _) =>
                {
                    var fragmentTransaction = SupportFragmentManager.BeginTransaction();
                    if (!activeFragment)
                    {
                        fragmentTransaction.Add(Resource.Id.MainFragmentLayoutDynamic, playlistsFragment);
                        activeFragment = true;
                    }
                    else if (activeFragment)
                        fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, playlistsFragment);

                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                    drawer?.CloseDrawer(GravityCompat.Start);
                    Title = "Playlists";
                };

            if (songsNavigationButton != null)
                songsNavigationButton.Click += (_, _) =>
                {
                    var fragmentTransaction = SupportFragmentManager.BeginTransaction();
                    if (!activeFragment)
                    {
                        fragmentTransaction.Add(Resource.Id.MainFragmentLayoutDynamic, songsFragment);
                        activeFragment = true;
                    }
                    else if (activeFragment)
                        fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, songsFragment);

                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                    drawer?.CloseDrawer(GravityCompat.Start);
                    Title = "Songs";
                };
            

            if (downloadNavigationButton != null)
                downloadNavigationButton.Click += (_, _) =>
                {
                    var fragmentTransaction = SupportFragmentManager.BeginTransaction();
                    if (!activeFragment)
                    {
                        fragmentTransaction.Add(Resource.Id.MainFragmentLayoutDynamic, youtubeFragment);
                        activeFragment = true;
                    }
                    else fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, youtubeFragment);

                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                    drawer?.CloseDrawer(GravityCompat.Start);
                    Title = "Download";
                };

            if (shareNavigationButton != null)
                shareNavigationButton.Click += (_, _) =>
                {
                    var fragmentTransaction = SupportFragmentManager.BeginTransaction();
                    if (!activeFragment)
                    {
                        fragmentTransaction.Add(Resource.Id.MainFragmentLayoutDynamic, shareFragment, "shareFragTag");
                        activeFragment = true;
                    }
                    else fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, shareFragment, "shareFragTag");

                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                    drawer?.CloseDrawer(GravityCompat.Start);
                    Title = "Share";
                };
        }


        /// <inheritdoc />
        public override bool DispatchTouchEvent(MotionEvent ev)
        {
            if (ev.Action == MotionEventActions.Down)
            {
                View? v = CurrentFocus;
                if (v is EditText)
                {
                    Rect outRect = new Rect();
                    v.GetGlobalVisibleRect(outRect);
                    if (!outRect.Contains((int)ev.RawX, (int)ev.RawY))
                    {
                        v.ClearFocus();
                        InputMethodManager imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(v.WindowToken, 0);
                    }
                }
            }
            return base.DispatchTouchEvent(ev);
        }
        

        /// <inheritdoc />
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode != ActionInstallPermissionRequestCode) return;
#if DEBUG
            MyConsole.WriteLine($"resultCode: {resultCode}");       
#endif
            InstallUpdate($"{FileManager.PrivatePath}/tmp/update.apk");
        }

        /// <inheritdoc />
        [Obsolete("deprecated")]
        public override void OnBackPressed()
        {
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
        public override bool OnCreateOptionsMenu(IMenu? menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
           

            return base.OnCreateOptionsMenu(menu);
        }

        /// <inheritdoc />
        protected override void OnDestroy()
        {
            UnregisterReceiver(_receiver);
            base.OnDestroy();
        }

        /// <inheritdoc />
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            return id == Resource.Id.action_settings || base.OnOptionsItemSelected(item);
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            

            drawer?.CloseDrawer(GravityCompat.Start);
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
            List<string> permissionsLocation = new List<string>{Android.Manifest.Permission.ForegroundService};
            
            if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu) {
                permissionsLocation.Add(Android.Manifest.Permission.WriteExternalStorage);
                permissionsLocation.Add(Android.Manifest.Permission.ReadExternalStorage);
            }
            
            if (SettingsManager.CanUseNetwork == CanUseNetworkState.Allowed)
            {
                permissionsLocation.Add(Android.Manifest.Permission.AccessWifiState);
                permissionsLocation.Add(Android.Manifest.Permission.AccessFineLocation);
            }

            bool exitFlag = permissionsLocation.Aggregate(true, (current, perm) => current & ContextCompat.CheckSelfPermission(this, perm) == (int)Permission.Granted);

            if (exitFlag && Android.OS.Environment.IsExternalStorageManager)
            {
                AfterReceivingPermissions();
                return;
            }

            string explanation = Android.OS.Environment.IsExternalStorageManager switch
            {
                false when SettingsManager.CanUseNetwork == CanUseNetworkState.Allowed =>
                    "Storage access is required for storing and playing songs, location is required for identifying current network for security",
                true when SettingsManager.CanUseNetwork == CanUseNetworkState.Allowed =>
                    "Location is required for identifying current network for security",
                false when SettingsManager.CanUseNetwork != CanUseNetworkState.Allowed =>
                    "Storage access is required for storing and playing songs",
                _ => string.Empty
            };

            Snackbar.Make(FindViewById<DrawerLayout>(Resource.Id.drawer_layout),  explanation, Snackbar.LengthIndefinite)
                    .SetAction("OK", _ =>
                    {
                        if (!Android.OS.Environment.IsExternalStorageManager)
                        {
                            try
                            {
                                Intent intent = new Intent();
                                intent.SetAction(Settings.ActionManageAppAllFilesAccessPermission);
                                Android.Net.Uri uri = Android.Net.Uri.FromParts("package", PackageName, null);
                                intent.SetData(uri);
                                StartActivity(intent);
                            }catch(Exception ex)
                            {
#if DEBUG
                                MyConsole.WriteLine(ex);
#endif
                            }
                        }
                        RequestPermissions(permissionsLocation.ToArray(), ActionPermissionsRequestCode);
                    }).Show();
        }

        private void AfterReceivingPermissions()
        {
            FileManager.Innit();
            
            if (SettingsManager.CheckUpdates == AutoUpdate.Requested)
            {
                CheckUpdates();
            }

            if (SettingsManager.CanUseNetwork == CanUseNetworkState.Allowed)
            {
                //new Thread(NetworkManager.Listener).Start();
            }
            
            new Thread(() => {
                FileManager.DiscoverFiles(stateHandler.Songs.Count == 0);
                if (stateHandler.Songs.Count < FileManager.GetSongsCount())
                {
                    stateHandler.Songs = new List<Song>();
                    stateHandler.Artists = new List<Artist>();
                    stateHandler.Albums = new List<Album>();
                    
                    stateHandler.Artists.Add(new Artist("No Artist", "Default"));
                    FileManager.GenerateList(FileManager.MusicFolder);
                }

                if (stateHandler.Songs.Count != 0)
                {
                    stateHandler.Songs = stateHandler.Songs.Order(SongOrderType.ByDate);
                }
                RunOnUiThread(() => side_player.populate_side_bar(this, Assets));
#if DEBUG
                MyConsole.WriteLine($"Songs count {stateHandler.Songs.Count}");       
#endif
                
                //stateHandler.Artists = stateHandler.Artists.Distinct().ToList();
                //stateHandler.Albums = stateHandler.Albums.Distinct().ToList();
                //stateHandler.Songs = stateHandler.Songs.Distinct().ToList();

                //serialization test
                /*SongJsonConverter set = new SongJsonConverter(true);
                string x = JsonConvert.SerializeObject(stateHandler.Songs, set);
                Console.WriteLine($"length: {Encoding.UTF8.GetBytes(x).Length}");
                Console.WriteLine($"data: {x}");

                List<Song> y = JsonConvert.DeserializeObject<List<Song>>(x, set);
                Console.WriteLine(stateHandler.Songs[0].ToString());
                Console.WriteLine(y[0].ToString());
                Console.WriteLine(stateHandler.Songs[1].ToString());
                Console.WriteLine(y[1].ToString());
                Console.WriteLine("end");*/


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
            try
            {
                string currentVersionString = VersionTracking.CurrentBuild;
                const string owner = "peto59";
                const string repoName = "Ass_Pain";
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

                builder.SetNegativeButton("No", (_, _) =>
                {
                });

                AlertDialog dialog = builder.Create();
                dialog.Show();

            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
            }
          
        }

        private async void GetUpdate(string downloadUrl)
        {
            using HttpClient httpClient = new HttpClient();
            await File.WriteAllBytesAsync($"{FileManager.PrivatePath}/tmp/update.apk", await httpClient.GetByteArrayAsync(downloadUrl));
            
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

                        builder.SetPositiveButton("Yes", (_, _) =>
                        {
                            // Request the permission
                            Intent installPermissionIntent = new Intent(Settings.ActionManageUnknownAppSources, Android.Net.Uri.Parse("package:" + PackageName));
                            StartActivityForResult(installPermissionIntent, ActionInstallPermissionRequestCode);
                
                        });

                        builder.SetNegativeButton("No", (_, _) =>
                        {
                        });

                        AlertDialog dialog = builder.Create();
                        dialog.Show();
                    });
                }
                else
                {
                    InstallUpdate($"{FileManager.PrivatePath}/tmp/update.apk");
                }
                
            }
            else
            {
                InstallUpdate($"{FileManager.PrivatePath}/tmp/update.apk");
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

