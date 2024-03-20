using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views.InputMethods;
using Google.Android.Material.FloatingActionButton;
using Java.Lang;
using MWP.BackEnd;
using Xamarin.Essentials;
using Fragment = AndroidX.Fragment.App.Fragment;
using Orientation = Android.Widget.Orientation;
#if DEBUG
using MWP.Helpers;
#endif


namespace MWP
{
    /// <summary>
    /// Fragment slúžiaci na vyobrazenie zoznamu všetkých skladieb zariadenia používateľa. V rámci fragmentu má používateľ k dispozícii
    /// vyhľadávanie a zoradzovanie skladieb. Možnosti zoradenia sú (od a-z, od z-a, do najnovšiej a od najstršiej). Zoznam je interaktívny,
    /// používateľ je schopný skladby spúśťať a pri dlhom podržaní s nimi manipulovať ako napríklad, meniť metadáta, vymazávať, pridávať do playlistov.
    /// Sklaby su renderované prostredníctvom techniky Lazy Loading implementovaný za pomoci viacerých vlákien.
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class SongsFragment : Fragment
    {
        private const int ActionScrollViewHeight = 320;
        private float scale;
        private readonly Context context;
        private RelativeLayout? mainLayout;
        private Typeface? font;
        private ScrollView allSongsScroll;
        private static LinearLayout? _allSongsLnMain;
        private static AssetManager? _assets;
        
        /*
         * Listy obsahujúce jednotlivé elementy skladieb a ich príslušné obrázky. Obrázky sú v osobitnom liste z dôvody
         * Lazy Loading-u (takže pre rýchle nezávislé načítavanie)
         */
        private Dictionary<LinearLayout?, Guid> songButtons = new Dictionary<LinearLayout?, Guid>();
        private static Dictionary<string, LinearLayout> _lazyBuffer;
        private static ObservableDictionary<string, Bitmap>? _lazyImageBuffer;
        
        private readonly long delay = 500; 
        private long lastTextEdit = 0;
        private static Handler _handler = new Handler();

        /*
         * Hodnoty udávajúce Marginy jednotlivých elementov kartičiek v zozname skladieb
         */
        private int[] allSongsButtonMargins;
        private int[] allSongsNameMargins;
        private int[] allSongsCardMargins;
        
            
       /// <inheritdoc cref="context"/>
        public SongsFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            _assets = assets;
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;

            allSongsButtonMargins = new []{ 50, 50, 50, 50 };
            allSongsNameMargins = new []{ 50, 50, 50, 50 };
            allSongsCardMargins = new []{ 0, 50, 0, 0 };
            
            _lazyImageBuffer = new ObservableDictionary<string, Bitmap>();

            StateHandler.OnTagManagerFragmentRefresh += tuple =>
            {
                UpdateSong(tuple.oldTitle, tuple.song);
            };
        }
        
        
        
        
        
