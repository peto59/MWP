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

namespace Ass_Pain
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class all_songs : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        DrawerLayout drawer;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_all_songs);

            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);


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

            // -=-=-=-=-=
            set_buttons_color(Resource.Id.author);
            set_buttons_color(Resource.Id.all_songs);
            set_buttons_color(Resource.Id.playlists);

            populate_grid(0);
        }

        public void set_buttons_color(int id)
        {
            Button authors = FindViewById<Button>(id);
            authors.Background.SetColorFilter(
                Color.Rgb(255, 76, 41),
                PorterDuff.Mode.Multiply
            );
            authors.SetTextColor(Color.Black);
        }

        public void populate_grid(int type)
        {
            
            
            switch (type)
            {
                case 0: // author
                    LinearLayout lin = FindViewById<LinearLayout>(Resource.Id.linearLayout1);

                    var albums = FileManager.GetAlbums();

                    for (int i = 0; i < albums.Count; i++)
                    {
                        //リネアルレーアート作る
                        LinearLayout ln_in = new LinearLayout(this);
                        ln_in.Orientation = Orientation.Vertical;
                        ln_in.SetBackgroundResource(Resource.Drawable.rounded);

                        LinearLayout.LayoutParams ln_in_params = new LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MatchParent,
                            LinearLayout.LayoutParams.WrapContent
                        );
                        ln_in_params.SetMargins(50, 0, 0, 0);
                        ln_in.LayoutParameters = ln_in_params;



                        // ボッタン作って
                        float scale = Resources.DisplayMetrics.Density;
                        int w = (int)(150 * scale + 0.5f);
                        int h = (int)(180 * scale + 0.5f);


                        ImageButton mori = new ImageButton(this);
                        LinearLayout.LayoutParams ll = new LinearLayout.LayoutParams(
                            w, h
                        );
                        ll.SetMargins(50, 50, 50, 0);
                        mori.LayoutParameters = ll;

                        //<adam je kkt a jebal sa ti do kodu ale u neho fungoval>
                        Bitmap image = null;
                        DirectoryInfo dir = new DirectoryInfo(albums[i]);
                        FileInfo[] files = dir.GetFiles("cover.*");
                        if (files.Length > 0)
                        {
                            string validFileTypes = ".jpg,.png,.webm";
                            foreach (FileInfo file in files)
                            {
                                if (validFileTypes.Contains(file.Extension))
                                {
                                    image = BitmapFactory.DecodeStream(File.OpenRead(file.FullName)); // extracts image from cover.* in album dir
                                    break;
                                }
                            }
                        }
                        if (image == null)
                        {
                            TagLib.File tagFile;

                            foreach (string song in FileManager.GetSongs(albums[i]))
                            {
                                try
                                {
                                    tagFile = TagLib.File.Create(
                                        song//extracts image from first song of album that contains embedded picture
                                    );
                                    MemoryStream ms = new MemoryStream(tagFile.Tag.Pictures[0].Data.Data);
                                    image = BitmapFactory.DecodeStream(ms);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine($"Doesnt contain image: {song}");
                                }
                            }
                            if (image == null)
                            {
                                image = BitmapFactory.DecodeStream(Assets.Open("music_placeholder.png")); //In case of no cover and no embedded picture show default image from assets 
                            }
                        }
                        //</adam je kkt a jebal sa ti do kodu ale u neho fungoval>

                        mori.SetImageBitmap(
                            image
                        );
                        ln_in.AddView(mori);



                        //アルブムの名前
                        int h_name = (int)(40 * scale + 0.5f);

                        TextView name = new TextView(this);
                        name.Text = FileManager.GetNameFromPath(albums[i]);
                        name.TextSize = 15;
                        name.SetTextColor(Color.White);
                        name.TextAlignment = TextAlignment.Center;

                        LinearLayout.LayoutParams ln_name_params = new LinearLayout.LayoutParams(
                          w,
                          h_name
                        );
                        ln_name_params.SetMargins(50, 0, 50, 50);
                        name.LayoutParameters = ln_name_params;

                        ln_in.AddView(name);



                        //全部加える
                        lin.AddView(ln_in);

                    }

                    break;
                case 1: // all
                    break;
                case 2: // playlists
                    break;
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
                Intent intetn = new Intent(this, typeof(equalizer));
                StartActivity(intetn);
            }
            else if (id == Resource.Id.nav_slideshow) // youtube
            {
                Intent intetn = new Intent(this, typeof(youtube));
                StartActivity(intetn);
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