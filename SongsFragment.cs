using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using Android.Graphics;
using Fragment = AndroidX.Fragment.App.Fragment;
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
        private const int SCROLL_VIEW_HEIGHT = 150;
        private float SCALE;
        private Context _context;
        private RelativeLayout MAIN_LAYOUT;

        private Dictionary<LinearLayout, int> SongButtons = new Dictionary<LinearLayout, int>();
            
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inflater"></param>
        /// <param name="container"></param>
        /// <param name="savedInstanceState"></param>
        /// <returns></returns>
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.songs_fragment, container, false);

            MAIN_LAYOUT = view?.FindViewById<RelativeLayout>(Resource.Id.songs_fragment_main);
            
            RenderSongs();
            
            return view;
        }

        /// <summary>
        /// Constructor for SongsFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (e.g. "this")</param>
        public SongsFragment(Context ctx)
        {
            _context = ctx;
            if (ctx.Resources is { DisplayMetrics: not null }) SCALE = ctx.Resources.DisplayMetrics.Density;
        }
        
        
        
        private void RenderSongs()
        {
            ScrollView allSongsScroll = new ScrollView(_context);
            RelativeLayout.LayoutParams allSongsScrollParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            allSongsScrollParams.SetMargins(0, SCROLL_VIEW_HEIGHT, 0, 0);
            allSongsScrollParams.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
            allSongsScroll.LayoutParameters = allSongsScrollParams;


            LinearLayout allSongsLnMain = new LinearLayout(_context);
            allSongsLnMain.Orientation = Orientation.Vertical;
            RelativeLayout.LayoutParams allSongsLnMainParams = new RelativeLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent
            );
            allSongsLnMainParams.SetMargins(20, 20, 20, 20);
            allSongsLnMain.LayoutParameters = allSongsLnMainParams;


            int[] allSongsButtonMargins = { 50, 50, 50, 50 };
            int[] allSongsNameMargins = { 50, 50, 50, 50 };
            int[] allSongsCardMargins = { 0, 50, 0, 0 };


            List<Tuple<LinearLayout, int>> lazyBuffer = new List<Tuple<LinearLayout, int>>();
            
            for (int i = 0; i < MainActivity.stateHandler.Songs.Count; i++)
            {

                LinearLayout lnIn = UIRenderFunctions.PopulateHorizontal(
                    MainActivity.stateHandler.Songs[i], SCALE,
                    150, 100,
                    allSongsButtonMargins, allSongsNameMargins, allSongsCardMargins,
                    17, i, _context, SongButtons, UIRenderFunctions.SongType.allSong, allSongsLnMain
                );
                
                lazyBuffer.Add(new Tuple<LinearLayout, int>(lnIn, i));

            }

            for (int i = 0; i < Math.Min(5, lazyBuffer.Count); i++)
            {
                UIRenderFunctions.SetTilesImage(
                    lazyBuffer[i].Item1, MainActivity.stateHandler.Songs[lazyBuffer[i].Item2],150, 100, allSongsButtonMargins, 15, allSongsNameMargins,
                    SCALE, _context);
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
                            lazyBuffer[i].Item1, MainActivity.stateHandler.Songs[lazyBuffer[i].Item2],150, 100, allSongsButtonMargins, 15, allSongsNameMargins,
                            SCALE, _context);
                        allSongsLnMain.AddView(lazyBuffer[i].Item1);
                    }

                    lazyBuffer.RemoveRange(0, Math.Min(5, lazyBuffer.Count));
                }
            };


            allSongsScroll.AddView(allSongsLnMain);
            MAIN_LAYOUT?.AddView(allSongsScroll);
        }
    }
}