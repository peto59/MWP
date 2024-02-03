using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using Google.Android.Material.Navigation;
using Google.Android.Material.Snackbar;
using MWP.BackEnd;
using MWP.BackEnd.Network;
using MWP.BackEnd.Player;
using MWP.DatatypesAndExtensions;
using Octokit;
using Xamarin.Essentials;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Application = Android.App.Application;
using Environment = Android.OS.Environment;
using Exception = System.Exception;
using FileProvider = AndroidX.Core.Content.FileProvider;
using FragmentTransaction = AndroidX.Fragment.App.FragmentTransaction;
using Thread = System.Threading.Thread;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Uri = Android.Net.Uri;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP
{
    /// <inheritdoc cref="AndroidX.AppCompat.App.AppCompatActivity" />
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, Description = "@string/app_description")]
    //[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "text/plain")]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        /// <summary>
        /// Instance of API Throttler
        /// </summary>
        public static readonly APIThrottler Throttler = new APIThrottler();
        private static MyBroadcastReceiver? _receiver;
        private static MyMediaBroadcastReceiver? _mediaReceiver;
        /// <summary>
        /// Instance of State Handler
        /// </summary>
        public static readonly StateHandler StateHandler = new StateHandler();
        private static readonly MediaServiceConnection ServiceConnectionPrivate = new MediaServiceConnection();
        private ActionBarDrawerToggle? toggle;
        private long lastBackPressedTime;
        /// <summary>
        /// Instance of Service Connection
        /// </summary>
        public static MediaServiceConnection ServiceConnection
        {
            get
            {
                if (ServiceConnectionPrivate is { Connected: false })
                {
#if DEBUG
                    MyConsole.WriteLine("Session not connected");
#endif
                    StateHandler.mainActivity.StartMediaService();
                }
                return ServiceConnectionPrivate;
            }
        }
        private const int ActionInstallPermissionRequestCode = 10356;
        private const int ActionPermissionsRequestCode = 13256;
        private Typeface? font;
        
        /*
         * Fragments
         */
        private YoutubeFragment? youtubeFragment;
        private ShareFragment? shareFragment;
        private SettingsFragment? settingsFragment;
        private SongsFragment? songsFragment;
        private AlbumAuthorFragment? albumsFragment;
        private PlaylistsFragment? playlistsFragment;
        
        bool activeFragment;
        
      
        private DrawerLayout? drawer;

        /// <inheritdoc />
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
#if DEBUG
            MyConsole.WriteLine("Creating MainActivity");
