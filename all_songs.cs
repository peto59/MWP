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

namespace Ass_Pain
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class all_songs : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        DrawerLayout drawer;

        Dictionary<ImageButton, string> album_buttons = new Dictionary<ImageButton, string>();

        string current = "";
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

            Button author = FindViewById<Button>(Resource.Id.author);
            Button all_songs = FindViewById<Button>(Resource.Id.all_songs);
            Button playlists = FindViewById<Button>(Resource.Id.playlists);

            author.Click += (sender, e) =>
            {
                populate_grid(0.0f);
            };
            all_songs.Click += (sender, e) =>
            {
                populate_grid(1.0f);
            };
            playlists.Click += (sender, e) =>
            {
                populate_grid(2.0f);
            };

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


        LinearLayout pupulate_songs(string song_path, float scale, bool ort, int ww, int hh, int[] btn_margins, int[] name_margins, int[] card_margins, int name_size, string album_song)
        {
            //リネアルレーアート作る
            LinearLayout ln_in = new LinearLayout(this);
            if (ort)
            {
                ln_in.Orientation = Orientation.Vertical;
            }
            else
            {
                ln_in.Orientation=Orientation.Horizontal;
            }
            ln_in.SetBackgroundResource(Resource.Drawable.rounded);

            LinearLayout.LayoutParams ln_in_params = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent,
                LinearLayout.LayoutParams.WrapContent
            );
            ln_in_params.SetMargins(card_margins[0], card_margins[1], card_margins[2], card_margins[3]);
            ln_in.LayoutParameters = ln_in_params;


            // ボッタン作って
            int w = (int)(ww * scale + 0.5f);
            int h = (int)(hh * scale + 0.5f);


            ImageButton mori = new ImageButton(this);
            LinearLayout.LayoutParams ll = new LinearLayout.LayoutParams(
                w, h
            );
            ll.SetMargins(btn_margins[0], btn_margins[1], btn_margins[2], btn_margins[3]);
            mori.LayoutParameters = ll;

            //<adam je kkt a jebal sa ti do kodu ale u neho fungoval>
            Bitmap image = null;
            TagLib.File tagFile;

            if (album_song == "album")
            {
                DirectoryInfo dir = new DirectoryInfo(song_path);
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

                    foreach (string song in FileManager.GetSongs(song_path))
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

            }
            else
            {
               
            
                try
                {
                    tagFile = TagLib.File.Create(
                        song_path//extracts image from first song of album that contains embedded picture
                    );
                    MemoryStream ms = new MemoryStream(tagFile.Tag.Pictures[0].Data.Data);
                    image = BitmapFactory.DecodeStream(ms);
            
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"Doesnt contain image: {song_path}");
                }
            
                if (image == null)
                {
                    image = BitmapFactory.DecodeStream(Assets.Open("music_placeholder.png")); //In case of no cover and no embedded picture show default image from assets 
                }

            }



            mori.SetImageBitmap(
                image
            ); 
            
            switch (album_song)
            {
                case "album":
                    mori.Click += new EventHandler(album_button_clicked);
                    break;
                case "song":
                    mori.Click += new EventHandler(songs_button_clicked);
                    break;
                case "author":
                    mori.Click += new EventHandler(author_button_clicked);
                    break;
            }
            album_buttons.Add(mori, song_path); 



            ln_in.AddView(mori);



            //アルブムの名前
            int h_name = (int)(40 * scale + 0.5f);

            TextView name = new TextView(this);
            name.Text = FileManager.GetNameFromPath(song_path);
            name.TextSize = name_size;
            name.SetTextColor(Color.White);
            name.TextAlignment = TextAlignment.Center;
            name.SetForegroundGravity(GravityFlags.Center);

            LinearLayout.LayoutParams ln_name_params = new LinearLayout.LayoutParams(
              w,
              h_name
            );
            ln_name_params.SetMargins(name_margins[0], name_margins[1], name_margins[2], name_margins[3]);
            
            name.LayoutParameters = ln_name_params;

            ln_in.AddView(name);

            return ln_in;
        }


        public LinearLayout album_tiles(float scale)
        {

            LinearLayout lin = new LinearLayout(this);
            lin.Orientation = Orientation.Vertical;
            LinearLayout.LayoutParams lin_params = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.WrapContent,
                    LinearLayout.LayoutParams.WrapContent
                );
            lin.LayoutParameters = lin_params;

            var albums = FileManager.GetAlbums();

            int[] button_margins = { 50, 50, 50, 0 };
            int[] name_margins = { 50, 0, 50, 50 };
            int[] card_margins = { 40, 50, 0, 0 };


            for (int i = 0; i < albums.Count; i++)
            {
                LinearLayout ln_in = pupulate_songs(albums[i], scale, true, 130, 160, button_margins, name_margins, card_margins, 15, "album");

                //全部加える
                lin.AddView(ln_in);

            }

            return lin;
        }

       

        public LinearLayout author_tiles(float scale)
        {

            LinearLayout lin = new LinearLayout(this);
            lin.Orientation = Orientation.Vertical;
            LinearLayout.LayoutParams lin_params = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.WrapContent,
                    LinearLayout.LayoutParams.WrapContent
                );
            lin.LayoutParameters = lin_params;

            var authors = FileManager.GetAuthors();

            int[] button_margins = { 50, 50, 50, 0 };
            int[] name_margins = { 50, 0, 50, 50 };
            int[] card_margins = { 50, 50, 0, 0 };

            foreach (var auth in authors)
            {
                LinearLayout ln_in = pupulate_songs(auth, scale, true, 130, 160, button_margins, name_margins, card_margins, 15, "author");

                //全部加える
                lin.AddView(ln_in);

            }

            return lin;
        }

        public void songs_button_clicked(Object sender, EventArgs e)
        {
            Console.WriteLine(current);
            Player player = new Player();
            player.GenerateQueue(current);
        }

        public void album_button_clicked(Object sender, EventArgs e)
        {
            ImageButton pressedButtoon = (ImageButton)sender;

            foreach(KeyValuePair<ImageButton, string> pr in album_buttons)
            {
                if (pr.Key == pressedButtoon)
                {
                    populate_grid(0.1f, pr.Value);
                    current = pr.Value;
                    break;
                }
            }
        }

        public void author_button_clicked(Object sender, EventArgs e)
        {
            ImageButton pressedButtoon = (ImageButton)sender;

            foreach (KeyValuePair<ImageButton, string> pr in album_buttons)
            {
                if (pr.Key == pressedButtoon)
                {
                    populate_grid(0.2f, pr.Value);
                    break;
                }
            }
        }

        public void populate_grid(float type, string path_for_01 = null, bool clear = true, int scroll_view_height = 150)
        {
            float scale = Resources.DisplayMetrics.Density;
            RelativeLayout main_rel_l = FindViewById<RelativeLayout>(Resource.Id.content);
            RelativeLayout mainnnnnnn = FindViewById<RelativeLayout>(Resource.Id.main_rel_l);

            ScrollView author_scroll = new ScrollView(this);
            ScrollView album_scroll = new ScrollView(this);

            switch (type)
            {
                case 0.0f: // 作家

                    main_rel_l.RemoveAllViews();

                    var display_metrics = Resources.DisplayMetrics;
                    int display_width = display_metrics.WidthPixels;

                    //作家
                    // ScrollView author_scroll = new ScrollView(this);
                    RelativeLayout.LayoutParams author_scroll_params = new RelativeLayout.LayoutParams(
                        display_width / 2,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    author_scroll_params.SetMargins(0, 150, 0, 0);
                    author_scroll_params.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    author_scroll.LayoutParameters = author_scroll_params;

                    LinearLayout author_lin = author_tiles(scale);
                    author_scroll.AddView(author_lin);
                    main_rel_l.AddView(author_scroll);



                    //アルブム
                    // ScrollView album_scroll = new ScrollView(this);
                    RelativeLayout.LayoutParams album_scroll_params = new RelativeLayout.LayoutParams(
                        display_width / 2,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    album_scroll_params.SetMargins(display_width / 2, 150, 0, 0);
                    album_scroll_params.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    album_scroll.LayoutParameters = album_scroll_params;

                    var all_albums = FileManager.GetAlbums();
                    LinearLayout album_lin = album_tiles(scale);
                    album_scroll.AddView(album_lin);

                    main_rel_l.AddView(album_scroll);

                   

                    break;
                case 0.1f: // songs from album
                    if (clear) main_rel_l.RemoveAllViews();

                    ScrollView songs_scroll = new ScrollView(this);
                    RelativeLayout.LayoutParams songs_scroll_params = new RelativeLayout.LayoutParams(
                        RelativeLayout.LayoutParams.MatchParent,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    songs_scroll_params.SetMargins(0, scroll_view_height, 0, 0);
                    songs_scroll_params.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    songs_scroll.LayoutParameters = songs_scroll_params;
                    

                    LinearLayout ln_main = new LinearLayout(this);
                    ln_main.Orientation = Orientation.Vertical;
                    RelativeLayout.LayoutParams ln_main_params = new RelativeLayout.LayoutParams(
                            RelativeLayout.LayoutParams.MatchParent,
                            RelativeLayout.LayoutParams.MatchParent
                    );
                    ln_main_params.SetMargins(20, 20, 20, 20);
                    ln_main.LayoutParameters = ln_main_params;

                    int[] button_margins = { 50, 50, 50, 50 };
                    int[] name_margins = { 50, 130, 50, 50 };
                    int[] card_margins = { 0, 50, 0, 0 };

                    if (path_for_01 != null)
                    {
                        var album_songs = FileManager.GetSongs(path_for_01);
                        foreach (var song in album_songs)
                        {
                            LinearLayout ln_in = pupulate_songs(
                                song, scale, false, 
                                150, 100,
                                button_margins, name_margins, card_margins,
                                17, 
                                "song"
                            );
                            ln_main.AddView(ln_in);
                        }
                    }
                    else
                    {
                        Console.WriteLine("bad path, ln; 280");
                    }

                    songs_scroll.AddView(ln_main);
                    main_rel_l.AddView(songs_scroll);

                    break;
                case 0.2f: // categorized album (by author)

                    main_rel_l.RemoveAllViews();



                    HorizontalScrollView hr = new HorizontalScrollView(this);
                    RelativeLayout.LayoutParams hr_params = new RelativeLayout.LayoutParams(
                        RelativeLayout.LayoutParams.MatchParent,
                        (int)(240 * scale + 0.5f)
                    );
                    hr_params.SetMargins(0, 150, 0, 0);
                    hr_params.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    hr.LayoutParameters = hr_params;

                    LinearLayout lin = new LinearLayout(this);

                    
                    var albums = FileManager.GetAlbums(path_for_01);

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
                        ln_in_params.SetMargins(50, 50, 0, 0);
                        ln_in.LayoutParameters = ln_in_params;



                        // ボッタン作って
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

                        // IRyS Ch. hololive-EN

                        mori.SetImageBitmap(
                            image
                        );
                        mori.Click += new EventHandler(album_button_clicked);
                        album_buttons.Add(mori, albums[i]);

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

                    

                    hr.AddView(lin);
                    main_rel_l.AddView(hr);

                    populate_grid(0.1f, path_for_01, false, (int)(300 * scale + 0.5f));

                    break;
                case 1.0f: // all

                    main_rel_l.RemoveAllViews();

                    ScrollView all_songs_scroll = new ScrollView(this);
                    RelativeLayout.LayoutParams all_songs_scroll_params = new RelativeLayout.LayoutParams(
                        RelativeLayout.LayoutParams.MatchParent,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    all_songs_scroll_params.SetMargins(0, scroll_view_height, 0, 0);
                    all_songs_scroll_params.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    all_songs_scroll.LayoutParameters = all_songs_scroll_params;


                    LinearLayout all_songs_ln_main = new LinearLayout(this);
                    all_songs_ln_main.Orientation = Orientation.Vertical;
                    RelativeLayout.LayoutParams all_songs_ln_main_params = new RelativeLayout.LayoutParams(
                            RelativeLayout.LayoutParams.MatchParent,
                            RelativeLayout.LayoutParams.MatchParent
                    );
                    all_songs_ln_main_params.SetMargins(20, 20, 20, 20);
                    all_songs_ln_main.LayoutParameters = all_songs_ln_main_params;

                    int[] all_songs_button_margins = { 50, 50, 50, 50 };
                    int[] all_songs_name_margins = { 50, 130, 50, 50 };
                    int[] all_songs_card_margins = { 0, 50, 0, 0 };

                    
                    var list_songs = FileManager.GetSongs();
                    foreach (var song in list_songs)
                    {
                        LinearLayout ln_in = pupulate_songs(
                            song, scale, false,
                            150, 100,
                            all_songs_button_margins, all_songs_name_margins, all_songs_card_margins,
                            17,
                            "song"
                        );
                        all_songs_ln_main.AddView(ln_in);
                    }
                    
                    all_songs_scroll.AddView(all_songs_ln_main);
                    main_rel_l.AddView(all_songs_scroll);


                    break;


                case 2.0f: // playlists
                    main_rel_l.RemoveAllViews();




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