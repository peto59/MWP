using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using Android.Graphics;
using Android.Util;
using Fragment = AndroidX.Fragment.App.Fragment;
#if DEBUG
using Ass_Pain.Helpers;
#endif


namespace Ass_Pain
{
    /// <summary>
    /// Album fragment, initiated after one of the album buttons is clicked in the AlbumAuthorFragment
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AlbumFragment : Fragment
    {
        private readonly float scale;
        private readonly Context context;
        private RelativeLayout mainLayout;
        private Album album;
        
        private Dictionary<LinearLayout, int> SongButtons = new Dictionary<LinearLayout, int>();


        /// <summary>
        /// Constructor for AlbumFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (insert "this")</param>
        public AlbumFragment(Context ctx)
        {
            context = ctx;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
        }

        /// <summary>
        ///  AlbumFragment On create view
        /// </summary>
        /// <param name="inflater"></param>
        /// <param name="container"></param>
        /// <param name="savedInstanceState"></param>
        /// <returns></returns>
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.album_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.album_fragment_main);

            string title = Arguments.GetString("title");
            List<Album> retreivedSongs = MainActivity.stateHandler.Albums.Search(title);
            if (retreivedSongs.Count > 0)
            {
                album = retreivedSongs[0];
                UIRenderFunctions.FragmentPositionObject = album;
            }
            Console.WriteLine("FOUNDED SEARCHED ALBUM NAME IN FRAGMENT: " + album.Title);
            
            RenderSongs();

            return view;
        }


        private void RenderSongs()
        {
            ScrollView songsScroll = new ScrollView(context);
            RelativeLayout.LayoutParams songsScrollParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            songsScrollParams.SetMargins(0, 150, 0, 0);
            songsScrollParams.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
            songsScroll.LayoutParameters = songsScrollParams;
            

            LinearLayout lnMain = new LinearLayout(context);
            lnMain.Orientation = Orientation.Vertical;
            RelativeLayout.LayoutParams lnMainParams = new RelativeLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent
            );
            lnMainParams.SetMargins(20, 20, 20, 20);
            lnMain.LayoutParameters = lnMainParams;

            int[] buttonMargins = { 50, 50, 50, 50 };
            int[] nameMargins = { 50, 50, 50, 50 };
            int[] cardMargins = { 0, 50, 0, 0 };

            
            
            for (int i = 0; i < album.Songs.Count; i++)
            {

                LinearLayout lnIn = UIRenderFunctions.PopulateHorizontal(
                    album.Songs[i], scale,
                    150, 100,
                    buttonMargins, nameMargins, cardMargins,
                    17,
                    i, context, SongButtons, UIRenderFunctions.SongType.albumSong, lnMain
                );
                UIRenderFunctions.SetTilesImage(
                    lnIn, album.Songs[i],  150, 100,
                    buttonMargins, 17,
                    nameMargins, scale, context
                );
                lnMain.AddView(lnIn);
            }

           
            songsScroll.AddView(lnMain);
            mainLayout?.AddView(songsScroll);
                
        }
        

    }
}