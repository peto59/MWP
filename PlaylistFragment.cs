using System;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Graphics;
using Ass_Pain.BackEnd;
using Fragment = AndroidX.Fragment.App.Fragment;


namespace Ass_Pain
{
    /// <summary>
    /// 
    /// </summary>
    public class PlaylistFragment : Fragment
    {
        private readonly Context context;
        private RelativeLayout mainLayout;
        private const int ActionScrollViewHeight = 150;
        private readonly float scale;
        
        private Dictionary<LinearLayout, int> songButtons = new Dictionary<LinearLayout, int>();
        
            
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inflater"></param>
        /// <param name="container"></param>
        /// <param name="savedInstanceState"></param>
        /// <returns></returns>
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.playlist_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.playlistttt_fragment_main);
            
            string title = Arguments.GetString("title");
            RenderPlaylists(title);
            
            return view;
        }

        /// <summary>
        /// Constructor for SongsFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (e.g. "this")</param>
        public PlaylistFragment(Context ctx)
        {
            context = ctx;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
        }

        
        private void RenderPlaylists(string title)
        {
            ScrollView inPlaylistScroll = new ScrollView(context);
            RelativeLayout.LayoutParams inPlaylistScrollParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            inPlaylistScrollParams.SetMargins(0, ActionScrollViewHeight, 0, 0);
            inPlaylistScroll.LayoutParameters = inPlaylistScrollParams;
            
            LinearLayout inPlaylistLnMain = new LinearLayout(context);
            inPlaylistLnMain.Orientation = Orientation.Vertical;
            RelativeLayout.LayoutParams inPlaylistLnMainParams = new RelativeLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent
            );
            inPlaylistLnMainParams.SetMargins(20, 20, 20, 20);
            inPlaylistLnMain.LayoutParameters = inPlaylistLnMainParams;

            int[] inPlaylistButtonMargins = { 50, 50, 50, 50 };
            int[] inPlaylistNameMargins = { 50, 50, 50, 50 };
            int[] inPlaylistCardMargins = { 0, 50, 0, 0 };


            List<Song> playlistSongs = FileManager.GetPlaylist(title);
            UIRenderFunctions.FragmentPositionObject = playlistSongs;
            
            for (int i = 0; i < playlistSongs.Count; i++)
            {
                LinearLayout lnIn = UIRenderFunctions.PopulateHorizontal(
                    playlistSongs[i], scale,
                    150, 100,
                    inPlaylistButtonMargins, inPlaylistNameMargins, inPlaylistCardMargins,
                    17,
                    i, 
                    context, songButtons, UIRenderFunctions.SongType.playlistSong, inPlaylistLnMain
                );
                UIRenderFunctions.SetTilesImage(
                    lnIn, playlistSongs[i], 150, 100, 
                    inPlaylistButtonMargins, 17, 
                    inPlaylistNameMargins, 
                    scale, context
                );
                inPlaylistLnMain.AddView(lnIn);
            }
           

            inPlaylistScroll.AddView(inPlaylistLnMain);
            mainLayout?.AddView(inPlaylistScroll);
        }
        
        
    }
}