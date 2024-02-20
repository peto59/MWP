using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using MWP.BackEnd;
using Xamarin.Essentials;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;
using FragmentTransaction = AndroidX.Fragment.App.FragmentTransaction;
using Orientation = Android.Widget.Orientation;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP
{
    /// <summary>
    /// Abstraktná trieda obsahujúca základne metódy na vkreslovanie dynamického
    /// užívateľského rozhrania. Vznikla z dôvodu nevytvárania duplicitného kódu, keďže v aplikácii sa
    /// opakuj štýli a elementy generované dynamicky.
    /// </summary>
    public abstract class UiRenderFunctions
    {
        private static List<string> _selectedPlaylists = new List<string>();

        /// <summary>
        /// Pridelenie rozdielnych metód na základe typu skladby s ktorým je manipulované.
        /// </summary>
        public enum SongMediaType
        {
            /// <summary>
            /// skladba sa nachádza v albume (fargment Albums).
            /// </summary>
            AlbumSong,
            /// <summary>
            /// skladba sa nachádza vo všeobecnom prehľade všetkých skladieb (fagrment Songs)
            /// </summary>
            AllSong,
            /// <summary>
            /// skladba sa nachádza v playliste (fragment Playlist)
            /// </summary>
            PlaylistSong
        }

        /// <summary>
        /// General object, that will take on different forms, based on where its currently being defined.
        /// E.g. if album fragment is currently active, than the object of that active album you are browsing will be
        /// used to define this global position parameter
        /// </summary>
        public static object FragmentPositionObject;
        
        /// <summary>
        /// Shows dialog for user to accept or reject incoming songs from <paramref name="hostname"/>
        /// </summary>
        /// <param name="songs">List of songs to be received</param>
        /// <param name="hostname">Device that wants to send <paramref name="songs"/></param>
        /// <param name="context">App context</param>
        /// <param name="accept"><see cref="Action{T}"/> on accept</param>
        /// <param name="reject"><see cref="Action{T}"/> on Reject</param>
        public static void ListIncomingSongsPopup(List<Song> songs, string hostname, Context context, Action accept, Action reject)
        {
            int count = songs.Count;
            Typeface? font = Typeface.CreateFromAsset(context.Assets, "sulphur.ttf");
            
            LayoutInflater? ifl = LayoutInflater.From(context);
            View? view = ifl?.Inflate(Resource.Layout.accept_incoming_popup, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);

            AlertDialog? dialog = alert.Create();
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));

            TextView? title = view?.FindViewById<TextView>(Resource.Id.accept_incoming_title);
            if (title != null)
            {
                title.Typeface = font;
                title.Text = $"{hostname} wants to send you {count} songs";
            }

            LinearLayout? ln = view?.FindViewById<LinearLayout>(Resource.Id.accept_incoming_song_list);
            
            /*
             * Prechádzanie skladbami v liste a pre každú skladbu vytvorenie nového záznamu v liste v ScrollView.
             */
            for (int i = 0; i < count; i++)
            {
                /*
                 * Vytvaranie LinearLayout-u ktory bude obsahpvat text skladby
                 */
                LinearLayout lnIn = new LinearLayout(context);
                lnIn.Orientation = Orientation.Horizontal;
                lnIn.SetBackgroundResource(Resource.Drawable.rounded_light);

                LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                );
                lnInParams.SetMargins(20, 20, 20, 20);
                lnIn.LayoutParameters = lnInParams;
                lnIn.SetPadding(0, (int)ConvertDpToPixels(20, context), 0, (int)ConvertDpToPixels(20, context));
                lnIn.SetGravity(GravityFlags.Left);

                
                /*
                 * Vytváranie TextView komponentu pre názov skladby
                 */
                LinearLayout.LayoutParams nameAndArtistParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.MatchParent
                );
                LinearLayout nameAndArtist = new LinearLayout(context);
                nameAndArtist.LayoutParameters = nameAndArtistParams;
                nameAndArtist.Orientation = Orientation.Vertical;
                
                
                LinearLayout.LayoutParams nameParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent
                );
                TextView name = new TextView(context);
                name.LayoutParameters = nameParams;
                name.TextSize = (int)ConvertDpToPixels(8, context);
                name.Typeface = font;
                name.SetTextColor(Color.White);
                name.Text = songs[i].Title;
                nameAndArtist.AddView(name);
                
                LinearLayout.LayoutParams artistParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent
                );
                TextView artist = new TextView(context);
                artist.LayoutParameters = artistParams;
                artist.TextSize = (int)ConvertDpToPixels(5, context);
                artist.Typeface = font;
                artist.SetTextColor(Color.White);
                artist.Text = songs[i].Artist.Title;
                nameAndArtist.AddView(artist);
                
                
                
                LinearLayout.LayoutParams indexParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.MatchParent
                );
                TextView index = new TextView(context);
                indexParams.Gravity = GravityFlags.Center;
                indexParams.SetMargins(50, 0, 50, 0);
                index.Gravity = GravityFlags.Center;
                index.LayoutParameters = indexParams;
                index.TextSize = (int)ConvertDpToPixels(8, context);
                index.Typeface = font;
                index.SetTextColor(Color.White);
                index.Text = $"{i}";
                
                
                lnIn.AddView(index);
                lnIn.AddView(nameAndArtist);
                
                
                ln?.AddView(lnIn);
            }
            
            
            TextView? addNew = view?.FindViewById<TextView>(Resource.Id.accept_incoming_accept_songs);
            if (addNew != null)
            {
                addNew.Typeface = font;
                addNew.Click += (_, _) =>
                {
                    accept();
                    dialog?.Hide();
                };
            }

            TextView? cancel = view?.FindViewById<TextView>(Resource.Id.accept_incoming_reject_songs);
            if (cancel != null)
            {
                cancel.Typeface = font;
                cancel.Click += (_, _) =>
                {
#if DEBUG
                    MyConsole.WriteLine("User clicked on reject");
#endif
                    reject();
                    dialog?.Hide();
                };
            }


            dialog?.Show();
            
        }
        
        /// <summary>
        /// Premena DP jednotky na jodnutku pixelov. 
        /// </summary>
        /// <param name="dpValue">Hodnota v DP jednotkach.</param>
        /// <param name="context">Kontext hlavnej aktivity. Ziskat sa da porstrednictvom parametru ziskaneho z explictneho konstrkutora v akomokolvek fragmente.</param>
        /// <returns></returns>
        public static float ConvertDpToPixels(float dpValue, Context context) {
            if (context.Resources is not { DisplayMetrics: not null }) return 0.0f;
            var screenPixelDensity = context.Resources.DisplayMetrics.Density;
            var pixels = dpValue * screenPixelDensity;
            return pixels;

        } 
        
        
        
        private static void AreYouSure(
            object sender, EventArgs e, Song song, AlertDialog? di, 
            LinearLayout linFromDelete, LinearLayout? linForDelete, 
            Context context, SongsFragment songsFragmentContext, Typeface font)
        {
            LayoutInflater? ifl = LayoutInflater.From(context);
            View? view = ifl?.Inflate(Resource.Layout.share_are_you_sure, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);
            
            TextView? title = view?.FindViewById<TextView>(Resource.Id.share_are_you_sure_title);
            TextView? yes = view?.FindViewById<TextView>(Resource.Id.share_are_you_sure_yes);
            TextView? no = view?.FindViewById<TextView>(Resource.Id.share_are_you_sure_no);

            if (title != null) title.Typeface = font;
            if (yes != null) yes.Typeface = font;
            if (no != null) no.Typeface = font;


            AlertDialog? dialog = alert.Create();
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
            
            title?.SetTextColor(Color.White);
            if (title != null) title.Text = "Deleting: " + song.Title;
            
            yes?.SetTextColor(Color.White);

            if (yes != null)
                yes.Click += delegate
                {
                    song.Delete();
                    dialog?.Hide();
                    di?.Hide();
                    songsFragmentContext.InvalidateCache();

                    linFromDelete.RemoveView(linForDelete);
                    Toast.MakeText(context, $"{song.Title} has been deleted", ToastLength.Short)?.Show();
                };


            if (no != null)
                no.Click += (_, _) =>
                {
                    dialog?.Hide();
                    di?.Hide();
                };

            dialog?.Show();
        }

        private static void ListPlaylistsPopup(Song song, Context context, float scale, Typeface? font)
        {
            LayoutInflater? ifl = LayoutInflater.From(context);
            View? view = ifl?.Inflate(Resource.Layout.list_plas_popup, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);

            AlertDialog? dialog = alert.Create();

            LinearLayout? ln = view?.FindViewById<LinearLayout>(Resource.Id.playlists_list_la);

            List<string> playlists = FileManager.GetPlaylist();
            foreach (string p in playlists)
            {
                
                LinearLayout lnIn = new LinearLayout(context);
                lnIn.Orientation = Orientation.Vertical;
                lnIn.SetBackgroundResource(Resource.Drawable.rounded_light);

                LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    (int)(50 * scale + 0.5f)
                );
                lnInParams.SetMargins(20, 20, 20, 20);
                lnIn.LayoutParameters = lnInParams;
                lnIn.SetGravity(GravityFlags.Center);

                lnIn.Click += (_, _) =>
                {
                    if (_selectedPlaylists.Contains(p))
                    {
                        _selectedPlaylists.Remove(p);
                        lnIn.SetBackgroundResource(Resource.Drawable.rounded_light);
                    }
                    else
                    {
                        _selectedPlaylists.Add(p);
                        lnIn.SetBackgroundResource(Resource.Drawable.rounded_dark);
                    }
                };
                
                TextView name = new TextView(context);
                name.Typeface = font;
                name.TextAlignment = TextAlignment.Center;
                name.SetTextColor(Color.White);
                name.Text = p;
                lnIn.AddView(name);
                
                ln?.AddView(lnIn);
            }
            
            
            TextView? submit = view?.FindViewById<TextView>(Resource.Id.submit_plas);
            if (submit != null)
            {
                submit.Typeface = font;
                submit.Click += (_, _) =>
                {
                    foreach (string s in _selectedPlaylists)
                    {
                        List<Song> plaSongs = FileManager.GetPlaylist(s);
                        if (plaSongs.Any(a => a.Equals(song)))
                            Toast.MakeText(context, "already exists in : " + s, ToastLength.Short)?.Show();
                        else
                        {
                            FileManager.AddToPlaylist(s, song);

                            Toast.MakeText(
                                    context, "added successfully",
                                    ToastLength.Short
                                )
                                ?.Show();
                        }
                    }


                    dialog?.Hide();
                    _selectedPlaylists.Clear();
                };
            }

            TextView? cancel = view?.FindViewById<TextView>(Resource.Id.cancel_plas);
            if (cancel != null)
            {
                cancel.Typeface = font;
                cancel.Click += (_, _) => { dialog?.Hide(); };
            }


            dialog?.Show();
            
            
            
        }

        private static void ShowPopupSongEdit(
            MusicBaseClass path, LinearLayout linFromDelete, LinearLayout? linForDelete, Context context, float scale, 
            AssetManager? assets, FragmentManager manager, SongsFragment? songsFragmentContext = null
        )
        {
            LayoutInflater? ifl = LayoutInflater.From(context);
            View? view = ifl?.Inflate(path is Song ? Resource.Layout.edit_song_popup : Resource.Layout.edit_album_popup, null);

            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);
            
            Typeface? font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
            if (context.Resources is { DisplayMetrics: not null }) scale = context.Resources.DisplayMetrics.Density;

            AlertDialog? dialog = alert.Create();
            
            if (path is Song song)
            {
                TextView? addToPla = view?.FindViewById<TextView>(Resource.Id.add_to_pla);
                if (addToPla != null)
                {
                    addToPla.SetTextColor(Color.White);
                    addToPla.Typeface = font;
                    addToPla.Click += (_, _) =>
                    {
                        dialog?.Hide();
                        ListPlaylistsPopup(song, context, scale, font);
                    };
                }

                
                
                
                TextView? addToQu = view?.FindViewById<TextView>(Resource.Id.add_to_qu);
                TextView? delete = view?.FindViewById<TextView>(Resource.Id.delete);
                TextView? edit = view?.FindViewById<TextView>(Resource.Id.editSong_tags);
                if (addToQu != null && delete != null && edit != null)
                {
                    addToQu.Typeface = font;
                    delete.Typeface = font;
                    edit.Typeface = font;
                    

                    addToQu.Click += (_, _) =>
                    {
                        MainActivity.ServiceConnection?.Binder?.Service?.AddToQueue(song);
                        dialog?.Hide();
                    };

                    delete.Click += (o, args) =>
                    {
                        if (songsFragmentContext != null && font != null)
                        {
                            AreYouSure(o, args, song, dialog, linFromDelete, linForDelete, context,
                                    songsFragmentContext, font);
                            dialog?.Hide();
                        }
                    };

                    edit.Click += (_, _) =>
                    {
                        dialog?.Hide();

                            
                        TagManagerFragment tagFrag = new TagManagerFragment(context, assets, path);
                        FragmentTransaction fragmentTransaction = manager.BeginTransaction();
                        fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, tagFrag);
                        fragmentTransaction.AddToBackStack(null);
                        fragmentTransaction.Commit();
                    };
                }
            }
            
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
            dialog?.Show();
        }


        /// <summary>
        /// Horizontal order of elements within single element in list of songs or albums (only songs, bcs only albums have vertical alignment of cover and title)
        /// </summary>
        /// <param name="musics"></param>
        /// <param name="scale"></param>
        /// <param name="ww"></param>
        /// <param name="hh"></param>
        /// <param name="btnMargins"></param>
        /// <param name="nameMargins"></param>
        /// <param name="cardMargins"></param>
        /// <param name="nameSize"></param>
        /// <param name="index"></param>
        /// <param name="context"></param>
        /// <param name="songButtons"></param>
        /// <param name="songMediaType"></param>
        /// <param name="assets"></param>
        /// <param name="linForDelete"></param>
        /// <returns></returns>
        public static LinearLayout? PopulateHorizontal(
            MusicBaseClass musics, float scale, int ww, int hh, int[] btnMargins, int[] nameMargins, int[] cardMargins, int nameSize,
            Context context, Dictionary<LinearLayout?, Guid> songButtons, SongMediaType songMediaType, AssetManager? assets, FragmentManager manager,
            LinearLayout? linForDelete = null, SongsFragment? songsfragmentContext = null 
        )
        {
            //リネアルレーアート作る
            LinearLayout? lnIn = new LinearLayout(context);
            lnIn.Orientation = Orientation.Horizontal;
            lnIn.SetBackgroundResource(Resource.Drawable.rounded_primaryColor);

            LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            lnInParams.SetMargins(cardMargins[0], cardMargins[1], cardMargins[2], cardMargins[3]);
            lnIn.LayoutParameters = lnInParams;
            
            /*
             * Create ImageView for song image
             */
            ImageView mori = new ImageView(context);
            LinearLayout.LayoutParams ll = new LinearLayout.LayoutParams(
                (int)(ww * scale + 0.5f), (int)(hh * scale + 0.5f)
            );
            ll.SetMargins(btnMargins[0], btnMargins[1], btnMargins[2], btnMargins[3]);
            mori.LayoutParameters = ll;
            
            
            lnIn.AddView(mori);
            
            
            /*
             * Handle events on song tile
             */
            lnIn.Click += (sender, _) =>
            {
                LinearLayout pressedButton = (LinearLayout)sender;
                foreach (KeyValuePair<LinearLayout, Guid> pr in songButtons.Where(pr => pr.Key == pressedButton))
                {
                    switch (songMediaType) 
                    {
                        case SongMediaType.AllSong:
                            MainActivity.ServiceConnection?.Binder?.Service?.GenerateQueue(MainActivity.StateHandler.Songs, pr.Value);
                            break;
                        case SongMediaType.AlbumSong:
                            MainActivity.ServiceConnection?.Binder?.Service?.GenerateQueue((Album)FragmentPositionObject, pr.Value);
                            break;
                        case SongMediaType.PlaylistSong:
                            MainActivity.ServiceConnection?.Binder?.Service?.GenerateQueue((List<Song>)FragmentPositionObject, pr.Value);
                            break;
                    }
                }
            };
            lnIn.LongClick += (_, _) =>
            {
                if (linForDelete != null)
                    ShowPopupSongEdit(musics, linForDelete, lnIn, context, scale, assets, manager,
                        songsfragmentContext);
            };

            songButtons.Add(lnIn, musics.Id);
            
            
            /*
             * Set Song Information
             */
            TextView name = new TextView(context);
            name.Text = musics.Title;
            name.TextSize = nameSize;
            name.SetTextColor(Color.White);
            name.TextAlignment = TextAlignment.Center;
            name.SetForegroundGravity(GravityFlags.Center);

            LinearLayout.LayoutParams lnNameParams = new LinearLayout.LayoutParams(
                (int)(130 * scale + 0.5f),
                ViewGroup.LayoutParams.WrapContent
            );
            lnNameParams.SetMargins(nameMargins[0], nameMargins[1], nameMargins[2], nameMargins[3]);

            name.LayoutParameters = lnNameParams;

            lnIn.SetGravity(GravityFlags.Center);
            lnIn.SetHorizontalGravity(GravityFlags.Center);
            lnIn.AddView(name);
            
            return lnIn;
        }


        /// <summary>
        /// Vertical order of elements within single element in list of songs or albums (only albums, bcs only albums have vertical alignment of cover and title) 
        /// </summary>
        /// <param name="musics"></param>
        /// <param name="scale"></param>
        /// <param name="btnMargins"></param>
        /// <param name="cardMargins"></param>
        /// <param name="nameMargins"></param>
        /// <param name="nameSize"></param>
        /// <param name="index"></param>
        /// <param name="context"></param>
        /// <param name="albumButtons"></param>
        /// <param name="manager"></param>
        /// <param name="assets"></param>
        /// <param name="linForDelete"></param>
        /// <param name="albumFragment"></param>
        /// <param name="authorFragment"></param>
        /// <param name="ww"></param>
        /// <param name="hh"></param>
        /// <param name="songsFragmentContext"></param>
        /// <returns></returns>
        public static LinearLayout? PopulateVertical(
            MusicBaseClass musics, float scale, int ww, int hh, int[] btnMargins, int[] cardMargins, int[] nameMargins, int nameSize, int index,
            Context context, Dictionary<LinearLayout?, object> albumButtons, 
            FragmentManager manager, AssetManager? assets,
            AlbumFragment? albumFragment = null, AuthorFragment? authorFragment = null,
            LinearLayout? linForDelete = null, SongsFragment? songsFragmentContext = null 
        )
        {
            //リネアルレーアート作る
            LinearLayout? lnIn = new LinearLayout(context);
            lnIn.Orientation = Orientation.Vertical;
            lnIn.SetBackgroundResource(Resource.Drawable.rounded_primaryColor);

            LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            lnInParams.SetMargins(cardMargins[0], cardMargins[1], cardMargins[2], cardMargins[3]);
            lnIn.LayoutParameters = lnInParams;
            
            /*
             * Creating imageView for later to load album/artist thumbnail image
             */
            ImageView mori = new ImageView(context);
            LinearLayout.LayoutParams ll = new LinearLayout.LayoutParams(
                (int)(ww * scale + 0.5f), (int)(hh * scale + 0.5f)
            );
            ll.SetMargins(btnMargins[0], btnMargins[1], btnMargins[2], btnMargins[3]);
            mori.LayoutParameters = ll;
            
            
            lnIn.AddView(mori);


            // ボッタン作って
            if (musics is Album album)
            {
                lnIn.Click += (sender, _) =>
                {
                    LinearLayout pressedButton = (LinearLayout)sender;
                    foreach(KeyValuePair<LinearLayout?, object> pr in albumButtons)
                    {
                        if (pr.Key == pressedButton && pr.Value is Album album1)
                        {
                            // ((AllSongsFragment)activity).ReplaceFragments(AllSongsFragment.FragmentType.AlbumFrag, album1.Title);
                            // AllSongsFragment.GetInstance().ReplaceFragments(AllSongsFragment.FragmentType.AlbumFrag, album1.Title);
                            FragmentTransaction fragmentTransaction = manager.BeginTransaction();
                            Bundle bundle = new Bundle();
                            bundle.PutString("title", album1.Title);

                            if (albumFragment != null)
                            {
                                albumFragment.Arguments = bundle;
                                fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, albumFragment);
                            }

                            fragmentTransaction.AddToBackStack(null);
                            fragmentTransaction.Commit();
                        
                            break;
                        }
                    }
                    
                };

                
                lnIn.LongClick += (_, _) =>
                {
                    if (linForDelete != null)
                        ShowPopupSongEdit(album, linForDelete, lnIn, context, scale, assets, manager,
                            songsFragmentContext);
                };
                

                albumButtons.Add(lnIn, album);
            }
            else if (musics is Artist artist)
            {
                lnIn.Click += (sender, _) =>
                {
                    LinearLayout pressedButton = (LinearLayout)sender;

                    foreach (KeyValuePair<LinearLayout?, object> pr in albumButtons)
                    {
                        if (pr.Key == pressedButton && pr.Value is Artist artist1)
                        {
                            // ((AllSongsFragment)activity).ReplaceFragments(AllSongsFragment.FragmentType.AuthorFrag, artist1.Title);
                            // AllSongsFragment.GetInstance().ReplaceFragments(AllSongsFragment.FragmentType.AuthorFrag, artist1.Title);
                            FragmentTransaction fragmentTransaction = manager.BeginTransaction();
                            Bundle bundle = new Bundle();
                            bundle.PutString("title", artist1.Title);


                            if (authorFragment != null)
                            {
                                authorFragment.Arguments = bundle;
                                fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, authorFragment);
                            }

                            fragmentTransaction.AddToBackStack(null);
                            fragmentTransaction.Commit();
                            
                            break;
                        }
                    }
                };

                
                lnIn.LongClick += (_, _) =>
                {
                    if (linForDelete != null)
                        ShowPopupSongEdit(artist, linForDelete, lnIn, context, scale, assets, manager,
                            songsFragmentContext);
                };
                

                albumButtons.Add(lnIn, artist);
            }
            
            //アルブムの名前
            TextView name = new TextView(context);
            name.Text = musics.Title;
            name.TextSize = nameSize;
            name.SetTextColor(Color.White);
            name.TextAlignment = TextAlignment.Center;
            name.SetForegroundGravity(GravityFlags.Center);

            LinearLayout.LayoutParams lnNameParams = new LinearLayout.LayoutParams(
                (int)(130 * scale + 0.5f),
                ViewGroup.LayoutParams.WrapContent
            );
            lnNameParams.SetMargins(nameMargins[0], nameMargins[1], nameMargins[2], nameMargins[3]);

            name.LayoutParameters = lnNameParams;

            lnIn.SetGravity(GravityFlags.Center);
            lnIn.SetHorizontalGravity(GravityFlags.Center);
            lnIn.AddView(name);
            
            return lnIn;
        }
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Funkcia sluziacia na zistinie stavu elementu na obrazovke
        /// Ak element existuje ale je mimo obrazovky funkcia vrati false.
        /// Ak element existuje a je na obrazovke a uzivatel ho moze vidiet, vrati true.
        /// Funkcia ako prve zisti ci je element bol vytvoreny a exsituje, nasledne si vytvorim Rect objekt ktory bude sluzit na ulozenie dimenzii a pozicie elementu.
        /// Nasledne pomocou funkcie GetGlobalVisibleRect, ktora je dostupna z objektu View, zistim X, Y poziciu elementu
        /// a nakoniec si vytvorim dalsi Rect ktory ma dimenzie obrazovky a spravim prienik tychto dvoch stvrocov
        /// </summary>
        /// <param name="view">Element ktory chcem vyskusat ci je alebo nie je na obrazovke</param>
        /// <returns></returns>
        public static bool IsVisible(View? view) {
            if (view == null || !view.IsShown) {
                return false;
            }
            Rect actualPosition = new Rect();
            view.GetGlobalVisibleRect(actualPosition);
            Rect screen = new Rect(0, 0,
                (int)DeviceDisplay.MainDisplayInfo.Width, (int)DeviceDisplay.MainDisplayInfo.Height);
            return actualPosition.Intersect(screen);
        }


        public enum LoadImageType
        {
            ALBUM,
            AUTHOR,
            SONG
        }

        /// <summary>
        /// Funkcia sluziaca na asynchronne nacitanie Bitmap obrazkov. Sluzi hlavne na prenesenie nacitavani obrazkov z hlavneho UI vlakna
        /// na vlakno vytvorene na pozadi, Je to z dovodu uvolnenia UI vlakna pre rychlejsie a responzivnejsie uizivatelske rozrhanie.
        /// Funkcia je typu Task coz hovori o tom ze mozme na danu funkciu pouzit await pri jej volani.
        /// 
        /// Funkcia prijma list pesniciek cez ktory sa precyklime pomocou for-loopu a pre kazdy pesnicku nacitame obrazok z backend-u
        /// a nasladne sa ulozi do slovnika spolu s klucom ako nazov pesnicku pre rychle vyhladavanie obrazok pre pesnicky v uzivatelskom rohrani.
        /// </summary>
        /// <param name="objects">List generickeho typu, nasledne kovertovany na zaklade UIRenderFunctions.LoadImageType</param>
        /// <param name="buffer">slovnik do ktoreho ulozime nacitane obrazky ako hodnotu a priradime kluc s nazvom pesnicky</param>
        /// <param name="type">Typ dat ktore prijmame ako list. Takze albumy/autorov/pesnicky. Pre koho chcem nacitavat obrazky</param>
        public static async Task LoadSongImages<T>(List<T> objects, ObservableDictionary<string, Bitmap>? buffer, LoadImageType type)
        {
            switch (type)
            {
                case LoadImageType.ALBUM:
                    List<Album> al = objects.ConvertAll(TtoAlbum);
                    for (int i = 0; i < al.Count; i++)
                    {
                        if (buffer != null && !buffer.Items.ContainsKey(al[i].Title)) 
                            buffer.AddItem(al[i].Title, WidgetServiceHandler.GetRoundedCornerBitmap(al[i].Image, 50));
                    }
                    break;
                case LoadImageType.SONG:
                    List<Song> sg = objects.ConvertAll(TtoSong);
                    for (int i = 0; i < sg.Count; i++)
                    {
                        if (buffer != null && !buffer.Items.ContainsKey(sg[i].Title)) 
                            buffer.AddItem(sg[i].Title, WidgetServiceHandler.GetRoundedCornerBitmap(sg[i].Image, 50));
                    }
                    break;
                case LoadImageType.AUTHOR:
                    List<Artist> au = objects.ConvertAll(TtoArtist);
                    for (int i = 0; i < au.Count; i++)
                    {
                        if (buffer != null && !buffer.Items.ContainsKey(au[i].Title)) 
                            buffer.AddItem(au[i].Title, WidgetServiceHandler.GetRoundedCornerBitmap(au[i].Image, 50));
                    }
                    break;
            }
            
            
            
        }

        private static Album TtoAlbum<T>(T a)
        {
            return (Album)Convert.ChangeType(a, typeof(Album));
        }
        private static Song TtoSong<T>(T a)
        {
            return (Song)Convert.ChangeType(a, typeof(Song));
        }
        private static Artist TtoArtist<T>(T a)
        {
            return (Artist)Convert.ChangeType(a, typeof(Artist));
        }
        

        /// <summary>
        /// Funkcia sluziaca na nastavenie obrazka jedneho policka uzivatelskeho rozhrania. Funkcia prijme jeden element typu LinearLayout ktory obsahuje ImageView a TextView.
        /// Do ImageView vlozime nacitany obrazok ktory ziskame zo slovnika s obrazkami a vyhladame ho v tomto slovniku na zaklade textu ktory sa nachadza v LinearLayout-e.
        /// Pokial obrazok nemoze byt najdeny, nacitame miesto toho staticky *.png z assetov.
        /// </summary>
        /// <param name="child">Dieta typu LinearLayout (musi obsahovat TextView a ImageView) z listu policok pesniciek/albumov/artistov</param>
        /// <param name="images">Slovnik s obrazkami</param>
        /// <param name="assets">Assety projektu</param>
        public static void LoadSongImageFromBuffer(LinearLayout? child, ObservableDictionary<string, Bitmap>? images, AssetManager? assets)
        {
            ImageView currentImage = (ImageView)child?.GetChildAt(0)!;
            TextView? currentTitle = (TextView)child?.GetChildAt(1)!;
            if (images != null && currentTitle.Text != null && images.Items.ContainsKey(currentTitle.Text))
            {
#if DEBUG
                MyConsole.WriteLine($"{images.Items?[currentTitle.Text]} <<>> {currentTitle.Text}");
#endif
                if (((BitmapDrawable)currentImage?.Drawable!)?.Bitmap != images.Items?[currentTitle.Text]) 
                    currentImage?.SetImageBitmap(images.Items?[currentTitle.Text]);
            }
            else
            {
                currentImage?.SetImageDrawable(Drawable.CreateFromStream(assets?.Open("music_placeholder.png"), null));
            }
            
        }


        /// <summary>
        /// Funkcia sluziaca na najdenie takzvanych "dier" v zozname ci uz albumov, zoznamov alebo pesniciek a nasledne ich vyplnenie.
        /// Funkcia najde prazdne policka a prideli im nacitany obrazok. Diery vznikaju z dovodu asynnchronneho nacitavania obrazkov pri ktorom sa obrazky nenacitavaju po poradi ale
        /// na pozadi a to naraz, moze dojst k tomu, ze uzivatelske rozhranie je pomalsie nacitane ako pesnicky na slabsich zariadeniach a tak su tieto diery vyplnene na konci nacitavania obrazkov.
        ///
        /// Pomocou foreach-u prechadzam cez vsetky policka, v kazdom policku najdem prislusny ImageView v ktorom skontrolujem, ci obrazok tam je alebo nie. Pokial nie je zavolam
        /// funkciy UIRenderFunctions.LoadSongImageFromBuffer pre nacitanie prislusneho obrazka pre prislusne policko.
        /// </summary>
        /// <param name="context">kontext hlavnej aktivity</param>
        /// <param name="tiles">List Elementov uzivatelskeho rozhrania (Songs, Albums, Authors) v podobe slovnika v paroch (string, LinearLayout)</param>
        /// <param name="images">List Obrazkov pre elementy uzivatelskeho rozhrania v podobe slovnika v paroch (string, Bitmap)</param>
        /// <param name="assets">staticke subory, assety</param>
        public static void FillImageHoles(Context context, Dictionary<string, LinearLayout?> tiles, ObservableDictionary<string, Bitmap>? images, AssetManager? assets)
        {
            
            ((Activity)context).RunOnUiThread(() =>
            {
                foreach (var tile in tiles)
                {
                    ImageView? vv = (ImageView)tile.Value.GetChildAt(0);
                    if (vv?.Drawable == null)
                        LoadSongImageFromBuffer(tile.Value, images, assets);
                }
            });
        }

    }
}