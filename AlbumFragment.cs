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
using Java.Util;
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

        private Dictionary<string, LinearLayout> songTilesBuffer;
        private ObservableDictionary<string, Bitmap> songImagesBuffer;


        /// <summary>
        /// Constructor for AlbumFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (insert "this")</param>
        public AlbumFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            this.assets = assets;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;

            songTilesBuffer = new Dictionary<string, LinearLayout>();
            songImagesBuffer = new ObservableDictionary<string, Bitmap>();
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


            songImagesBuffer.ValueChanged += delegate
            {
                ((Activity)context).RunOnUiThread(() =>
                {
                    string last = songImagesBuffer.Items.Keys.Last();
                    LinearLayout child = songTilesBuffer?[last] ?? new LinearLayout(context);
                    if (assets != null)
                        UIRenderFunctions.LoadSongImageFromBuffer(child, songImagesBuffer, assets);
                });
            };
            
            RenderSongs();

            return view;
        }

        
        /// <inheritdoc />
        public override void OnViewCreated(View view, Bundle? savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            Task.Run(async () =>
            {
                await UIRenderFunctions.LoadSongImages(album.Songs, songImagesBuffer,
                    UIRenderFunctions.LoadImageType.SONG);
                
                UIRenderFunctions.FillImageHoles(context, songTilesBuffer, songImagesBuffer, assets);
            });
           
        }
        
        
        

        private void RenderSongs()
        {
            songTilesBuffer = new Dictionary<string, LinearLayout>();
            
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
                if (songTilesBuffer.TryAdd(album.Songs[i].Title, lnIn)) 
                    lnMain.AddView(lnIn);
                    
                    
            }

           
            songsScroll.AddView(lnMain);
            mainLayout?.AddView(songsScroll);
                
        }
        

    }
}