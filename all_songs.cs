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
using Android.Util;


namespace Ass_Pain
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AllSongs : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        DrawerLayout drawer;

        Dictionary<LinearLayout, object> albumButtons = new Dictionary<LinearLayout, object>();
        Dictionary<LinearLayout, int> songButtons = new Dictionary<LinearLayout, int>();

        (string location, object album) whereAreYouAreYouAreYouAreYouAreYouAre = ("", "");
        (bool is_auth, string auth) inAuthor = (false, "");

        List<string> selectedPlaylists = new List<string>();

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
            if (Intent != null)
            {
                int intentAuthor = Intent.GetIntExtra("link_author", 0);
                if (intentAuthor != 0)
                    populate_grid(0.2f, MainActivity.stateHandler.Artists.First(a => a.GetHashCode() == intentAuthor));
                string action = Intent.GetStringExtra("action");
                if (action == "openDrawer")
                    drawer.OpenDrawer(GravityCompat.Start);
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
            Button allSongs = FindViewById<Button>(Resource.Id.all_songs);
            Button playlists = FindViewById<Button>(Resource.Id.playlists);

            author.Click += (sender, e) =>
            {
                populate_grid(0.0f);
                inAuthor.is_auth = false;
                inAuthor.auth = "";
            };
            allSongs.Click += (sender, e) =>
            {
                populate_grid(1.0f);
                inAuthor.is_auth = false;
                inAuthor.auth = "";

            };
            playlists.Click += (sender, e) =>
            {
                populate_grid(2.0f);
                inAuthor.is_auth = false;
                inAuthor.auth = "";

            };


            FloatingActionButton createPlaylist = FindViewById<FloatingActionButton>(Resource.Id.fab);
            createPlaylist.Background.SetColorFilter(
                Color.Rgb(255, 76, 41),
                PorterDuff.Mode.Multiply
            );

            createPlaylist.Click += new EventHandler(show_popup);

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

        
        void song_tiles_image_set(LinearLayout parent, MusicBaseClass obj, float scale, int ww, int hh, int[] btnMargins, int nameSize, int[] nameMargins)
        {
            ImageView mori = new ImageView(this);
            LinearLayout.LayoutParams ll = new LinearLayout.LayoutParams(
                (int)(ww * scale + 0.5f), (int)(hh * scale + 0.5f)
            );
            ll.SetMargins(btnMargins[0], btnMargins[1], btnMargins[2], btnMargins[3]);
            mori.LayoutParameters = ll;

            Bitmap image = null;
            TagLib.File tagFile;

            if (!(obj is Album || obj is Artist || obj is Song))
            {
                return;
            }
            
            
            mori.SetImageBitmap(
                obj.Image
            );
            

            parent.AddView(mori);

            //アルブムの名前
            TextView name = new TextView(this);
            name.Text = obj.Title;
            name.TextSize = nameSize;
            name.SetTextColor(Color.White);
            name.TextAlignment = TextAlignment.Center;
            name.SetForegroundGravity(GravityFlags.Center);

            LinearLayout.LayoutParams lnNameParams = new LinearLayout.LayoutParams(
              (int)(130 * scale + 0.5f),
              LinearLayout.LayoutParams.WrapContent
            );
            lnNameParams.SetMargins(nameMargins[0], nameMargins[1], nameMargins[2], nameMargins[3]);

            name.LayoutParameters = lnNameParams;

            parent.SetGravity(GravityFlags.Center);
            parent.SetHorizontalGravity(GravityFlags.Center);
            parent.AddView(name);
            

        }

        LinearLayout pupulate_songs(
            MusicBaseClass musics, float scale, bool ort, int ww, int hh, int[] btnMargins, int[] nameMargins, int[] cardMargins, int nameSize, int index,
            LinearLayout linForDelete = null
        )
        {
            //リネアルレーアート作る
            LinearLayout lnIn = new LinearLayout(this);
            if (ort)
            {
                lnIn.Orientation = Orientation.Vertical;
            }
            else
            {
                lnIn.Orientation = Orientation.Horizontal;
            }
            lnIn.SetBackgroundResource(Resource.Drawable.rounded);

            LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent,
                LinearLayout.LayoutParams.WrapContent
            );
            lnInParams.SetMargins(cardMargins[0], cardMargins[1], cardMargins[2], cardMargins[3]);
            lnIn.LayoutParameters = lnInParams;


            // ボッタン作って
            int w = (int)(ww * scale + 0.5f);
            int h = (int)(hh * scale + 0.5f);



            if (musics is Album album)
            {
                lnIn.Click += new EventHandler(album_button_clicked);

                lnIn.LongClick += (sender, e) =>
                {
                    show_popup_song_edit(sender, e, album, linForDelete, lnIn);
                };

                albumButtons.Add(lnIn, album);
            }
            else if (musics is Artist artist)
            {
                lnIn.Click += new EventHandler(author_button_clicked);

                lnIn.LongClick += (sender, e) =>
                {
                    show_popup_song_edit(sender, e, artist, linForDelete, lnIn);
                };

                albumButtons.Add(lnIn, artist);
            }
            else if (musics is Song song)
            {
                lnIn.Click += delegate(object sender, EventArgs args)
                {
                    Console.WriteLine("song clicked");
                    songs_button_clicked(sender, args);
                };
                
                lnIn.LongClick += (sender, e) =>
                {
                    show_popup_song_edit(sender, e, song, linForDelete, lnIn);
                };

                songButtons.Add(lnIn, index);
            }

            lnIn.SetHorizontalGravity(GravityFlags.Center);
            return lnIn;
        }


        public LinearLayout album_tiles(float scale)
        {

            LinearLayout lin = new LinearLayout(this);
            lin.Orientation = Orientation.Vertical;
            LinearLayout.LayoutParams linParams = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.WrapContent,
                    LinearLayout.LayoutParams.WrapContent
                );
            lin.LayoutParameters = linParams;


            int[] buttonMargins = { 50, 50, 50, 0 };
            int[] nameMargins = { 50, 50, 50, 50 };
            int[] cardMargins = { 40, 50, 0, 0 };

            for (int i = 0; i < MainActivity.stateHandler.Albums.Count; i++)
            {
                
                LinearLayout lnIn = pupulate_songs(MainActivity.stateHandler.Albums[i], scale, true, 130, 160, buttonMargins, nameMargins, cardMargins, 15, i);
                song_tiles_image_set(
                    lnIn, MainActivity.stateHandler.Albums[i], scale, 150, 100, 
                    buttonMargins, 15, 
                    nameMargins
                );

                //全部加える
                lin.AddView(lnIn);
            }
            

            return lin;
        }

       
       
        public LinearLayout author_tiles(float scale)
        {
            LinearLayout lin = new LinearLayout(this);
            lin.Orientation = Orientation.Vertical;
            LinearLayout.LayoutParams linParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WrapContent,
                LinearLayout.LayoutParams.WrapContent
            );
            lin.LayoutParameters = linParams;
            
            int[] buttonMargins = { 50, 50, 50, 0 };
            int[] nameMargins = { 50, 50, 50, 50 };
            int[] cardMargins = { 50, 50, 0, 0 };


            for (int i = 0; i < MainActivity.stateHandler.Artists.Count; i++)
            {
                LinearLayout lnIn = pupulate_songs( MainActivity.stateHandler.Artists[i], scale, true, 130, 160, buttonMargins, nameMargins, cardMargins, 15, i);
                song_tiles_image_set(
                    lnIn, MainActivity.stateHandler.Artists[i], scale, 150, 100,
                    buttonMargins, 17,
                    nameMargins
                );
                //全部加える
                lin.AddView(lnIn);

            } 

            return lin;
        }

        

        public void songs_button_clicked(Object sender, EventArgs e)
        {
            LinearLayout pressedButtoon = (LinearLayout)sender;
            foreach (KeyValuePair<LinearLayout, int> pr in songButtons)
            {
                if (pr.Key == pressedButtoon)
                {
                    if (whereAreYouAreYouAreYouAreYouAreYouAre.album is "all")
                    {
                        MainActivity.ServiceConnection?.Binder?.Service?.GenerateQueue(MainActivity.stateHandler.Songs, pr.Value);
                    }
                    else
                    {
                        if (whereAreYouAreYouAreYouAreYouAreYouAre.album is Album album)
                            MainActivity.ServiceConnection?.Binder?.Service?.GenerateQueue(album, pr.Value);
                    }
                    break;
                }
            }
        }

        public void album_button_clicked(Object sender, EventArgs e)
        {
            LinearLayout pressedButtoon = (LinearLayout)sender;

            foreach(KeyValuePair<LinearLayout, object> pr in albumButtons)
            {
                if (pr.Key == pressedButtoon && pr.Value is Album album)
                {
                    whereAreYouAreYouAreYouAreYouAreYouAre.album = album;
                    populate_grid(0.1f, album);
                    inAuthor.is_auth = false;
                    inAuthor.auth = "";

                    break;
                }
            }
        }

        public void author_button_clicked(Object sender, EventArgs e)
        {
            LinearLayout pressedButtoon = (LinearLayout)sender;

            foreach (KeyValuePair<LinearLayout, object> pr in albumButtons)
            {
                if (pr.Key == pressedButtoon && pr.Value is Artist artist)
                {
                    whereAreYouAreYouAreYouAreYouAreYouAre.album = artist;
                    populate_grid(0.2f, artist);
                    inAuthor.is_auth = true;
                    inAuthor.auth = artist.Title;

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

            EditText userData = view.FindViewById<EditText>(Resource.Id.editText);
            alert.SetCancelable(false)
                ?.SetPositiveButton("submit", delegate
            {
                FileManager.CreatePlaylist(userData.Text);
                Toast.MakeText(
                    this, userData.Text + " Created successfully",
                    ToastLength.Short
                )
                    ?.Show();
            })
                ?.SetNegativeButton("cancel", delegate
                {
                    alert.Dispose();
                });


            Android.App.AlertDialog dialog = alert.Create();
            dialog.Show();
        }



        public void add_alias_popup(string authorN)
        {
            Console.WriteLine("popup clicked");

            LayoutInflater ifl = LayoutInflater.From(this);
            View view = ifl.Inflate(Resource.Layout.add_alias_popup, null);
            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetView(view);

            Android.App.AlertDialog dialog = alert.Create();

            TextView author = view.FindViewById<TextView>(Resource.Id.author_name);
            author.Text = authorN;

            EditText userInput = view.FindViewById<EditText>(Resource.Id.user_author);
            Button sub = view.FindViewById<Button>(Resource.Id.submit_alias);
            sub.Click += delegate
            {
                FileManager.AddAlias(authorN, userInput.Text);


                dialog.Hide();
            };

            Button cancel = view.FindViewById<Button>(Resource.Id.cancel_alias);
            cancel.Click += delegate
            {
                dialog.Hide();
            };

            
            dialog.Show();
        }

        public void list_playlists_popup(object sender, EventArgs e, Song song)
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
                LinearLayout lnIn = new LinearLayout(this);
                lnIn.Orientation = Orientation.Vertical;
                lnIn.SetBackgroundResource(Resource.Drawable.rounded_light);

                LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.MatchParent,
                    (int)(50 * scale + 0.5f)
                );
                lnInParams.SetMargins(20, 20, 20, 20);
                lnIn.LayoutParameters = lnInParams;
                lnIn.SetGravity(GravityFlags.Center);

                lnIn.Click += (sender, e) =>
                {
                    Console.WriteLine(song);
                    if (selectedPlaylists.Contains(p))
                    {
                        selectedPlaylists.Remove(p);
                        lnIn.SetBackgroundResource(Resource.Drawable.rounded_light);
                        Console.WriteLine("removed : " + p);
                    }
                    else
                    {
                        selectedPlaylists.Add(p);
                        Console.WriteLine("added: " + p);
                        lnIn.SetBackgroundResource(Resource.Drawable.rounded_dark);

                    }
                };

                // ---
                TextView name = new TextView(this);
                name.TextAlignment = TextAlignment.Center;
                name.SetTextColor(Color.White);
                name.Text = p;
                lnIn.AddView(name);


                ln.AddView(lnIn);
            }


            // ----
            Button submit = view.FindViewById<Button>(Resource.Id.submit_plas);
            submit.Background.SetColorFilter(
                Color.Rgb(255, 76, 41),
                PorterDuff.Mode.Multiply
            ); submit.SetTextColor(Color.Black);

            submit.Click += (sender, e) =>
            {
                foreach (string s in selectedPlaylists)
                {
                    Console.WriteLine(s + " " + selectedPlaylists.Count);

                    List<Song> plaSongs = FileManager.GetPlaylist((string)s);
                    if (plaSongs.Any(a => a.Equals(song)))
                        Toast.MakeText(this, "already exists in : " + s, ToastLength.Short)?.Show();
                    else
                    {
                        FileManager.AddToPlaylist(s, song.Path);

                        Toast.MakeText(
                            this, "added successfully",
                            ToastLength.Short
                        )
                            ?.Show();
                    }

                }


                dialog.Hide();
                selectedPlaylists.Clear();

                
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

        public void are_you_sure(object sender, EventArgs e, Song song, Android.App.AlertDialog di, LinearLayout linFromDelete, LinearLayout linForDelete)
        {
            Console.WriteLine("popup clicked");


            LayoutInflater ifl = LayoutInflater.From(this);
            View view = ifl.Inflate(Resource.Layout.are_you_sure_popup, null);
            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetView(view);

            Android.App.AlertDialog dialog = alert.Create();

            TextView txt = view.FindViewById<TextView>(Resource.Id.are_you_sure_text);
            txt.SetTextColor(Color.White);
            txt.Text = "Deleting: " + song.Title;

            Button yes = view.FindViewById<Button>(Resource.Id.yes_daddy);
            yes.Background.SetColorFilter(
                Color.Rgb(255, 76, 41),
                PorterDuff.Mode.Multiply
            ); yes.SetTextColor(Color.Black);

            yes.Click += delegate
            {
                song.Delete();
                dialog.Hide();
                deleting_song(sender, e, di);

                if (song is Song)
                    linFromDelete.RemoveView(linForDelete);
                else
                {
                    populate_grid(0.0f);
                    inAuthor.is_auth = false;
                    inAuthor.auth = "";
                }
                    

                Toast.MakeText(this, $"{song.Title} has been deleted", ToastLength.Short).Show();
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

        public void show_popup_song_edit(object sender, EventArgs e, MusicBaseClass path, LinearLayout linFromDelet, LinearLayout linForDelete)
        {
            Console.WriteLine("popup clicked");

            LayoutInflater ifl = LayoutInflater.From(this);
            View view;
            if (path is Song)
                view = ifl.Inflate(Resource.Layout.edit_song_popup, null);
            else
                view = ifl.Inflate(Resource.Layout.edit_album_popup, null);

            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
            alert.SetView(view);

            Android.App.AlertDialog dialog = alert.Create();

            /*
             * popup buttons start
             */
            if (path is Song song)
            {
                Button addToPla = view.FindViewById<Button>(Resource.Id.add_to_pla);
                addToPla.Background.SetColorFilter(
                    Color.Rgb(255, 76, 41),
                    PorterDuff.Mode.Multiply
                ); addToPla.SetTextColor(Color.Black);
                addToPla.Click += (sender, e) =>
                {
                    dialog.Hide();
                    list_playlists_popup(sender, e, song);
                    Console.WriteLine(path);

                };
                
                Button addToQu = view.FindViewById<Button>(Resource.Id.add_to_qu);
                Button delete = view.FindViewById<Button>(Resource.Id.delete);
                addToQu.Background.SetColorFilter(
                    Color.Rgb(255, 76, 41),
                    PorterDuff.Mode.Multiply
                ); addToQu.SetTextColor(Color.Black);
                delete.Background.SetColorFilter(
                    Color.Rgb(255, 76, 41),
                    PorterDuff.Mode.Multiply
                ); delete.SetTextColor(Color.Black);

                // handle clicked
                addToQu.Click += (sender, e) =>
                {
                    MainActivity.ServiceConnection?.Binder?.Service?.AddToQueue(song);
                };
                delete.Click += (sender, e) =>
                {
                    are_you_sure(sender, e, song, dialog, linFromDelet, linForDelete);
                };
            }




            /*
             * popup buttons end
             */

            
            dialog.Show();
        }

        public void populate_grid(float type, object obj = null, bool clear = true, int scrollViewHeight = 150)
        {
            float scale = Resources.DisplayMetrics.Density;
            RelativeLayout mainRelL = FindViewById<RelativeLayout>(Resource.Id.content);
           

            ScrollView authorScroll = new ScrollView(this);
            ScrollView albumScroll = new ScrollView(this);

            switch (type)
            {
                case 0.0f: // 作家

                    mainRelL.RemoveAllViews();

                    whereAreYouAreYouAreYouAreYouAreYouAre.location = "main";
                    whereAreYouAreYouAreYouAreYouAreYouAre.album = "";

                    DisplayMetrics displayMetrics = Resources.DisplayMetrics;
                    int displayWidth = displayMetrics.WidthPixels;

                    //作家
                    // ScrollView author_scroll = new ScrollView(this);
                    RelativeLayout.LayoutParams authorScrollParams = new RelativeLayout.LayoutParams(
                        displayWidth / 2,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    authorScrollParams.SetMargins(0, 150, 0, 0);
                    authorScrollParams.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    authorScroll.LayoutParameters = authorScrollParams;

                    LinearLayout authorLin = author_tiles(scale);
                    authorScroll.AddView(authorLin);
                    mainRelL.AddView(authorScroll);



                    //アルブム
                    // ScrollView album_scroll = new ScrollView(this);
                    RelativeLayout.LayoutParams albumScrollParams = new RelativeLayout.LayoutParams(
                        displayWidth / 2,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    albumScrollParams.SetMargins(displayWidth / 2, 150, 0, 0);
                    albumScrollParams.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    albumScroll.LayoutParameters = albumScrollParams;
                    
                    LinearLayout albumLin = album_tiles(scale);
                    albumScroll.AddView(albumLin);
                    mainRelL.AddView(albumScroll);

                   

                    break;
                case 0.1f: // songs from album
                    if (clear) mainRelL.RemoveAllViews();

                    ScrollView songsScroll = new ScrollView(this);
                    RelativeLayout.LayoutParams songsScrollParams = new RelativeLayout.LayoutParams(
                        RelativeLayout.LayoutParams.MatchParent,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    songsScrollParams.SetMargins(0, scrollViewHeight, 0, 0);
                    songsScrollParams.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    songsScroll.LayoutParameters = songsScrollParams;
                    

                    LinearLayout lnMain = new LinearLayout(this);
                    lnMain.Orientation = Orientation.Vertical;
                    RelativeLayout.LayoutParams lnMainParams = new RelativeLayout.LayoutParams(
                            RelativeLayout.LayoutParams.MatchParent,
                            RelativeLayout.LayoutParams.MatchParent
                    );
                    lnMainParams.SetMargins(20, 20, 20, 20);
                    lnMain.LayoutParameters = lnMainParams;

                    int[] buttonMargins = { 50, 50, 50, 50 };
                    int[] nameMargins = { 50, 50, 50, 50 };
                    int[] cardMargins = { 0, 50, 0, 0 };

                    if (obj is Album album)
                    {
                        for (int i = 0; i < album.Songs.Count; i++)
                        {

                            LinearLayout lnIn = pupulate_songs(
                                album.Songs[i], scale, false,
                                150, 100,
                                buttonMargins, nameMargins, cardMargins,
                                17,
                                i, lnMain
                            );
                            song_tiles_image_set(
                                lnIn, album.Songs[i], scale, 150, 100,
                                buttonMargins, 17,
                                nameMargins
                            );
                            lnMain.AddView(lnIn);
                        }
                        
                    }
                    else
                    {
                        Console.WriteLine("bad path, ln; 845");
                    }

                    songsScroll.AddView(lnMain);
                    mainRelL.AddView(songsScroll);

                    break;
                case 0.2f: // categorized album (by author)

                    mainRelL.RemoveAllViews();

                    HorizontalScrollView hr = new HorizontalScrollView(this);
                    RelativeLayout.LayoutParams hrParams = new RelativeLayout.LayoutParams(
                        RelativeLayout.LayoutParams.MatchParent,
                        (int)(240 * scale + 0.5f)
                    );
                    hrParams.SetMargins(0, 150, 0, 0);
                    hrParams.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    hr.LayoutParameters = hrParams;

                    LinearLayout lin = new LinearLayout(this);

                    if (obj is Artist artist)
                    {
                        for (int i = 0; i < artist.Albums.Count; i++)
                        {
                            //リネアルレーアート作る
                            LinearLayout lnIn = new LinearLayout(this);
                            lnIn.Orientation = Orientation.Vertical;
                            lnIn.SetBackgroundResource(Resource.Drawable.rounded);

                            LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                                LinearLayout.LayoutParams.MatchParent,
                                LinearLayout.LayoutParams.WrapContent
                            );
                            lnInParams.SetMargins(50, 50, 0, 0);
                            lnIn.LayoutParameters = lnInParams;



                            // ボッタン作って
                            int w = (int)(150 * scale + 0.5f);
                            int h = (int)(180 * scale + 0.5f);


                            ImageView mori = new ImageView(this);
                            LinearLayout.LayoutParams ll = new LinearLayout.LayoutParams(
                                w, h
                            );
                            ll.SetMargins(50, 50, 50, 0);
                            mori.LayoutParameters = ll;

                            

                            // IRyS Ch. hololive-EN
                            Console.WriteLine("test");
                            mori.SetImageBitmap(
                                artist.Albums[i].Image
                            );
                            lnIn.Click += new EventHandler(album_button_clicked);
                            albumButtons.Add(lnIn, artist.Albums[i]);

                            lnIn.AddView(mori);



                            //アルブムの名前
                            int hName = (int)(40 * scale + 0.5f);

                            TextView name = new TextView(this);
                            name.Text = artist.Albums[i].Title;
                            name.TextSize = 15;
                            name.SetTextColor(Color.White);
                            name.TextAlignment = TextAlignment.Center;

                            LinearLayout.LayoutParams lnNameParams = new LinearLayout.LayoutParams(
                              w,
                              hName
                            );
                            lnNameParams.SetMargins(50, 0, 50, 50);
                            name.LayoutParameters = lnNameParams;

                            lnIn.AddView(name);



                            //全部加える
                            lin.AddView(lnIn);
                        }

                       

                        hr.AddView(lin);
                        mainRelL.AddView(hr);

                        populate_grid(0.1f, artist.Songs, false, (int)(300 * scale + 0.5f));
                    }
                    

                    break;
                case 1.0f: // all

                    mainRelL.RemoveAllViews();

                    whereAreYouAreYouAreYouAreYouAreYouAre.location = "all";
                    whereAreYouAreYouAreYouAreYouAreYouAre.album = "all";

                    ScrollView allSongsScroll = new ScrollView(this);
                    RelativeLayout.LayoutParams allSongsScrollParams = new RelativeLayout.LayoutParams(
                        RelativeLayout.LayoutParams.MatchParent,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    allSongsScrollParams.SetMargins(0, scrollViewHeight, 0, 0);
                    allSongsScrollParams.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    allSongsScroll.LayoutParameters = allSongsScrollParams;


                    LinearLayout allSongsLnMain = new LinearLayout(this);
                    allSongsLnMain.Orientation = Orientation.Vertical;
                    RelativeLayout.LayoutParams allSongsLnMainParams = new RelativeLayout.LayoutParams(
                            RelativeLayout.LayoutParams.MatchParent,
                            RelativeLayout.LayoutParams.MatchParent
                    );
                    allSongsLnMainParams.SetMargins(20, 20, 20, 20);
                    allSongsLnMain.LayoutParameters = allSongsLnMainParams;


                    int[] allSongsButtonMargins = { 50, 50, 50, 50 };
                    int[] allSongsNameMargins = { 50, 50, 50, 50 };
                    int[] allSongsCardMargins = { 0, 50, 0, 0 };


                    List<Tuple<LinearLayout, int>> lazyBuffer = new List<Tuple<LinearLayout, int>>();
                    
                    for (int i = 0; i < MainActivity.stateHandler.Songs.Count; i++)
                    {

                        LinearLayout lnIn = pupulate_songs(
                            MainActivity.stateHandler.Songs[i], scale, false,
                            150, 100,
                            allSongsButtonMargins, allSongsNameMargins, allSongsCardMargins,
                            17, i, allSongsLnMain
                        );
                        
                        lazyBuffer.Add(new Tuple<LinearLayout, int>(lnIn, i));

                    }

                    for (int i = 0; i < Math.Min(5, lazyBuffer.Count); i++)
                    {
                        song_tiles_image_set(lazyBuffer[i].Item1, MainActivity.stateHandler.Songs[lazyBuffer[i].Item2], scale, 150, 100, allSongsButtonMargins, 15, allSongsNameMargins);
                        allSongsLnMain.AddView(lazyBuffer[i].Item1);
                    }
                   
                    lazyBuffer.RemoveRange(0, Math.Min(5, lazyBuffer.Count));

                    allSongsScroll.ScrollChange += (sender, e) =>
                    {
                        View view = allSongsLnMain.GetChildAt(allSongsLnMain.ChildCount - 1);
                        int topDetect = allSongsScroll.ScrollY;
                        int bottomDetect = view.Bottom - (allSongsScroll.Height + allSongsScroll.ScrollY);

                        if (bottomDetect == 0 && lazyBuffer.Count != 0)
                        {
                            Console.WriteLine("loading new");

                            for (int i = 0; i < Math.Min(5, lazyBuffer.Count); i++)
                            {
                                song_tiles_image_set(lazyBuffer[i].Item1, MainActivity.stateHandler.Songs[lazyBuffer[i].Item2], scale, 150, 100, allSongsButtonMargins, 17, allSongsNameMargins);
                                allSongsLnMain.AddView(lazyBuffer[i].Item1);
                            }

                            lazyBuffer.RemoveRange(0, Math.Min(5, lazyBuffer.Count));
                        }
                    };


                    allSongsScroll.AddView(allSongsLnMain);
                    mainRelL.AddView(allSongsScroll);


                    break;


                case 2.0f: // playlists
                    mainRelL.RemoveAllViews();

                    ScrollView playlistsScroll = new ScrollView(this);
                    RelativeLayout.LayoutParams playlistScrollParams = new RelativeLayout.LayoutParams(
                        RelativeLayout.LayoutParams.MatchParent,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    playlistScrollParams.SetMargins(0, scrollViewHeight, 0, 0);
                    playlistScrollParams.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    playlistsScroll.LayoutParameters = playlistScrollParams;


                    LinearLayout playlistLnMain = new LinearLayout(this);
                    playlistLnMain.Orientation = Orientation.Vertical;
                    RelativeLayout.LayoutParams playlistLnMainParams = new RelativeLayout.LayoutParams(
                            RelativeLayout.LayoutParams.MatchParent,
                            RelativeLayout.LayoutParams.MatchParent
                    );
                    playlistLnMainParams.SetMargins(20, 20, 20, 20);
                    playlistLnMain.LayoutParameters = playlistLnMainParams;

                    int[] playlistButtonMargins = { 50, 50, 50, 50 };
                    int[] playlistCardMargins = { 0, 50, 0, 0 };


                    List<string> playlists = FileManager.GetPlaylist();
                    Parallel.ForEach(playlists, playlist =>
                    {

                        LinearLayout lnIn = new LinearLayout(this);
                        lnIn.Orientation = Orientation.Vertical;
                        lnIn.SetBackgroundResource(Resource.Drawable.rounded);

                        LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MatchParent,
                            LinearLayout.LayoutParams.WrapContent
                        );
                        lnInParams.SetMargins(
                            playlistCardMargins[0], playlistCardMargins[1], 
                            playlistCardMargins[2], playlistCardMargins[3]
                        );
                        lnIn.LayoutParameters = lnInParams;


                        TextView plaName = new TextView(this);
                        LinearLayout.LayoutParams nameParams = new LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MatchParent,
                            LinearLayout.LayoutParams.WrapContent
                        );
                        nameParams.SetMargins(0, 20, 0, 20);
                        plaName.LayoutParameters = nameParams; 
                        plaName.Text = playlist;
                        plaName.SetTextColor(Color.White);
                        plaName.TextSize = 30;
                        plaName.TextAlignment = TextAlignment.Center;

                        TextView songsCount = new TextView(this);
                        LinearLayout.LayoutParams countParams = new LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MatchParent,
                            LinearLayout.LayoutParams.WrapContent
                        );
                        countParams.SetMargins(0, 20, 0, 20);
                        songsCount.LayoutParameters = countParams;
                        songsCount.Text = $"number of songs: {FileManager.GetPlaylist(playlist).Count}";
                        songsCount.SetTextColor(Color.White);
                        songsCount.TextSize = 15;
                        songsCount.TextAlignment = TextAlignment.Center;


                        lnIn.Click += (sender, e) =>
                        {
                            populate_grid(2.1f, playlist);
                            //Toast.MakeText(this, playlist + " opend", ToastLength.Short).Show();
                        };

                        lnIn.AddView(plaName);
                        lnIn.AddView(songsCount);

                        playlistLnMain.AddView(lnIn);

                        Console.WriteLine("pl name: " + playlist);
                    });
                   

                    playlistsScroll.AddView(playlistLnMain);
                    mainRelL.AddView(playlistsScroll);


                    break;
                case 2.1f: // playlist songs
                    mainRelL.RemoveAllViews();

                    // Toast.MakeText(this, path_for_01 + " opend", ToastLength.Short).Show();

                    whereAreYouAreYouAreYouAreYouAreYouAre.album = "all";

                    ScrollView inPlaylistScroll = new ScrollView(this);
                    RelativeLayout.LayoutParams inPlaylistScrollParams = new RelativeLayout.LayoutParams(
                        RelativeLayout.LayoutParams.MatchParent,
                        RelativeLayout.LayoutParams.MatchParent
                    );
                    inPlaylistScrollParams.SetMargins(0, scrollViewHeight, 0, 0);
                    inPlaylistScrollParams.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
                    inPlaylistScroll.LayoutParameters = inPlaylistScrollParams;
                    
                    LinearLayout inPlaylistLnMain = new LinearLayout(this);
                    inPlaylistLnMain.Orientation = Orientation.Vertical;
                    RelativeLayout.LayoutParams inPlaylistLnMainParams = new RelativeLayout.LayoutParams(
                            RelativeLayout.LayoutParams.MatchParent,
                            RelativeLayout.LayoutParams.MatchParent
                    );
                    inPlaylistLnMainParams.SetMargins(20, 20, 20, 20);
                    inPlaylistLnMain.LayoutParameters = inPlaylistLnMainParams;

                    int[] inPlaylistButtonMargins = { 50, 50, 50, 50 };
                    int[] inPlaylistNameMargins = { 50, 50, 50, 50 };
                    int[] inPlaylistCardMargins = { 0, 50, 0, 0 };


                    List<Song> plyalistSongs = FileManager.GetPlaylist((string)obj);

                    for (int i = 0; i < plyalistSongs.Count; i++)
                    {
                        LinearLayout lnIn = pupulate_songs(
                            plyalistSongs[i], scale, false,
                            150, 100,
                            inPlaylistButtonMargins, inPlaylistNameMargins, inPlaylistCardMargins,
                            17,
                            i, inPlaylistLnMain
                        );
                        song_tiles_image_set(
                            lnIn, plyalistSongs[i], scale, 150, 100, 
                            inPlaylistButtonMargins, 17, 
                            inPlaylistNameMargins
                        );
                        inPlaylistLnMain.AddView(lnIn);
                    }
                   

                    inPlaylistScroll.AddView(inPlaylistLnMain);
                    mainRelL.AddView(inPlaylistScroll);

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