        /// <summary>
        /// OnCreateView je metóda ktorá sa spúšta pri vytváraní fragmentu a slúźi na inicializáciu elementov fragmentu, ako napríklad:
        /// Lazy Loading, začatie načítavania obrázkov skladieb; vytvorenie hlavného layout-u zoznamu skladieb; Vytvorenie Fab tlačidla;
        /// inicializácie funkcie vyhľadávania; inicializácia funkcie zoradzovania skladieb.
        /// </summary>
        /// <returns></returns>
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.songs_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.songs_fragment_main);

            /*
             * Metoda priradena datovej polozke EventHandler v objekte typu ObservableDictionary.
             * Metoda sa spusti vzdy ked je pridany novy element do dictionary. Po tom co sa
             * prida novy element spusti sa nasledovna seria prikazov.
             */
            if (_lazyImageBuffer != null)
                _lazyImageBuffer.ValueChanged += (_, _) =>
                {
                    ((Activity)context).RunOnUiThread(() => {
                        /*
                         * ziskanie posledneho pridaneho elementu typu Bitmap (cize obrazok skladby)
                         * a nasledne jeho priradenie prisluchajucemu policku v zozname skladibe v uzivatelskom rozhrani
                         */
                        string last = _lazyImageBuffer.Items.Keys.Last();

                        LinearLayout? child = _lazyBuffer?[last] ?? new LinearLayout(context);
                        if (_assets != null)
                            UiRenderFunctions.LoadSongImageFromBuffer(child, _lazyImageBuffer, _assets);
                    });
                };
            

            /*
             * Vytvaranie zakladneho ScrollView elementu pre zoznam skladieb a obaloveho linearLayout elementu
             * z dovodu toho, ze ScrollView moze obsahovat iba jeden element.
             */
            allSongsScroll = new ScrollView(context);
            RelativeLayout.LayoutParams allSongsScrollParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            allSongsScrollParams.SetMargins(0, ActionScrollViewHeight, 0, 0);
            allSongsScroll.LayoutParameters = allSongsScrollParams;


            _allSongsLnMain = new LinearLayout(context);
            _allSongsLnMain.Orientation = Orientation.Vertical;
            RelativeLayout.LayoutParams allSongsLnMainParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            allSongsLnMainParams.SetMargins(20, 20, 20, 20);
            _allSongsLnMain.LayoutParameters = allSongsLnMainParams;
            
            allSongsScroll.AddView(_allSongsLnMain);
            mainLayout?.AddView(allSongsScroll);
            
            
            SongOrder(view);
            SongSearch(view);
            
            /*
             * Vytvorenie FloatingActionButton sluziace na zapnutie nahodneho prehravania.
             * Nastavi sa nahodne prehravanie v pripade ak  este nie je nastavene as spusti sa prehravanie hudby preostrednictvom
             * metod z pozadia aplikacie.
             */
            FloatingActionButton? createPlaylist = mainLayout?.FindViewById<FloatingActionButton>(Resource.Id.songs_fab);
            if (BlendMode.Multiply != null)
                createPlaylist?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
            if (createPlaylist != null) createPlaylist.Click += delegate
            {
                MainActivity.ServiceConnection.Binder?.Service.Shuffle(true);
                MainActivity.ServiceConnection.Binder?.Service.Play(true);
            };

            
            /*
             * Vytvorenie noveho vlakna v ramci ktoreho sa na pozadi budu nacitavat skladby do ObservableDictionary,
             * co znamena ze pri akelkovej uprave listu sa spusti metoda ktora prida posledny nacitany obrazok do UI
             */ 
            Task.Run(async () =>
            {
                MainActivity.StateHandler.FileListGenerated.WaitOne();
                await UiRenderFunctions.LoadSongImages(MainActivity.StateHandler.Songs, _lazyImageBuffer, UiRenderFunctions.LoadImageType.SONG);
                UiRenderFunctions.FillImageHoles(context, _lazyBuffer, _lazyImageBuffer, _assets);
            });
            
            return view;
        }


        /// <summary>
        /// Metóda slúži na obstranie událostí pri klknutí na tlčidlá slúžiace na zoradenie skladieb.
        /// </summary>
        /// <param name="view">View v ktorom sa nachádza fragment a z ktorého získavame jednotlivé elementy nachádzajúce sa vo fragmente</param>
        private void SongOrder(View? view)
        {
            TextView? aZ = view?.FindViewById<TextView>(Resource.Id.A_Z_btn);
            TextView? zA = view?.FindViewById<TextView>(Resource.Id.Z_A_btn);
            TextView? newDate = view?.FindViewById<TextView>(Resource.Id.new_order_btn);
            TextView? oldDate = view?.FindViewById<TextView>(Resource.Id.old_order_btn);

            if (aZ != null) aZ.Typeface = font;
            if (zA != null) zA.Typeface = font;
            if (newDate != null) newDate.Typeface = font;
            if (oldDate != null) oldDate.Typeface = font;

            if (aZ != null && zA != null && newDate != null && oldDate != null)
            {
                aZ.Typeface = font;
                zA.Typeface = font;
                newDate.Typeface = font;
                oldDate.Typeface = font;
                
                aZ.Click += delegate
                {
                    _allSongsLnMain?.RemoveAllViews();
                    RenderSongs(MainActivity.StateHandler.Songs.OrderAlphabetically());
                };
                zA.Click += delegate
                {
                    _allSongsLnMain?.RemoveAllViews();
                    RenderSongs(MainActivity.StateHandler.Songs.OrderAlphabetically(true));
                };
                newDate.Click += delegate
                {
                    _allSongsLnMain?.RemoveAllViews();
                    RenderSongs(MainActivity.StateHandler.Songs.OrderByDate());
                };
                oldDate.Click += delegate
                {
                    _allSongsLnMain?.RemoveAllViews();
                    RenderSongs(MainActivity.StateHandler.Songs.OrderByDate(true));
                };
            }
       
        }
        

        private void SongSearch(View? view)
        {
             /*
             * Metóda slúžiaca na prerenderovanie políčok skladieb po tom čo puživateľ prestal písať po dobu 1 skeundy.
             * Ak čas uplynutý od poslednej úpravy vyhľadávania je väčší ako čas od kedy používateľ niečo zadal plus x milisekúnd
             * podľa premennej delay. Pokiaľ je podmienka pravidvá, premaže sa celé rozhranie sladieb a načítaju sa od znova, ale iba tie ktoré
              * vyhovujú vyhľiadávaniu
             */
            Action<List<Song>, View, Context> inputFinishChecker = (songs, view1, ctx) =>
            {   
                if ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) > (lastTextEdit + delay - 500))
                {
                    _allSongsLnMain?.RemoveAllViews();
                    RenderSongs(songs);
                }
            };
            

            TextView? searchButton = view?.FindViewById<TextView>(Resource.Id.search_confirm_songs);
            EditText? searchInput = view?.FindViewById<EditText>(Resource.Id.search_songs_input);
            if (searchInput != null)
            {
                /*
                 * Pokiaľ je zadaný text vo vyhľiadavaní prázdny. Vykreslia sa všetky skladby.
                 */
                searchInput.Typeface = font;
                if (searchInput.Text == "")
                {
                    _allSongsLnMain?.RemoveAllViews();
                    RenderSongs(MainActivity.StateHandler.Songs);
                }
                
                /*
                 * V prípade zmeny vyhľiadavania používateľom:
                 */
                searchInput.TextChanged += (_, e) =>
                {
                    _handler.RemoveCallbacks(() =>
                    {
                        if (view != null) inputFinishChecker(MainActivity.StateHandler.Songs, view, context);
                    });

                    /*
                     * Pokiaľ zadaný text nie je prázdny, zaznamenáme poslednú úpravu, zostatkový text vo vyhľiadavaní
                     * použijeme na získanie skladieb vyhovujícim vyhľiadavaniu za pomoci metódy Search z pozadia,
                     * a prekreslíme na obrazovku za pomoci metódy InputFinishChecker
                     */
                    if (e.Text != null && e.Text.Any())
                    {
                        lastTextEdit = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        _handler.PostDelayed(() =>
                            {
                                if (view != null)
                                    inputFinishChecker(
                                        MainActivity.StateHandler.Songs.Search(e.Text.ToString()).ToList(),
                                        view,
                                        context
                                    );
                            }, delay
                        );
                    }
                    else
                    {
                        /*
                         * Ak je vyhľiadavanie prázdne, vyhľiadajú sa všetky sklaby zo zariadenia.
                         */
                        lastTextEdit = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        _handler.PostDelayed(() =>
                        {
                            if (view != null) inputFinishChecker(MainActivity.StateHandler.Songs, view, context);
                        }, delay);
                    }
                };
                
                // Nacitanie songov po tom co pouzivatel stlaci tlacidlo na potvrdenie vyhladavania
                if (searchButton != null)
                    searchButton.Click += delegate
                    {
                        if (searchInput.Text == null) return;
                        if (view != null)
                            inputFinishChecker(
                                MainActivity.StateHandler.Songs.Search(searchInput.Text).ToList(),
                                view, context
                            );
                    };
            }
        }
        
        
        /*
         * Hlavna metoda sluziaca na vytvorenie hlavneho zoznamu skladieb.
         */
        private async void RenderSongs(List<Song> songs)
        {
            _lazyBuffer = new Dictionary<string, LinearLayout>();
            
            /*
             * Prechadzanie vsetkymi skladbami zo zosiakenho listu ako parameter.
             * Pre kazdy list je vytvorene nove policko ako LinearLayout. Kazde pridane policko
             * sa prida aj do listu neskor pouzity na pridelenie korektneho obrazka skladby.
             */
            for (int i = 0; i < songs.Count; i++)
            {
                
                LinearLayout? lnIn = UiRenderFunctions.PopulateHorizontal(
                    songs[i], scale,
                    150, 100,
                    allSongsButtonMargins, allSongsNameMargins, allSongsCardMargins,
                    17,  context, songButtons, UiRenderFunctions.SongMediaType.AllSong, _assets, ParentFragmentManager, 
                    _allSongsLnMain, this
                );
                if (lnIn != null && _lazyBuffer.TryAdd(songs[i].Title, lnIn))
                {
                    _allSongsLnMain?.AddView(lnIn);
                }
                    
            }
            

            /*
             * Ak je metoda RenderSongs zavolana po prvotnom vytvoreni zoznamu, takze napriklad vyhladavanim alebo zmenenim
             * poradia skladieb (A-Z, Z-A, ...), tak obrazky sa uz nenacitavaju odznova ale su nacitanvane z ObservableDictionary
             * kde sa pri prvotnom nacitanu nahrali vsetky obrazky.
             * Obrazky sa nacitaju ale len pod podmienkou ak zoznam skladieb nie je prazdy a nacitanych obrazkov je viac ako 90%
             */
            if (_lazyBuffer.Count > 0)
            {
                var percentage = (_lazyImageBuffer.Items.Count / _lazyBuffer.Count) * 100;
                if (percentage > 80)
                {
                    foreach (var song in songs)
                    {
                        LinearLayout? child = _lazyBuffer[song.Title];
                        if (_assets != null) UiRenderFunctions.LoadSongImageFromBuffer(child, _lazyImageBuffer, _assets);
                    }
                }
            }
           
            
        } 
        

        /// <summary>
        /// Use for invalidating rendered songs and rerender them again, due to rerendering
        /// whole view when something changed, e.g. song was deleted
        /// </summary>
        public void InvalidateCache()
        {
            songButtons.Clear();
            _lazyBuffer?.Clear();
            _allSongsLnMain?.RemoveAllViews();
            RenderSongs(MainActivity.StateHandler.Songs);
        }

       /// <summary>
       /// Táto metóda aktualizuje zoznamu skladieb ak bola skladba upravená v TagManager-i
       /// Prechádza všetky potomky lineárneho layoutu, ktorý obsahuje zoznam skladieb. Porovnáva názvy skladieb a ak nájde skladbu so starým názvom,
       /// odstráni ju a nahradí novou skladbou.
       /// </summary>
       /// <param name="oldTitle">starý názov skladby pre identifikáciu sklaby ktorá sa zmenila a jej následne odobratie</param>
       /// <param name="song">nová skladba na pridanie do zoznamu s upravenými metadátami</param>
        private void UpdateSong(string oldTitle, Song song)
        {
          
            for (int i = 0; i < _allSongsLnMain!.ChildCount; i++)
            {
                LinearLayout? ithChild = (LinearLayout)_allSongsLnMain.GetChildAt(i)!;
                var childTitle = ((TextView)ithChild?.GetChildAt(1)!).Text;
                if (childTitle != null && childTitle.Equals(oldTitle))
                {
#if DEBUG
                    MyConsole.WriteLine($"old title {oldTitle}");
                    MyConsole.WriteLine($"child title {childTitle}");
                    MyConsole.WriteLine($"new title {song.Title}");
                    MyConsole.WriteLine($"new song id {song.Id}");
#endif
                    
                    _allSongsLnMain.RemoveView(ithChild);
                    _lazyBuffer?.Remove(oldTitle);
                    _lazyImageBuffer?.Items.Remove(oldTitle);
                    songButtons.Remove(ithChild);
                    // fea2db10-94ec-4fb3-8e32-f0d351d77d1b
                    LinearLayout? lnIn = UiRenderFunctions.PopulateHorizontal(
                        song, scale,
                        150, 100,
                        allSongsButtonMargins, allSongsNameMargins, allSongsCardMargins,
                        17,  context, songButtons, UiRenderFunctions.SongMediaType.AllSong, _assets, ParentFragmentManager, 
                        _allSongsLnMain, this
                    );
                    ImageView? cover = (ImageView)lnIn?.GetChildAt(0)!;
                    cover.SetImageBitmap(song.Image);
                    _lazyImageBuffer?.Items.Add(song.Title, song.Image);
                    
                    if (_lazyBuffer != null && _lazyBuffer.TryAdd(song.Title, lnIn))
                    {
                        _allSongsLnMain?.AddView(lnIn);
                    }
                    
                    // RenderSongs(MainActivity.StateHandler.Songs);

                    return;

                }
            }
            
            
            
        }
        
        
        
        /*private void add_alias_popup(string authorN)
        {

            LayoutInflater? ifl = LayoutInflater.From(context);
            View? view = ifl?.Inflate(Resource.Layout.add_alias_popup, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);

            AlertDialog? dialog = alert.Create();

            TextView? author = view?.FindViewById<TextView>(Resource.Id.author_name);
            if (author != null) author.Text = authorN;

            EditText? userInput = view?.FindViewById<EditText>(Resource.Id.user_author);
            Button? sub = view?.FindViewById<Button>(Resource.Id.submit_alias);
            if (sub != null)
                sub.Click += delegate
                {
                    if (userInput is { Text: not null }) FileManager.AddAlias(authorN, userInput.Text);
                    dialog?.Hide();
                };

            Button? cancel = view?.FindViewById<Button>(Resource.Id.cancel_alias);
            if (cancel != null)
                cancel.Click += delegate { dialog?.Hide(); };


            dialog?.Show();
        }*/
    }
    
    
}