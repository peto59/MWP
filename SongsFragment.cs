using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views.InputMethods;
using Ass_Pain.BackEnd;
using Google.Android.Material.FloatingActionButton;
using Fragment = AndroidX.Fragment.App.Fragment;
using Orientation = Android.Widget.Orientation;
#if DEBUG
using Ass_Pain.Helpers;
#endif


namespace Ass_Pain
{
    /// <summary>
    /// Fragment for all songs scroll view
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class SongsFragment : Fragment
    {
        private const int ActionScrollViewHeight = 200;
        private float scale;
        private readonly Context context;
        private RelativeLayout? mainLayout;
        private Typeface? font;
        private ScrollView allSongsScroll;
        private LinearLayout allSongsLnMain;
        private AssetManager? assets;

        private Dictionary<LinearLayout, int> songButtons = new Dictionary<LinearLayout, int>();
        
        long delay = 1000; 
        long lastTextEdit = 0;
        Handler handler = new Handler();
        
            
       /// <inheritdoc cref="context"/>
        public SongsFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            this.assets = assets;
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
        }
        
        
        
        
        /// <inheritdoc />
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.songs_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.songs_fragment_main);
            
            allSongsScroll = new ScrollView(context);
            RelativeLayout.LayoutParams allSongsScrollParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            allSongsScrollParams.SetMargins(0, ActionScrollViewHeight, 0, 0);
            allSongsScroll.LayoutParameters = allSongsScrollParams;


            allSongsLnMain = new LinearLayout(context);
            allSongsLnMain.Orientation = Orientation.Vertical;
            RelativeLayout.LayoutParams allSongsLnMainParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            allSongsLnMainParams.SetMargins(20, 20, 20, 20);
            allSongsLnMain.LayoutParameters = allSongsLnMainParams;
            
            allSongsScroll.AddView(allSongsLnMain);
            mainLayout?.AddView(allSongsScroll);
            
            
            
            /*
             * VYHLADAVNIE
             */
            Action<List<Song>, View, Context> inputFinishChecker = (songs, view1, ctx) =>
            {   
                if ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) > (lastTextEdit + delay - 500))
                {
                    Console.WriteLine("Stopped Writing, USER STOPPED WRITING OM GOUUTYAYAYD, THAT SOI COOOL");
                    allSongsLnMain.RemoveAllViews();
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
                    allSongsLnMain.RemoveAllViews();
                    RenderSongs(MainActivity.stateHandler.Songs);
                }
                
                searchInput.TextChanged += (object sender, Android.Text.TextChangedEventArgs e) =>
                {
                    handler.RemoveCallbacks(() =>
                    {
                        if (view != null) inputFinishChecker(MainActivity.stateHandler.Songs, view, context);
                    });

                    if (e.Text != null && e.Text.Any())
                    {
                        lastTextEdit = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        handler.PostDelayed(() =>
                            {
                                if (view != null)
                                    inputFinishChecker(
                                        MainActivity.stateHandler.Songs.Search(e.Text.ToString()).ToList(),
                                        view,
                                        context
                                    );
                            }, delay
                        );
                    }
                    else
                    {
                        lastTextEdit = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        handler.PostDelayed(() =>
                        {
                            if (view != null) inputFinishChecker(MainActivity.stateHandler.Songs, view, context);
                        }, delay);
                    }
                };

                /*
                 * Nacitanie songov po tom co pouzivatel stlaci tlacidlo na potvrdenie vyhladavania
                 */
                if (searchButton != null)
                    searchButton.Click += delegate
                    {
                        if (searchInput.Text == null) return;
                        if (view != null)
                            inputFinishChecker(
                                MainActivity.stateHandler.Songs.Search(searchInput.Text).ToList(),
                                view, context
                            );
                    };
            }
            

            
            FloatingActionButton? createPlaylist = mainLayout?.FindViewById<FloatingActionButton>(Resource.Id.fab);
            if (BlendMode.Multiply != null)
                createPlaylist?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
            if (createPlaylist != null) createPlaylist.Click += CreatePlaylistPopup;
            
            return view;
        }

        
        
        private void RenderSongs(List<Song> songs)
        {
            int[] allSongsButtonMargins = { 50, 50, 50, 50 };
            int[] allSongsNameMargins = { 50, 50, 50, 50 };
            int[] allSongsCardMargins = { 0, 50, 0, 0 };


            List<Tuple<LinearLayout, int>> lazyBuffer = new List<Tuple<LinearLayout, int>>();
            
            for (int i = 0; i < songs.Count; i++)
            {

                LinearLayout lnIn = UIRenderFunctions.PopulateHorizontal(
                    songs[i], scale,
                    150, 100,
                    allSongsButtonMargins, allSongsNameMargins, allSongsCardMargins,
                    17, i, context, songButtons, UIRenderFunctions.SongType.allSong, assets, ParentFragmentManager, allSongsLnMain
                );
                
                lazyBuffer.Add(new Tuple<LinearLayout, int>(lnIn, i));

            }

            for (int i = 0; i < Math.Min(5, lazyBuffer.Count); i++)
            {
                UIRenderFunctions.SetTilesImage(
                    lazyBuffer[i].Item1, songs[lazyBuffer[i].Item2],150, 100, allSongsButtonMargins, 15, allSongsNameMargins,
                    scale, context);
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
                    for (int i = 0; i < Math.Min(5, lazyBuffer.Count); i++)
                    {
                        UIRenderFunctions.SetTilesImage(
                            lazyBuffer[i].Item1, songs[lazyBuffer[i].Item2],150, 100, allSongsButtonMargins, 15, allSongsNameMargins,
                            scale, context);
                        allSongsLnMain.AddView(lazyBuffer[i].Item1);
                    }

                    lazyBuffer.RemoveRange(0, Math.Min(5, lazyBuffer.Count));
                }
            };
            
        } 
        
        
        
        private void CreatePlaylistPopup(object sender, EventArgs e)
        {
            LayoutInflater? ifl = LayoutInflater.From(context);
            View? view = ifl?.Inflate(Resource.Layout.new_playlist_popup, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);

            TextView? dialogTitle = view?.FindViewById<TextView>(Resource.Id.AddPlaylist_title);
            if (dialogTitle != null) dialogTitle.Typeface = font;

            EditText? userData = view?.FindViewById<EditText>(Resource.Id.editText);
            if (userData != null)
            {
                userData.Typeface = font;
                alert.SetCancelable(false);


                TextView? pButton = view?.FindViewById<TextView>(Resource.Id.AddPlaylist_submit);
                if (pButton != null) pButton.Typeface = font;
                if (pButton != null)
                    pButton.Click += (_, _) =>
                    {
                        if (userData.Text != null)
                        {
                            FileManager.CreatePlaylist(userData.Text);
                            Toast.MakeText(
                                    context, userData.Text + " Created successfully",
                                    ToastLength.Short
                                )
                                ?.Show();
                        }

                        alert.Dispose();
                    };
            }

            TextView? nButton = view?.FindViewById<TextView>(Resource.Id.AddPlaylist_cancel);
            if (nButton != null) nButton.Typeface = font;
         

            AlertDialog? dialog = alert.Create();
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
            if (nButton != null) nButton.Click += (_, _) => dialog?.Cancel();
            
            
            dialog?.Show();
            

        }
        
        
        
        private void add_alias_popup(string authorN)
        {

            LayoutInflater ifl = LayoutInflater.From(context);
            View view = ifl?.Inflate(Resource.Layout.add_alias_popup, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);

            AlertDialog dialog = alert.Create();

            if (view != null)
            {
                TextView author = view.FindViewById<TextView>(Resource.Id.author_name);
                if (author != null) author.Text = authorN;
            }

            EditText userInput = view?.FindViewById<EditText>(Resource.Id.user_author);
            Button sub = view?.FindViewById<Button>(Resource.Id.submit_alias);
            if (sub != null)
                sub.Click += delegate
                {
                    if (userInput != null) FileManager.AddAlias(authorN, userInput.Text);
                    dialog?.Hide();
                };

            Button cancel = view?.FindViewById<Button>(Resource.Id.cancel_alias);
            if (cancel != null)
                cancel.Click += delegate { dialog?.Hide(); };


            dialog?.Show();
        }
    }
}