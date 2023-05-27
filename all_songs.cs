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
using System.Runtime.InteropServices;
using Android.Service.Autofill;
using Android.Icu.Number;
using Org.Apache.Http.Conn;
using Com.Arthenica.Ffmpegkit;
using Android.Drm;
using AngleSharp.Html.Dom;
using Newtonsoft.Json;
using System.Threading;
using Org.Apache.Http.Authentication;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Android.Util;


namespace Ass_Pain
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class all_songs : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        DrawerLayout drawer;

        Dictionary<LinearLayout, string> album_buttons = new Dictionary<LinearLayout, string>();
        Dictionary<LinearLayout, int> song_buttons = new Dictionary<LinearLayout, int>();

        (string location, string album) where_are_you_are_you_are_you_are_you_are_you_are_ = ("", "");
        (bool is_auth, string auth) in_author = (false, "");

        List<string> selected_playlists = new List<string>();

        // Slovenska_prostituka player = MainActivity.player;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_all_songs);

            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            // get intent sent from side bar to navigate author
            {
                string intent_author = Intent.GetStringExtra("link_author");
                if (intent_author != "")
                    populate_grid(0.2f, intent_author);

            }

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

            accept_songs.wake_drag_button(this, Resource.Id.main_rel_l);

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
                in_author.is_auth = false;
                in_author.auth = "";
            };
            all_songs.Click += (sender, e) =>
            {
                populate_grid(1.0f);
                in_author.is_auth = false;
                in_author.auth = "";

            };
            playlists.Click += (sender, e) =>
            {
                populate_grid(2.0f);
                in_author.is_auth = false;
                in_author.auth = "";

            };


            FloatingActionButton create_playlist = FindViewById<FloatingActionButton>(Resource.Id.fab);
            create_playlist.Background.SetColorFilter(
                Color.Rgb(255, 76, 41),
                PorterDuff.Mode.Multiply
            );

            create_playlist.Click += new EventHandler(show_popup);

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

        
        void song_tiles_image_set(LinearLayout parent, string song_path, float scale, int ww, int hh, int[] btn_margins, string album_song, int name_size, int[] name_margins)
        {
            ImageView mori = new ImageView(this);
            LinearLayout.LayoutParams ll = new LinearLayout.LayoutParams(
                (int)(ww * scale + 0.5f), (int)(hh * scale + 0.5f)
            );
            ll.SetMargins(btn_margins[0], btn_margins[1], btn_margins[2], btn_margins[3]);
            mori.LayoutParameters = ll;

            //<adam je kkt a jebal sa ti do kodu ale u neho fungoval>
            Bitmap image = null;
            TagLib.File tagFile;

            if (album_song == "album" || album_song == "author")
            {
                DirectoryInfo dir = new DirectoryInfo(song_path);
                FileInfo[] files = dir.GetFiles("cover.*");
                if (files.Length > 0)
                {
                    string validFileTypes = ".jpg,.png,.webm";
                    Parallel.ForEach(files, (file, state) =>
                    {
                        if (validFileTypes.Contains(file.Extension))
                        {
                            image = BitmapFactory.DecodeStream(File.OpenRead(file.FullName)); // extracts image from cover.* in album dir
                            state.Break();
                        }

                    });

                }
                if (image == null)
                {
                    Parallel.ForEach(FileManager.GetSongs(song_path), (song, state) =>
                    {

                        try
                        {
                            tagFile = TagLib.File.Create(
                                song//extracts image from first song of album that contains embedded picture
                            );
                            MemoryStream ms = new MemoryStream(tagFile.Tag.Pictures[0].Data.Data);
                            image = BitmapFactory.DecodeStream(ms);
                            state.Break();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine($"Doesnt contain image: {song}");
                        }
                    });

                    if (image == null)
                    {
                        image = BitmapFactory.DecodeStream(Assets.Open("music_placeholder.png")); //In case of no cover and no embedded picture show default image from assets 
                    }
                }
                //</adam je kkt a jebal sa ti do kodu ale u neho fungoval> 

            }
            else
            {
                Console.WriteLine(FileManager.GetSongTitle(song_path));

                try
                {
                    tagFile = TagLib.File.Create(
                        song_path //extracts image from first song of album that contains embedded picture
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

            parent.AddView(mori);


            //アルブムの名前
            int h_name = (int)(42 * scale + 0.5f);

            TextView name = new TextView(this);
            name.Text = FileManager.GetNameFromPath(song_path);
            name.TextSize = name_size;
            name.SetTextColor(Color.White);
            name.TextAlignment = TextAlignment.Center;
            name.SetForegroundGravity(GravityFlags.Center);

            LinearLayout.LayoutParams ln_name_params = new LinearLayout.LayoutParams(
              (int)(130 * scale + 0.5f),
              LinearLayout.LayoutParams.WrapContent
            );
            ln_name_params.SetMargins(name_margins[0], name_margins[1], name_margins[2], name_margins[3]);

            name.LayoutParameters = ln_name_params;

            parent.SetGravity(GravityFlags.Center);
            parent.SetHorizontalGravity(GravityFlags.Center);
            parent.AddView(name);
            

        }

        LinearLayout pupulate_songs(
            string song_path, float scale, bool ort, int ww, int hh, int[] btn_margins, int[] name_margins, int[] card_margins, int name_size, string album_song, int index,
            LinearLayout lin_for_delete = null
        )
        {
            //リネアルレーアート作る
            LinearLayout ln_in = new LinearLayout(this);
            if (ort)
            {
                ln_in.Orientation = Orientation.Vertical;
            }
            else
            {
                ln_in.Orientation = Orientation.Horizontal;
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

           


            switch (album_song)
            {
                case "album":
                    ln_in.Click += new EventHandler(album_button_clicked);

                    ln_in.LongClick += (sender, e) =>
                    {
                        show_popup_song_edit(sender, e, song_path, lin_for_delete, ln_in, "album");
                    };

                    album_buttons.Add(ln_in, song_path);
                    break;
                case "song":
                    ln_in.Click += new EventHandler(songs_button_clicked);
                    

                    ln_in.LongClick += (sender, e) =>
                    {
                        show_popup_song_edit(sender, e, song_path, lin_for_delete, ln_in, "song");
                    };

                    song_buttons.Add(ln_in, index);
                    break;
                case "author":
                    ln_in.Click += new EventHandler(author_button_clicked);

                    ln_in.LongClick += (sender, e) =>
                    {
                        show_popup_song_edit(sender, e, song_path, lin_for_delete, ln_in, "album");
                    };

                    album_buttons.Add(ln_in, song_path); 
                    break;
            }


            ln_in.SetHorizontalGravity(GravityFlags.Center);
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

            List<string> albums = FileManager.GetAlbums();

            int[] button_margins = { 50, 50, 50, 0 };
            int[] name_margins = { 50, 50, 50, 50 };
            int[] card_margins = { 40, 50, 0, 0 };

            Parallel.For(0, albums.Count, i =>
            {

                LinearLayout ln_in = pupulate_songs(albums[i], scale, true, 130, 160, button_margins, name_margins, card_margins, 15, "album", i);
                song_tiles_image_set(
                    ln_in, albums[i], scale, 150, 100, 
                    button_margins, "album", 15, 
                    name_margins
                );

                //全部加える
                lin.AddView(ln_in);

            });

            /*for (int i = 0; i < albums.Count; i++)
            {
            }*/

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

            List<string> authors = FileManager.GetAuthors();

            int[] button_margins = { 50, 50, 50, 0 };
            int[] name_margins = { 50, 50, 50, 50 };
            int[] card_margins = { 50, 50, 0, 0 };


            for (int i = 0; i < authors.Count; i++)
            {
                LinearLayout ln_in = pupulate_songs(authors[i], scale, true, 130, 160, button_margins, name_margins, card_margins, 15, "author", i);
                song_tiles_image_set(
                    ln_in, authors[i], scale, 150, 100,
                    button_margins, "author", 17,
                    name_margins
                );
                //全部加える
                lin.AddView(ln_in);

            } 

            return lin;
        }

        

        public void songs_button_clicked(Object sender, EventArgs e)
        {
            LinearLayout pressedButtoon = (LinearLayout)sender;

            foreach (KeyValuePair<LinearLayout, int> pr in song_buttons)
            {
                if (pr.Key == pressedButtoon)
                {
                    if (where_are_you_are_you_are_you_are_you_are_you_are_.album == "all")
                    {
                        StartService(
                            new Intent(MediaService.ActionGenerateQueue, null, this, typeof(MediaService))
                            .PutExtra("sourceList", FileManager.GetSongs().ToArray())
                            .PutExtra("i", pr.Value)
                        );
                    }
                    else
                    {
                        StartService(
                           new Intent(MediaService.ActionGenerateQueue, null, this, typeof(MediaService))
                           .PutExtra("sourceList", FileManager.GetSongs(where_are_you_are_you_are_you_are_you_are_you_are_.album).ToArray())
                           .PutExtra("i", pr.Value)
                        );
                    }
                    break;
                }
            }
        }

        public void album_button_clicked(Object sender, EventArgs e)
        {
            LinearLayout pressedButtoon = (LinearLayout)sender;

            foreach(KeyValuePair<LinearLayout, string> pr in album_buttons)
            {
                if (pr.Key == pressedButtoon)
                {
                    where_are_you_are_you_are_you_are_you_are_you_are_.album = pr.Value;
                    populate_grid(0.1f, pr.Value);
                    in_author.is_auth = false;
                    in_author.auth = "";

                    break;
                }
            }
        }

        public void author_button_clicked(Object sender, EventArgs e)
        {
            LinearLayout pressedButtoon = (LinearLayout)sender;

            foreach (KeyValuePair<LinearLayout, string> pr in album_buttons)
            {
                if (pr.Key == pressedButtoon)
                {
                    where_are_you_are_you_are_you_are_you_are_you_are_.album = pr.Value;
                    populate_grid(0.2f, pr.Value);
                    in_author.is_auth = true;
                    in_author.auth = pr.Value;

                    break;
                }
            }
        }

        public void show_popup(object sender, EventArgs e)
        {
            Console.WriteLine("popup clicked");

            LayoutInflater ifl = LayoutInflater.From(this);
            View view = ifl.Inflate(Resource.Layout.new_playlist_popup, null);
            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetView(view);

            EditText user_data = view.FindViewById<EditText>(Resource.Id.editText);
            alert.SetCancelable(false).SetPositiveButton("submit", delegate
            {
                FileManager.CreatePlaylist(user_data.Text);
                Toast.MakeText(
                    this, user_data.Text + " Created successfully",
                    ToastLength.Short
                ).Show();
            })
                .SetNegativeButton("cancel", delegate
                {
                    alert.Dispose();
                });


            Android.App.AlertDialog dialog = alert.Create();
            dialog.Show();
        }



        public void add_alias_popup(string author_n)
        {
            Console.WriteLine("popup clicked");

            LayoutInflater ifl = LayoutInflater.From(this);
            View view = ifl.Inflate(Resource.Layout.add_alias_popup, null);
            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetView(view);

            Android.App.AlertDialog dialog = alert.Create();

            TextView author = view.FindViewById<TextView>(Resource.Id.author_name);
            author.Text = author_n;

            EditText user_input = view.FindViewById<EditText>(Resource.Id.user_author);
            Button sub = view.FindViewById<Button>(Resource.Id.submit_alias);
            sub.Click += delegate
            {
                FileManager.AddAlias(author_n, user_input.Text);


                dialog.Hide();
            };

            Button cancel = view.FindViewById<Button>(Resource.Id.cancel_alias);
            cancel.Click += delegate
            {
                dialog.Hide();
            };

            
            dialog.Show();
        }

        public void list_playlists_popup(object sender, EventArgs e, string path)
        {
            Console.WriteLine("popup clicked");
            float scale = Resources.DisplayMetrics.Density;

            LayoutInflater ifl = LayoutInflater.From(this);
            View view = ifl.Inflate(Resource.Layout.list_plas_popup, null);
            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetView(view);

            Android.App.AlertDialog dialog = alert.Create();

            LinearLayout ln = view.FindViewById<LinearLayout>(Resource.Id.playlists_list_la);

            List<string> plas = FileManager.GetPlaylist();
            foreach (string p in plas)
            {
                LinearLayout ln_in = new LinearLayout(this);
                ln_in.Orientation = Orientation.Vertical;
                ln_in.SetBackgroundResource(Resource.Drawable.rounded_light);

                LinearLayout.LayoutParams ln_in_params = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.MatchParent,
                    (int)(50 * scale + 0.5f)
                );
                ln_in_params.SetMargins(20, 20, 20, 20);
                ln_in.LayoutParameters = ln_in_params;
                ln_in.SetGravity(GravityFlags.Center);

                ln_in.Click += (sender, e) =>
                {
                    Console.WriteLine(path);
                    if (selected_playlists.Contains(p))
                    {
                        selected_playlists.Remove(p);
                        ln_in.SetBackgroundResource(Resource.Drawable.rounded_light);
                        Console.WriteLine("removed : " + p);
                    }
                    else
                    {
                        selected_playlists.Add(p);
                        Console.WriteLine("added: " + p);
                        ln_in.SetBackgroundResource(Resource.Drawable.rounded_dark);

                    }
                };

                // ---
                TextView name = new TextView(this);
                name.TextAlignment = TextAlignment.Center;
                name.SetTextColor(Color.White);
                name.Text = p;
                ln_in.AddView(name);


                ln.AddView(ln_in);
            }


            // ----
            Button submit = view.FindViewById<Button>(Resource.Id.submit_plas);
            submit.Background.SetColorFilter(
                Color.Rgb(255, 76, 41),
                PorterDuff.Mode.Multiply
            ); submit.SetTextColor(Color.Black);

            submit.Click += (sender, e) =>
            {
                foreach (string s in selected_playlists)
                {
                    Console.WriteLine(s + " " + selected_playlists.Count);

                    List<string> pla_songs = FileManager.GetPlaylist(s);
                    if (pla_songs.Contains(path))
                        Toast.MakeText(this, "already exists in : " + s, ToastLength.Short).Show();
                    else
                    {
                        FileManager.AddToPlaylist(s, path);

                        Toast.MakeText(
                            this, "added successfully",
                            ToastLength.Short
                        ).Show();
                    }

                }


                dialog.Hide();
                selected_playlists.Clear();

                
            };

            Button cancel = view.FindViewById<Button>(Resource.Id.cancel_plas);
            cancel.Background.SetColorFilter(
                Color.Rgb(255, 76, 41),
                PorterDuff.Mode.Multiply
            ); cancel.SetTextColor(Color.Black);

            cancel.Click += (sender, e) =>
            {
                dialog.Hide();
            };



            dialog.Show();
        }


        public void deleting_song(Object sender, EventArgs e, Android.App.AlertDialog di)
        {
            di.Hide();
        }

        public void are_you_sure(object sender, EventArgs e, string path, Android.App.AlertDialog di, LinearLayout lin_from_delete, LinearLayout lin_for_delete, string is_what)
        {
            Console.WriteLine("popup clicked");


            LayoutInflater ifl = LayoutInflater.From(this);
            View view = ifl.Inflate(Resource.Layout.are_you_sure_popup, null);
            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetView(view);

            Android.App.AlertDialog dialog = alert.Create();

            TextView txt = view.FindViewById<TextView>(Resource.Id.are_you_sure_text);
            txt.SetTextColor(Color.White);
            txt.Text = "Deleting: " + FileManager.GetNameFromPath(path); 

            Button yes = view.FindViewById<Button>(Resource.Id.yes_daddy);
            yes.Background.SetColorFilter(
                Color.Rgb(255, 76, 41),
                PorterDuff.Mode.Multiply
            ); yes.SetTextColor(Color.Black);

            yes.Click += delegate
            {
                FileManager.Delete(path);
                dialog.Hide();
                deleting_song(sender, e, di);

                if (is_what == "song")
                    lin_from_delete.RemoveView(lin_for_delete);
                else
                {
                    populate_grid(0.0f);
                    in_author.is_auth = false;
                    in_author.auth = "";
                }
                    

                Toast.MakeText(this, $"{path} has been deleted", ToastLength.Short).Show();
            };
            
            Button no = view.FindViewById<Button>(Resource.Id.you_are_not_sure);
            no.Background.SetColorFilter(
                Color.Rgb(255, 76, 41),
                PorterDuff.Mode.Multiply
            ); no.SetTextColor(Color.Black);

            no.Click += (sender, e) =>
            {
                deleting_song(sender, e, di);
            };
            
            dialog.Show();
        }

        public void show_popup_song_edit(object sender, EventArgs e, string path, LinearLayout lin_from_delet, LinearLayout lin_for_delete, string what_is)
        {
            Console.WriteLine("popup clicked");

            LayoutInflater ifl = LayoutInflater.From(this);
            View view;
            if (what_is == "song")
                view = ifl.Inflate(Resource.Layout.edit_song_popup, null);
            else
                view = ifl.Inflate(Resource.Layout.edit_album_popup, null);

            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetView(view);

            Android.App.AlertDialog dialog = alert.Create();

            /*
             * popup buttons start
             */
            if (what_is == "song")
            {
                Button add_to_pla = view.FindViewById<Button>(Resource.Id.add_to_pla);
                add_to_pla.Background.SetColorFilter(
                    Color.Rgb(255, 76, 41),
                    PorterDuff.Mode.Multiply
                ); add_to_pla.SetTextColor(Color.Black);
                add_to_pla.Click += (sender, e) =>
                {
                    dialog.Hide();
                    list_playlists_popup(sender, e, path);
                    Console.WriteLine(path);

                };
            }


            Button add_to_qu = view.FindViewById<Button>(Resource.Id.add_to_qu);
            Button delete = view.FindViewById<Button>(Resource.Id.delete);
            add_to_qu.Background.SetColorFilter(
                Color.Rgb(255, 76, 41),
                PorterDuff.Mode.Multiply
            ); add_to_qu.SetTextColor(Color.Black);
            delete.Background.SetColorFilter(
                Color.Rgb(255, 76, 41),
                PorterDuff.Mode.Multiply
            ); delete.SetTextColor(Color.Black);

            // handle clicked

           
            add_to_qu.Click += (sender, e) =>
            {
                StartService(
                    new Intent(MediaService.ActionAddToQueue, null, this, typeof(MediaService))
                    .PutExtra("addition", path)
                );
            };
            delete.Click += (sender, e) =>
            {
                are_you_sure(sender, e, path, dialog, lin_from_delet, lin_for_delete, what_is);
            };


            /*
             * popup buttons end
             */

            
            dialog.Show();
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

                    where_are_you_are_you_are_you_are_you_are_you_are_.location = "main";
                    where_are_you_are_you_are_you_are_you_are_you_are_.album = "";

                    DisplayMetrics display_metrics = Resources.DisplayMetrics;
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

                    List<string> all_albums = FileManager.GetAlbums();
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
                    int[] name_margins = { 50, 50, 50, 50 };
                    int[] card_margins = { 0, 50, 0, 0 };

                    if (path_for_01 != null)
                    {
                        List<string> album_songs = FileManager.GetSongs(path_for_01);
                        for (int i = 0; i < album_songs.Count; i++)
                        {

                            LinearLayout ln_in = pupulate_songs(
                                album_songs[i], scale, false,
                                150, 100,
                                button_margins, name_margins, card_margins,
                                17,
                                "song", i, ln_main
                            );
                            song_tiles_image_set(
                                ln_in, album_songs[i], scale, 150, 100,
                                button_margins, "song", 17,
                                name_margins
                            );
                            ln_main.AddView(ln_in);
                        }
                        
                    }
                    else
                    {
                        Console.WriteLine("bad path, ln; 845");
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
                    
                    
                    List<string> albums = FileManager.GetAlbums(path_for_01);
                    Parallel.For(0, albums.Count, i =>
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


                        ImageView mori = new ImageView(this);
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
                        Console.WriteLine("test");
                        mori.SetImageBitmap(
                            image
                        );
                        ln_in.Click += new EventHandler(album_button_clicked);
                        album_buttons.Add(ln_in, albums[i]);

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
                    });

                   

                    hr.AddView(lin);
                    main_rel_l.AddView(hr);

                    populate_grid(0.1f, path_for_01, false, (int)(300 * scale + 0.5f));

                    break;
                case 1.0f: // all

                    main_rel_l.RemoveAllViews();

                    where_are_you_are_you_are_you_are_you_are_you_are_.location = "all";
                    where_are_you_are_you_are_you_are_you_are_you_are_.album = "all";

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
                    int[] all_songs_name_margins = { 50, 50, 50, 50 };
                    int[] all_songs_card_margins = { 0, 50, 0, 0 };


                    List<Tuple<LinearLayout, int>> lazy_buffer = new List<Tuple<LinearLayout, int>>();

                    
                    List<string> list_songs = FileManager.GetSongs();
                    for (int i = 0; i < list_songs.Count; i++)
                    {

                        LinearLayout ln_in = pupulate_songs(
                            list_songs[i], scale, false,
                            150, 100,
                            all_songs_button_margins, all_songs_name_margins, all_songs_card_margins,
                            17,
                            "song", i, all_songs_ln_main
                        );


                        Console.WriteLine("this song is going to buffer " + list_songs[i]);
                        lazy_buffer.Add(new Tuple<LinearLayout, int>(ln_in, i));

                    }

                    for (int i = 0; i < Math.Min(5, lazy_buffer.Count); i++)
                    {
                        song_tiles_image_set(lazy_buffer[i].Item1, list_songs[lazy_buffer[i].Item2], scale, 150, 100, all_songs_button_margins, "song", 15, all_songs_name_margins);
                        all_songs_ln_main.AddView(lazy_buffer[i].Item1);
                    }
                   
                    lazy_buffer.RemoveRange(0, Math.Min(5, lazy_buffer.Count));

                    all_songs_scroll.ScrollChange += (sender, e) =>
                    {
                        View view = all_songs_ln_main.GetChildAt(all_songs_ln_main.ChildCount - 1);
                        int top_detect = all_songs_scroll.ScrollY;
                        int bottom_detect = view.Bottom - (all_songs_scroll.Height + all_songs_scroll.ScrollY);

                        if (bottom_detect == 0 && lazy_buffer.Count != 0)
                        {
                            Console.WriteLine("loading new");

                            for (int i = 0; i < Math.Min(5, lazy_buffer.Count); i++)
                            {
                                song_tiles_image_set(lazy_buffer[i].Item1, list_songs[lazy_buffer[i].Item2], scale, 150, 100, all_songs_button_margins, "song", 17, all_songs_name_margins);
                                all_songs_ln_main.AddView(lazy_buffer[i].Item1);
                            }

                            lazy_buffer.RemoveRange(0, Math.Min(5, lazy_buffer.Count));
                        }
                    };


                    all_songs_scroll.AddView(all_songs_ln_main);
                    main_rel_l.AddView(all_songs_scroll);


                    break;


                case 2.0f: // playlists
                    main_rel_l.RemoveAllViews();

                    ScrollView playlists_scroll = new ScrollView(this);
                    RelativeLayout.LayoutParams playlist_scroll_params = new RelativeLayout.LayoutParams(
                        RelativeLayout.LayoutParams.MatchParent,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    playlist_scroll_params.SetMargins(0, scroll_view_height, 0, 0);
                    playlist_scroll_params.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    playlists_scroll.LayoutParameters = playlist_scroll_params;


                    LinearLayout playlist_ln_main = new LinearLayout(this);
                    playlist_ln_main.Orientation = Orientation.Vertical;
                    RelativeLayout.LayoutParams playlist_ln_main_params = new RelativeLayout.LayoutParams(
                            RelativeLayout.LayoutParams.MatchParent,
                            RelativeLayout.LayoutParams.MatchParent
                    );
                    playlist_ln_main_params.SetMargins(20, 20, 20, 20);
                    playlist_ln_main.LayoutParameters = playlist_ln_main_params;

                    int[] playlist_button_margins = { 50, 50, 50, 50 };
                    int[] playlist_card_margins = { 0, 50, 0, 0 };


                    List<string> playlists = FileManager.GetPlaylist();
                    Parallel.ForEach(playlists, playlist =>
                    {

                        LinearLayout ln_in = new LinearLayout(this);
                        ln_in.Orientation = Orientation.Vertical;
                        ln_in.SetBackgroundResource(Resource.Drawable.rounded);

                        LinearLayout.LayoutParams ln_in_params = new LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MatchParent,
                            LinearLayout.LayoutParams.WrapContent
                        );
                        ln_in_params.SetMargins(
                            playlist_card_margins[0], playlist_card_margins[1], 
                            playlist_card_margins[2], playlist_card_margins[3]
                        );
                        ln_in.LayoutParameters = ln_in_params;


                        TextView pla_name = new TextView(this);
                        LinearLayout.LayoutParams name_params = new LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MatchParent,
                            LinearLayout.LayoutParams.WrapContent
                        );
                        name_params.SetMargins(0, 20, 0, 20);
                        pla_name.LayoutParameters = name_params; 
                        pla_name.Text = playlist;
                        pla_name.SetTextColor(Color.White);
                        pla_name.TextSize = 30;
                        pla_name.TextAlignment = TextAlignment.Center;

                        TextView songs_count = new TextView(this);
                        LinearLayout.LayoutParams count_params = new LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MatchParent,
                            LinearLayout.LayoutParams.WrapContent
                        );
                        count_params.SetMargins(0, 20, 0, 20);
                        songs_count.LayoutParameters = count_params;
                        songs_count.Text = $"number of songs: {FileManager.GetPlaylist(playlist).Count}";
                        songs_count.SetTextColor(Color.White);
                        songs_count.TextSize = 15;
                        songs_count.TextAlignment = TextAlignment.Center;


                        ln_in.Click += (sender, e) =>
                        {
                            populate_grid(2.1f, playlist);
                            //Toast.MakeText(this, playlist + " opend", ToastLength.Short).Show();
                        };

                        ln_in.AddView(pla_name);
                        ln_in.AddView(songs_count);

                        playlist_ln_main.AddView(ln_in);

                        Console.WriteLine("pl name: " + playlist);
                    });
                   

                    playlists_scroll.AddView(playlist_ln_main);
                    main_rel_l.AddView(playlists_scroll);


                    break;
                case 2.1f: // playlist songs
                    main_rel_l.RemoveAllViews();

                    Toast.MakeText(this, path_for_01 + " opend", ToastLength.Short).Show();

                    where_are_you_are_you_are_you_are_you_are_you_are_.album = "all";

                    ScrollView in_playlist_scroll = new ScrollView(this);
                    RelativeLayout.LayoutParams in_playlist_scroll_params = new RelativeLayout.LayoutParams(
                        RelativeLayout.LayoutParams.MatchParent,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    in_playlist_scroll_params.SetMargins(0, scroll_view_height, 0, 0);
                    in_playlist_scroll_params.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    in_playlist_scroll.LayoutParameters = in_playlist_scroll_params;


                    LinearLayout in_playlist_ln_main = new LinearLayout(this);
                    in_playlist_ln_main.Orientation = Orientation.Vertical;
                    RelativeLayout.LayoutParams in_playlist_ln_main_params = new RelativeLayout.LayoutParams(
                            RelativeLayout.LayoutParams.MatchParent,
                            RelativeLayout.LayoutParams.MatchParent
                    );
                    in_playlist_ln_main_params.SetMargins(20, 20, 20, 20);
                    in_playlist_ln_main.LayoutParameters = in_playlist_ln_main_params;

                    int[] in_playlist_button_margins = { 50, 50, 50, 50 };
                    int[] in_playlist_name_margins = { 50, 50, 50, 50 };
                    int[] in_playlist_card_margins = { 0, 50, 0, 0 };


                    List<string> plyalist_songs = FileManager.GetPlaylist(path_for_01);

                    for (int i = 0; i < plyalist_songs.Count; i++)
                    {

                        if (FileManager.GetSongTitle(plyalist_songs[i]) != "cant get title")
                        {
                            LinearLayout ln_in = pupulate_songs(
                                plyalist_songs[i], scale, false,
                                150, 100,
                                in_playlist_button_margins, in_playlist_name_margins, in_playlist_card_margins,
                                17,
                                "song", i, in_playlist_ln_main
                            );
                            song_tiles_image_set(
                                ln_in, plyalist_songs[i], scale, 150, 100, 
                                in_playlist_button_margins, "song", 17, 
                                in_playlist_name_margins
                            );
                            in_playlist_ln_main.AddView(ln_in);
                        }
                        else
                        {
                            FileManager.DeletePlaylist(path_for_01, plyalist_songs[i]);
                            Console.WriteLine("deleted ddded");
                            populate_grid(2.0f);
                        }
                    }
                   

                    in_playlist_scroll.AddView(in_playlist_ln_main);
                    main_rel_l.AddView(in_playlist_scroll);

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
                if (in_author.is_auth)
                    add_alias_popup(FileManager.GetNameFromPath(in_author.auth));

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