#endif
            StateHandler.mainActivity = this;
            // Finish();   
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            Toolbar? toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            if (toolbar != null) SetSupportActionBar(toolbar);
            
            //RequestMyPermission();

            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            
            string? action = Intent?.GetStringExtra("action");
            if (action == "openDrawer")
                drawer?.OpenDrawer(GravityCompat.Start);

            action = Intent?.GetStringExtra("NotificationAction");
            if (action is "ShowConnectionStatus" or "ShowSongList")
            {
                ProcessNetworkNotification(Intent);
            }

            toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer?.OpenDrawer(GravityCompat.Start);
            drawer?.AddDrawerListener(toggle);
            toggle.SyncState();

            NavigationView? navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            DisplayMetrics metric = new DisplayMetrics();
            WindowManager?.DefaultDisplay?.GetMetrics(metric);
            if (navigationView != null)
            {
                if (navigationView.LayoutParameters != null) navigationView.LayoutParameters.Width = metric.WidthPixels;
                navigationView.SetNavigationItemSelectedListener(this);
            }

            if (Assets != null)
            {
                SidePlayer.populate_side_bar(this, Assets);
            }
            StateHandler.SetView(this);
            _receiver = new MyBroadcastReceiver();
            RegisterReceiver(_receiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));
            _mediaReceiver = new MyMediaBroadcastReceiver();
            IntentFilter intentFilter = new IntentFilter();
            intentFilter.AddAction(MyMediaBroadcastReceiver.PLAY);
            intentFilter.AddAction(MyMediaBroadcastReceiver.PAUSE);
            intentFilter.AddAction(MyMediaBroadcastReceiver.SHUFFLE);
            intentFilter.AddAction(MyMediaBroadcastReceiver.TOGGLE_LOOP);
            intentFilter.AddAction(MyMediaBroadcastReceiver.NEXT_SONG);
            intentFilter.AddAction(MyMediaBroadcastReceiver.PREVIOUS_SONG);
            RegisterReceiver(_mediaReceiver, intentFilter);
            
            FileManager.EarlyInnit();
            
            VersionTracking.Track();
            

            // notififcations
            //Local_notification_service notif_service = new Local_notification_service();
            //notif_service.song_control_notification();
            //new Thread(() => { Thread.Sleep(1500); Downloader.SearchAPI(); }).Start();

            
          
            
            /*
             * Managing Frgamentation
             */
            youtubeFragment = new YoutubeFragment(this);
            if (Assets != null)
            {
                shareFragment = new ShareFragment(this, Assets);
            }
            
            songsFragment = new SongsFragment(this, Assets);
            albumsFragment = new AlbumAuthorFragment(this, Assets);
            playlistsFragment = new PlaylistsFragment(this, Assets);
            settingsFragment = new SettingsFragment(this, Assets);
            
            
            /*
             * Font changing and button clicks in Nav Menu
             */
            font = Typeface.CreateFromAsset(Assets, "sulphur.ttf");
            
            TextView? songsNavigationButton = FindViewById<TextView>(Resource.Id.MainNavManuItemSongs);
            TextView? playlistsNavigationButton = FindViewById<TextView>(Resource.Id.MainNavManuItemPlaylists);
            TextView? albumsNavigationButton = FindViewById<TextView>(Resource.Id.MainNavManuItemAlbums);

            //TextView? downloadNavigationButton = FindViewById<TextView>(Resource.Id.MainNavManuItemDownload);
            TextView? downloadNavigationButton = FindViewById<TextView>(Resource.Id.MainNavManuItemUpload);
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
                    FragmentTransaction fragmentTransaction = SupportFragmentManager.BeginTransaction();
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
                    FragmentTransaction fragmentTransaction = SupportFragmentManager.BeginTransaction();
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
                    FragmentTransaction fragmentTransaction = SupportFragmentManager.BeginTransaction();
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
                    FragmentTransaction fragmentTransaction = SupportFragmentManager.BeginTransaction();
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
                    FragmentTransaction fragmentTransaction = SupportFragmentManager.BeginTransaction();
                    if (!activeFragment)
                    {
                        if (shareFragment != null)
                            fragmentTransaction.Add(Resource.Id.MainFragmentLayoutDynamic, shareFragment,
                                "shareFragTag");
                        activeFragment = true;
                    }
                    else if (shareFragment != null)
                        fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, shareFragment,
                            "shareFragTag");

                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                    drawer?.CloseDrawer(GravityCompat.Start);
                    Title = "Share";
                };

            if (settingsNavigationButton != null)
                settingsNavigationButton.Click += (_, _) =>
                {
                    FragmentTransaction fragmentTransaction = SupportFragmentManager.BeginTransaction();
                    if (!activeFragment)
                    {
                        if (shareFragment != null)
                            fragmentTransaction.Add(Resource.Id.MainFragmentLayoutDynamic, settingsFragment,
                                "settingsFragTag");
                        activeFragment = true;
                    }
                    else if (shareFragment != null)
                        fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, settingsFragment,
                            "settingsFragTag");

                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                    drawer?.CloseDrawer(GravityCompat.Start);
                    Title = "Settings";
                };

            /*
             * Initialize Widget Service
             */
            WidgetServiceHandler.Init(this);
            
            ShowDialogs();
        }
        
        
        /// <inheritdoc />
        public override bool DispatchTouchEvent(MotionEvent? ev)
        {
            if (ev is { Action: MotionEventActions.Down })
            {
                View? v = CurrentFocus;
                if (v is EditText)
                {
                    Rect outRect = new Rect();
                    v.GetGlobalVisibleRect(outRect);
                    if (!outRect.Contains((int)ev.RawX, (int)ev.RawY))
                    {
                        v.ClearFocus();
                        InputMethodManager? imm = (InputMethodManager?)GetSystemService(InputMethodService);
                        imm?.HideSoftInputFromWindow(v.WindowToken, 0);
                    }
                }
            }
            return base.DispatchTouchEvent(ev);
        }
        

        /// <inheritdoc />
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
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
            AndroidX.Fragment.App.Fragment? myFragment = SupportFragmentManager.FindFragmentByTag("SettingsFromDialog");
            if (myFragment is { IsVisible: true })
            {
#if DEBUG
                MyConsole.WriteLine($"returning from initial settings");
#endif
                AfterInitialSettingsReturn();
            }

            bool canCloseDrawer = true;
            if (SupportFragmentManager is { Fragments: not null })
            {
                bool isAnyFragmentVisible = SupportFragmentManager.Fragments.Aggregate(false, (current, fragment) => current | fragment.IsVisible);
                canCloseDrawer = !isAnyFragmentVisible;
                if (!isAnyFragmentVisible && lastBackPressedTime + 2000 > DateTimeOffset.Now.ToUnixTimeMilliseconds())
                {
                    Finish();
                }
                else
                {
                    Toast.MakeText(this, "Press back again to exit", ToastLength.Short)?.Show();
                    lastBackPressedTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }
            }

            if (drawer != null && drawer.IsDrawerOpen(GravityCompat.Start))
            {
                if (!canCloseDrawer)
                {
                    drawer.CloseDrawer(GravityCompat.Start);
                }
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

        /// <inheritdoc />
        public bool OnNavigationItemSelected(IMenuItem item)
        {
            drawer?.CloseDrawer(GravityCompat.Start);
            return true;
        }

        private void ShowDialogs()
        {
            sbyte remainingDialogsBeforeRequestingPermissions = 0;
            
            if (SettingsManager.MoveFiles == MoveFilesEnum.None)
            { 
                remainingDialogsBeforeRequestingPermissions--;
                AlertDialog.Builder builder2 = new AlertDialog.Builder(this);
                builder2.SetTitle("Excluded folders");
                builder2.SetMessage("You can exclude folders in which the app searches in settings. Would you like to edit them now?");
                builder2.SetCancelable(false);

                builder2.SetPositiveButton("Yes", delegate
                {
                    // ReSharper disable once AccessToModifiedClosure
                    remainingDialogsBeforeRequestingPermissions++;

                    if (toggle != null)
                    {
                        toggle.DrawerIndicatorEnabled = false;
                        //TODO: pretty back button
                        toggle.SetHomeAsUpIndicator(Resource.Drawable.back);
                        toggle.ToolbarNavigationClickListener = new MyClickListener(AfterInitialSettingsReturn);
                        
                        toggle.SyncState();
                    }

                    SettingsFragment settingsFragmentAdam = new SettingsFragment(this, Assets);
                    FragmentTransaction settingsTransaction = SupportFragmentManager.BeginTransaction();
                    settingsTransaction.Add(Resource.Id.MainFragmentLayoutDynamic, settingsFragmentAdam, "SettingsFromDialog");
                    settingsTransaction.AddToBackStack("SettingsFromDialog");
                    drawer?.SetDrawerLockMode(DrawerLayout.LockModeLockedClosed, GravityCompat.Start);
                    drawer?.CloseDrawer(GravityCompat.Start);
                    settingsTransaction.Commit();
                });

                builder2.SetNegativeButton("No", delegate
                {
                    remainingDialogsBeforeRequestingPermissions++;
                    if (remainingDialogsBeforeRequestingPermissions == 0)
                    {
                        RequestMyPermission();
                    }
                });

                AlertDialog dialog2 = builder2.Create();
                dialog2.Show();
                
                remainingDialogsBeforeRequestingPermissions--;
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle("File moving");
                builder.SetMessage("Would you like to enable moving of files into hierarchy based on metadata into Music folder?");
                builder.SetCancelable(false);

                builder.SetPositiveButton("Yes", delegate
                {
                    remainingDialogsBeforeRequestingPermissions++;
                    SettingsManager.MoveFiles = MoveFilesEnum.Yes;
                    if (remainingDialogsBeforeRequestingPermissions == 0)
                    {
                        RequestMyPermission();
                    }
                });

                builder.SetNegativeButton("No", delegate
                {
                    remainingDialogsBeforeRequestingPermissions++;
                    SettingsManager.MoveFiles = MoveFilesEnum.No;
                    if (remainingDialogsBeforeRequestingPermissions == 0)
                    {
                        RequestMyPermission();
                    }
                });

                AlertDialog dialog = builder.Create();
                dialog.Show();
            }
            
            if (SettingsManager.CheckUpdates == AutoUpdate.NoState)
            {
                remainingDialogsBeforeRequestingPermissions--;
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle("Automatic Updates");
                builder.SetMessage("Would you like to enable automatic updates?");
                builder.SetCancelable(false);

                builder.SetPositiveButton("Yes", delegate
                {
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
            
            if (SettingsManager.ShouldUseChromaprintAtDiscover == UseChromaprint.None)
            {
                remainingDialogsBeforeRequestingPermissions--;
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle("Automatic metadata discover");
                builder.SetMessage("Would you like to enable searching for metadata such as Artist and Album for your songs on the internet?");
                builder.SetCancelable(false);

                builder.SetPositiveButton("Yes", delegate
                {
                    AlertDialog.Builder builder2 = new AlertDialog.Builder(this);
                    builder2.SetTitle("Precise searching");
                    builder2.SetMessage("Would you like to have precise metadata searching with need to manually confirm each new tag or automatic which can produce unexpected tags?");
                    builder2.SetCancelable(false);

                    builder2.SetPositiveButton("Manual", delegate
                    {
                        remainingDialogsBeforeRequestingPermissions++;
                        SettingsManager.ShouldUseChromaprintAtDiscover = UseChromaprint.Manual;
                        if (remainingDialogsBeforeRequestingPermissions == 0)
                        {
                            RequestMyPermission();
                        }
                    });

                    builder2.SetNegativeButton("Automatic", delegate
                    {
                        remainingDialogsBeforeRequestingPermissions++;
                        SettingsManager.ShouldUseChromaprintAtDiscover = UseChromaprint.Automatic;
                        if (remainingDialogsBeforeRequestingPermissions == 0)
                        {
                            RequestMyPermission();
                        }
                    });

                    AlertDialog dialog2 = builder2.Create();
                    dialog2.Show();
                });

                builder.SetNegativeButton("No", delegate
                {
                    remainingDialogsBeforeRequestingPermissions++;
                    SettingsManager.ShouldUseChromaprintAtDiscover = UseChromaprint.No;
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
                builder.SetCancelable(false);

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
            if (SettingsManager.CanUseNetwork == CanUseNetworkState.Allowed)
            {

                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) ==
                    (int)Permission.Granted)
                {
                    if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessBackgroundLocation) !=
                        (int)Permission.Granted)
                    {
                        shouldRequestAgain = true;
                    }
                }
                else
                {
                    shouldRequestAgain = true;
                }
            }
            
            

            if (!Environment.IsExternalStorageManager || shouldRequestAgain)
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
            List<string> permissionsLocation = new List<string>{Manifest.Permission.ForegroundService};
            
            if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu) {
                permissionsLocation.Add(Manifest.Permission.WriteExternalStorage);
                permissionsLocation.Add(Manifest.Permission.ReadExternalStorage);
            }
            
            if (SettingsManager.CanUseNetwork == CanUseNetworkState.Allowed)
            {
                permissionsLocation.Add(Manifest.Permission.AccessWifiState);
                permissionsLocation.Add(
                    ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) ==
                    (int)Permission.Granted
                        ? Manifest.Permission.AccessBackgroundLocation
                        : Manifest.Permission.AccessFineLocation);
            }

            bool exitFlag = permissionsLocation.Aggregate(true, (current, perm) => current & ContextCompat.CheckSelfPermission(this, perm) == (int)Permission.Granted);

            if (exitFlag && Environment.IsExternalStorageManager)
            {
                AfterReceivingPermissions();
                return;
            }

            //TODO: add normal explanation for ExternalStorageManager and BackgroundLocation
            string explanation = Environment.IsExternalStorageManager switch
            {
                false when SettingsManager.CanUseNetwork == CanUseNetworkState.Allowed =>
                    "Storage access is required for storing and playing songs, location is required for identifying current network for security",
                true when SettingsManager.CanUseNetwork == CanUseNetworkState.Allowed =>
                    "Location is required for identifying current network for security",
                false when SettingsManager.CanUseNetwork != CanUseNetworkState.Allowed =>
                    "Storage access is required for storing and playing songs",
                _ => string.Empty
            };
            
            DrawerLayout? view = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (view != null)
                Snackbar.Make(view, explanation, BaseTransientBottomBar.LengthIndefinite)
                    .SetAction("OK", _ =>
                    {
                        if (!Environment.IsExternalStorageManager)
                        {
                            try
                            {
                                Intent intent = new Intent();
                                intent.SetAction(Settings.ActionManageAppAllFilesAccessPermission);
                                Uri? uri = Uri.FromParts("package", PackageName, null);
                                intent.SetData(uri);
                                StartActivity(intent);
                            }
                            catch (Exception ex)
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
            FileManager.LateInnit();
            
            
            if (SettingsManager.CheckUpdates == AutoUpdate.Requested)
            {
                CheckUpdates();
            }

            if (SettingsManager.CanUseNetwork == CanUseNetworkState.Allowed)
            {
                new Thread(NetworkManager.Listener).Start();
            }

            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            LayoutInflater? ifl = LayoutInflater.From(this);
            View? view = ifl?.Inflate(Resource.Layout.discovery_popup_layout, null);
            builder.SetView(view);
            builder.SetCancelable(false);
            
            AlertDialog dialog = builder.Create();
            dialog.Show();
            
            TextView? textView = view?.FindViewById<TextView>(Resource.Id.discoveryTextView);
            System.Timers.Timer timer = new System.Timers.Timer(500);
            
            timer.Elapsed += (_, _) => {
                RunOnUiThread(() =>
                {
                    if (textView != null)
                        textView.Text =
                            $"Indexing songs {System.Environment.NewLine}{StateHandler.Songs.Count} songs indexed {System.Environment.NewLine}Please wait";
                });
            };
            timer.Start();
            
            new Thread(() => {
                FileManager.DiscoverFiles(StateHandler.Songs.Count == 0);
                bool order = false;
                if (StateHandler.Songs.Count < FileManager.GetSongsCount())
                {
#if DEBUG
                    MyConsole.WriteLine("Generating new songs");
#endif
                    StateHandler.Songs = new List<Song>();
                    StateHandler.Artists = new List<Artist>();
                    StateHandler.Albums = new List<Album>();
                    
                    StateHandler.Artists.Add(new Artist("No Artist", "Default"));
                    FileManager.GenerateList(FileManager.MusicFolder);
                    order = true;
                }

                if (StateHandler.Songs.Count != 0 && order)
                {
                    StateHandler.Songs = StateHandler.Songs.Order(SongOrderType.ByDate);
                }
                RunOnUiThread(() =>
                {
                    if (Assets != null) SidePlayer.populate_side_bar(this, Assets);
                    dialog.Hide();
                    timer.Stop();
                    timer.Dispose();
                    //TODO: crashing because doing something on disposed textView
                    /*dialog.Dispose();
                    builder.Dispose();
                    view?.Dispose();
                    ifl?.Dispose();
                    textView?.Dispose();*/
                });
#if DEBUG
                MyConsole.WriteLine($"Songs count {StateHandler.Songs.Count}");       
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
        }

        /// <summary>
        /// Starts <see cref="MediaService"/>
        /// </summary>
        public void StartMediaService()
        {
            Intent serviceIntent = new Intent(Application.Context, typeof(MediaService));
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            if (!BindService(serviceIntent, ServiceConnectionPrivate, Bind.Important | Bind.AutoCreate))
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
                const string repoName = "MWP";
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
            
            
                AlertDialog.Builder builder = new AlertDialog.Builder(StateHandler.view);
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
                        AlertDialog.Builder builder = new AlertDialog.Builder(StateHandler.view);
                        builder.SetTitle("Permissions needed!");
                        builder.SetMessage("We need permission to install apps to update. Would you like to grant it now?");

                        builder.SetPositiveButton("Yes", (_, _) =>
                        {
                            // Request the permission
                            Intent installPermissionIntent = new Intent(Settings.ActionManageUnknownAppSources, Uri.Parse("package:" + PackageName));
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
            Uri? apkUri = FileProvider.GetUriForFile(this, authority, apkFile);
            
            Intent installIntent = new Intent(Intent.ActionView);
            installIntent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
            installIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
            installIntent.AddFlags(ActivityFlags.NewTask);
            StartActivity(installIntent);
        }

        private void AfterInitialSettingsReturn()
        {
            if (toggle != null)
            {
                toggle.DrawerIndicatorEnabled = true;
                toggle.SetHomeAsUpIndicator(null);
                toggle.ToolbarNavigationClickListener = null;
                toggle.SyncState();
            }
            drawer?.SetDrawerLockMode(DrawerLayout.LockModeUnlocked, GravityCompat.Start);
            drawer?.OpenDrawer(GravityCompat.Start);
            SupportFragmentManager.PopBackStack();
            RequestMyPermission();
        }

        private void ProcessNetworkNotification(Intent? intent)
        {
            string? action = Intent?.GetStringExtra("NotificationAction");
            string? remoteHostname = Intent?.GetStringExtra("RemoteHostname");
            if (action != null && remoteHostname != null)
            {
                if (action == "ShowConnectionStatus")
                {
                    NewConnectionDialog(remoteHostname, this);
                }
                else if (action == "ShowSongList")
                {
                    if (StateHandler.OneTimeSendSongs.TryGetValue(remoteHostname, out List<Song> songs))
                    {
                        UiRenderFunctions.ListIncomingSongsPopup(songs, remoteHostname, this, () => {}, () => {});
                    }
                }
            }
        }

        /// <summary>
        /// Shows dialog for user to accept or reject untrusted connection attempt
        /// </summary>
        /// <param name="remoteHostname">Name of device wanting to connect</param>
        /// <param name="ctx">App context</param>
        public static void NewConnectionDialog(string remoteHostname, Context ctx)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(ctx);
            builder.SetTitle("New connection");
            builder.SetMessage($"{remoteHostname} wants to connect to your device.");
            builder.SetCancelable(false);

            builder.SetPositiveButton("Allow", delegate
            {
                StateHandler.OneTimeSendStates[remoteHostname] = UserAcceptedState.ConnectionAccepted;
            });

            builder.SetNegativeButton("Disconnect", delegate
            {
                StateHandler.OneTimeSendStates[remoteHostname] = UserAcceptedState.Cancelled;
            });
        }
    }

    internal class MyClickListener : Java.Lang.Object, View.IOnClickListener
    {
        private readonly Action action;

        internal MyClickListener(Action a)
        {
            action = a;
        }
        public void OnClick(View? v)
        {
            action.Invoke();
        }
    }
}

