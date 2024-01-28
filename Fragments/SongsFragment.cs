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
    /// Fragment for all songs scroll view
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
        
        private Dictionary<LinearLayout?, Guid> songButtons = new Dictionary<LinearLayout?, Guid>();
        private static Dictionary<string, LinearLayout?>? _lazyBuffer;
        private static ObservableDictionary<string, Bitmap>? _lazyImageBuffer;
        
        private readonly long delay = 500; 
        private long lastTextEdit = 0;
        private static Handler _handler = new Handler();

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
        
        
        
        
        /// <inheritdoc />
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.songs_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.songs_fragment_main);

            if (_lazyImageBuffer != null)
                _lazyImageBuffer.ValueChanged += (_, _) =>
                {
                    ((Activity)context).RunOnUiThread(() => {
                        string last = _lazyImageBuffer.Items.Keys.Last();

                        LinearLayout? child = _lazyBuffer?[last] ?? new LinearLayout(context);
                        if (_assets != null)
                            UIRenderFunctions.LoadSongImageFromBuffer(child, _lazyImageBuffer, _assets);
                    });
                };
            

            /*
             * Handle creating base block views
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
            
            
            /*
             * Handle rendering songs by some order
             */
            SongOrder(view);

            
            /*
             * Handle song searching
             */
            SongSearch(view);

            
            /*
             * Handle floating button for creating new playlist
             */
            FloatingActionButton? createPlaylist = mainLayout?.FindViewById<FloatingActionButton>(Resource.Id.songs_fab);
            if (BlendMode.Multiply != null)
                createPlaylist?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
            if (createPlaylist != null) createPlaylist.Click += delegate
            {
                MainActivity.ServiceConnection.Binder?.Service.Shuffle(
                    !MainActivity.ServiceConnection.Binder?.Service.QueueObject.IsShuffled ?? false);
                MainActivity.ServiceConnection.Binder?.Service.Play();
            };

            
            /*
             * Load Song images
             */ 
            Task.Run(async () =>
            {
                await UIRenderFunctions.LoadSongImages(MainActivity.StateHandler.Songs, _lazyImageBuffer, UIRenderFunctions.LoadImageType.SONG);
                UIRenderFunctions.FillImageHoles(context, _lazyBuffer, _lazyImageBuffer, _assets);
            });
            
            return view;
        }



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
             * VYHLADAVNIE
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
                 * Nacitanie songov z vyhladavania po tom co pouzivatel prestane pisat po jednej sekunde
                 */
                searchInput.Typeface = font;
                if (searchInput.Text == "")
                {
                    _allSongsLnMain?.RemoveAllViews();
                    RenderSongs(MainActivity.StateHandler.Songs);
                }
                
                searchInput.TextChanged += (object sender, Android.Text.TextChangedEventArgs e) =>
                {
                    _handler.RemoveCallbacks(() =>
                    {
                        if (view != null) inputFinishChecker(MainActivity.StateHandler.Songs, view, context);
                    });

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
                        lastTextEdit = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        _handler.PostDelayed(() =>
                        {
                            if (view != null) inputFinishChecker(MainActivity.StateHandler.Songs, view, context);
                        }, delay);
                    }
                };

                
                
                /*
                 * Nacitanie songov po tom co pouzivatel stlaci tlacidlo na potvrdenie vyhladavania
                 * 
                 */
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
        
        
        private async void RenderSongs(List<Song> songs)
        {
            _lazyBuffer = new Dictionary<string, LinearLayout>();
            
            
            for (int i = 0; i < songs.Count; i++)
            {
                
                LinearLayout? lnIn = UIRenderFunctions.PopulateHorizontal(
                    songs[i], scale,
                    150, 100,
                    allSongsButtonMargins, allSongsNameMargins, allSongsCardMargins,
                    17,  context, songButtons, UIRenderFunctions.SongType.AllSong, _assets, ParentFragmentManager, 
                    _allSongsLnMain, this
                );
                if (_lazyBuffer.TryAdd(songs[i].Title, lnIn))
                {
                    _allSongsLnMain?.AddView(lnIn);
                }
                    
            }

            /*
            decimal percentage = ((decimal)_lazyImageBuffer.Items.Count / (decimal)_lazyBuffer.Count) * 100;
            #if DEBUG
            MyConsole.WriteLine($"Percentage of Loaded Songs {_lazyImageBuffer.Items.Count} / {_lazyBuffer.Count} = {(decimal)_lazyImageBuffer.Items.Count / (decimal)_lazyBuffer.Count}");
            #endif
            if (percentage > 80)
            {
                for (int i = 0; i < songs.Count; i++)
                {
                    LinearLayout? child = _lazyBuffer[songs[i].Title];
                    if (_assets != null) UIRenderFunctions.LoadSongImageFromBuffer(child, _lazyImageBuffer, _assets);
                }
            }
            */
            
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
                    LinearLayout? lnIn = UIRenderFunctions.PopulateHorizontal(
                        song, scale,
                        150, 100,
                        allSongsButtonMargins, allSongsNameMargins, allSongsCardMargins,
                        17,  context, songButtons, UIRenderFunctions.SongType.AllSong, _assets, ParentFragmentManager, 
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