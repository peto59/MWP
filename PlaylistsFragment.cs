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
    public class PlaylistsFragment : Fragment
    {
        private readonly Context context;
        private RelativeLayout mainLayout;


        private PlaylistFragment playlistFragment; 
            
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inflater"></param>
        /// <param name="container"></param>
        /// <param name="savedInstanceState"></param>
        /// <returns></returns>
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.playlists_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.playlists_fragment_main);

            RenderPlaylists();
            
            return view;
        }

        /// <summary>
        /// Constructor for SongsFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (e.g. "this")</param>
        public PlaylistsFragment(Context ctx)
        {
            context = ctx;
            playlistFragment = new PlaylistFragment(ctx);
        }


        private void RenderPlaylists()
        {
            ScrollView playlistsScroll = new ScrollView(context);
            RelativeLayout.LayoutParams playlistScrollParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            playlistScrollParams.SetMargins(0, 150, 0, 0);
            playlistsScroll.LayoutParameters = playlistScrollParams;


            LinearLayout playlistLnMain = new LinearLayout(context);
            playlistLnMain.Orientation = Orientation.Vertical;
            RelativeLayout.LayoutParams playlistLnMainParams = new RelativeLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent
            );
            playlistLnMainParams.SetMargins(20, 20, 20, 20);
            playlistLnMain.LayoutParameters = playlistLnMainParams;
            
            int[] playlistCardMargins = { 0, 50, 0, 0 };


            List<string> playlists = FileManager.GetPlaylist();
            Parallel.ForEach(playlists, playlist =>
            {

                LinearLayout lnIn = new LinearLayout(context);
                lnIn.Orientation = Orientation.Vertical;
                lnIn.SetBackgroundResource(Resource.Drawable.rounded);

                LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                );
                lnInParams.SetMargins(
                    playlistCardMargins[0], playlistCardMargins[1], 
                    playlistCardMargins[2], playlistCardMargins[3]
                );
                lnIn.LayoutParameters = lnInParams;


                TextView plaName = new TextView(context);
                LinearLayout.LayoutParams nameParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                );
                nameParams.SetMargins(0, 20, 0, 20);
                plaName.LayoutParameters = nameParams; 
                plaName.Text = playlist;
                plaName.SetTextColor(Color.White);
                plaName.TextSize = 30;
                plaName.TextAlignment = TextAlignment.Center;

                TextView songsCount = new TextView(context);
                LinearLayout.LayoutParams countParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                );
                countParams.SetMargins(0, 20, 0, 20);
                songsCount.LayoutParameters = countParams;
                songsCount.Text = $"number of songs: {FileManager.GetPlaylist(playlist).Count}";
                songsCount.SetTextColor(Color.White);
                songsCount.TextSize = 15;
                songsCount.TextAlignment = TextAlignment.Center;


                lnIn.Click += (_, _) =>
                {
                    var fragmentTransaction = ParentFragmentManager.BeginTransaction();
                    Bundle bundle = new Bundle();
                    bundle.PutString("title", playlist);
                    
                    playlistFragment.Arguments = bundle;
                    fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, playlistFragment);
                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                };
                

                lnIn.AddView(plaName);
                lnIn.AddView(songsCount);

                playlistLnMain.AddView(lnIn);

            });
           

            playlistsScroll.AddView(playlistLnMain);
            mainLayout?.AddView(playlistsScroll);
        }
        
        
    }
}