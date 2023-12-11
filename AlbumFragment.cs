using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using Android.Content.Res;
using Fragment = AndroidX.Fragment.App.Fragment;
using Orientation = Android.Widget.Orientation;
#if DEBUG
using MWP.Helpers;
#endif


namespace MWP
{
    /// <summary>
    /// Album fragment, initiated after one of the album buttons is clicked in the AlbumAuthorFragment
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AlbumFragment : Fragment
    {
        private readonly float scale;
        private readonly Context context;
        private RelativeLayout? mainLayout;
        private Album album;
        private AssetManager? assets;
        
        private Dictionary<LinearLayout, Guid> SongButtons = new Dictionary<LinearLayout, Guid>();


        /// <summary>
        /// Constructor for AlbumFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (insert "this")</param>
        public AlbumFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            this.assets = assets;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
        }

        /// <summary>
        ///  AlbumFragment On create view
        /// </summary>
        /// <param name="inflater"></param>
        /// <param name="container"></param>
        /// <param name="savedInstanceState"></param>
        /// <returns></returns>
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.album_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.album_fragment_main);

            string? title = Arguments?.GetString("title");
            List<Album> retreivedSongs = MainActivity.StateHandler.Albums.Search(title);
            if (retreivedSongs.Count > 0)
            {
                album = retreivedSongs[0];
                UIRenderFunctions.FragmentPositionObject = album;
            }
#if DEBUG
            MyConsole.WriteLine("FOUNDED SEARCHED ALBUM NAME IN FRAGMENT: " + album.Title);
#endif
            
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
            songsScrollParams.SetMargins(0, 20, 0, 0);
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
                     context, SongButtons, UIRenderFunctions.SongType.AlbumSong, assets, ParentFragmentManager, lnMain
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