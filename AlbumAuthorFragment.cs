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
using Android.Text.Style;
using Android.Util;
using Java.Util;
using MWP.Helpers;
using Fragment = AndroidX.Fragment.App.Fragment;
using Orientation = Android.Widget.Orientation;


namespace MWP
{
    
    /// <summary>
    /// Fragment view for Albums and Authors
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AlbumAuthorFragment : Fragment
    {
        private readonly float scale;
        private readonly Context context;
        private RelativeLayout? mainLayout;
        private AssetManager? assets;

        private Dictionary<LinearLayout?, object> albumButtons = new Dictionary<LinearLayout?, object>();

        private Dictionary<string, LinearLayout?> albumTilesBuffer;
        private Dictionary<string, LinearLayout?> authorTilesBuffer;
        private ObservableDictionary<string, Bitmap> albumImagesBuffer;
        private ObservableDictionary<string, Bitmap> authorImagesBuffer;

        private ObservableInteger UIdone;
        
        private AlbumFragment albumFragment;
        private AuthorFragment authorFragment;
        
        
        /// <summary>
        /// Constructor for AlbumAuthorFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (e.g. "this")</param>
        public AlbumAuthorFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            this.assets = assets;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
            
            // fragmentManager = ParentFragment.Activity.SupportFragmentManager;
            albumFragment = new AlbumFragment(context, this.assets);
            authorFragment = new AuthorFragment(context, this.assets);

            albumTilesBuffer = new Dictionary<string, LinearLayout?>();
            albumImagesBuffer = new ObservableDictionary<string, Bitmap>();

            authorImagesBuffer = new ObservableDictionary<string, Bitmap>();
            authorTilesBuffer = new Dictionary<string, LinearLayout?>();

            UIdone = new ObservableInteger();
            
        }


        /// <inheritdoc />
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.album_author_fragment, container, false);
            
            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.album_author_fragment_main);


            
            albumImagesBuffer.ValueChanged += delegate
            {
                ((Activity)context).RunOnUiThread(() => 
                {
                    string last = albumImagesBuffer.lastValueAdded;
                    LinearLayout? child = albumTilesBuffer?[last] ?? new LinearLayout(context);
                    if (assets != null)
                        UIRenderFunctions.LoadSongImageFromBuffer(child, albumImagesBuffer, assets);
                });
            };
            
            authorImagesBuffer.ValueChanged += delegate
            {
                ((Activity)context).RunOnUiThread(() => 
                {
                    string last = authorImagesBuffer.lastValueAdded;
                    LinearLayout? child = authorTilesBuffer?[last] ?? new LinearLayout(context);
                    if (assets != null)
                        UIRenderFunctions.LoadSongImageFromBuffer(child, authorImagesBuffer, assets);
                });
            };
            
            
            
            SetupStructure();
            

            
            return view;
        }


        /// <inheritdoc />
        public override void OnViewCreated(View view, Bundle? savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            
                 
            Task.Run(async () =>
            {
                await UIRenderFunctions.LoadSongImages(MainActivity.StateHandler.Albums, albumImagesBuffer,
                    UIRenderFunctions.LoadImageType.ALBUM);

                ((Activity)context).RunOnUiThread(() =>
                {
                    foreach (var tile in albumTilesBuffer)
                    {
                        ImageView? vv = (ImageView)tile.Value.GetChildAt(0);
                        if (vv?.Drawable == null)
                            UIRenderFunctions.LoadSongImageFromBuffer(tile.Value, albumImagesBuffer, assets);
                    }
                });
            });
            Task.Run(async () =>
            {
                await UIRenderFunctions.LoadSongImages(MainActivity.StateHandler.Artists, authorImagesBuffer,
                    UIRenderFunctions.LoadImageType.AUTHOR);
                
                ((Activity)context).RunOnUiThread(() =>
                {
                    foreach (var tile in authorTilesBuffer)
                    {
                        ImageView? vv = (ImageView)tile.Value.GetChildAt(0);
                        if (vv?.Drawable == null)
                            UIRenderFunctions.LoadSongImageFromBuffer(tile.Value, authorImagesBuffer, assets);
                    }
                });
            });

          
            
        }


        private LinearLayout AlbumTiles()
        {
            albumTilesBuffer = new Dictionary<string, LinearLayout?>();
            
            LinearLayout lin = new LinearLayout(context);
            lin.Orientation = Orientation.Vertical;
            LinearLayout.LayoutParams linParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent
                );
            lin.LayoutParameters = linParams;


            int[] buttonMargins = { 50, 50, 50, 0 };
            int[] nameMargins = { 50, 50, 50, 50 };
            int[] cardMargins = { 30, 50, 0, 0 };

            var albums = MainActivity.StateHandler.Albums;
            for (int i = 0; i < albums.Count; i++)
            {
                LinearLayout? lnIn = UIRenderFunctions.PopulateVertical(
                    albums[i], 
                    scale, 150, 100, buttonMargins,cardMargins, nameMargins, 15, i, 
                    context, albumButtons, 
                    ParentFragmentManager, assets, albumFragment, authorFragment);
                if (albumTilesBuffer.TryAdd(albums[i].Title, lnIn))
                {
                    lin.AddView(lnIn);
                }
            }
            
            
            decimal percentage = ((decimal)albumImagesBuffer.Items.Count / (decimal)albumTilesBuffer.Count) * 100;
            if (percentage > 80)
            {
                for (int i = 0; i < albums.Count; i++)
                {
                    LinearLayout? child = albumTilesBuffer[albums[i].Title];
                    if (assets != null) UIRenderFunctions.LoadSongImageFromBuffer(child, albumImagesBuffer, assets);
                }
            }
            

            return lin;
        }


        private LinearLayout AuthorTiles()
        {
            authorTilesBuffer = new Dictionary<string, LinearLayout?>();
            
            LinearLayout lin = new LinearLayout(context);
            lin.Orientation = Orientation.Vertical;
            LinearLayout.LayoutParams linParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent
            );
            lin.LayoutParameters = linParams;
            
            int[] buttonMargins = { 50, 50, 50, 0 };
            int[] nameMargins = { 50, 50, 50, 50 };
            int[] cardMargins = { 20, 50, 0, 0 };

            List<Artist> artists = MainActivity.StateHandler.Artists;
            for (int i = 0; i < artists.Count; i++)
            {
                LinearLayout? lnIn = UIRenderFunctions.PopulateVertical(
                    MainActivity.StateHandler.Artists[i], scale, 150, 100, buttonMargins ,cardMargins, nameMargins, 15, i, context, albumButtons, 
                    ParentFragmentManager, assets, albumFragment, authorFragment);
                if (authorTilesBuffer.TryAdd(artists[i].Title, lnIn))
                {
                    lin.AddView(lnIn);
                }
            } 
            
            decimal percentage = ((decimal)authorImagesBuffer.Items.Count / (decimal)authorTilesBuffer.Count) * 100;
            if (percentage > 80)
            {
                for (int i = 0; i < artists.Count; i++)
                {
                    LinearLayout? child = authorTilesBuffer[artists[i].Title];
                    if (assets != null) UIRenderFunctions.LoadSongImageFromBuffer(child, authorImagesBuffer, assets);
                }
            }

            return lin;
        }


        private void SetupStructure()
        {
            ScrollView authorScroll = new ScrollView(context);
            ScrollView albumScroll = new ScrollView(context);
            
            DisplayMetrics? displayMetrics = Resources.DisplayMetrics;
            if (displayMetrics != null)
            {
                int displayWidth = displayMetrics.WidthPixels;
                
               
                
                //作家
                // ScrollView author_scroll = new ScrollView(this);
                RelativeLayout.LayoutParams authorScrollParams = new RelativeLayout.LayoutParams(
                    displayWidth / 2,
                    ViewGroup.LayoutParams.MatchParent
                );
                authorScrollParams.SetMargins(0, 100, 0, 0);
                authorScroll.LayoutParameters = authorScrollParams;

                LinearLayout authorLin = AuthorTiles();
                authorScroll.AddView(authorLin);
                mainLayout?.AddView(authorScroll);



                //アルブム
                // ScrollView album_scroll = new ScrollView(this);
                RelativeLayout.LayoutParams albumScrollParams = new RelativeLayout.LayoutParams(
                    displayWidth / 2,
                    ViewGroup.LayoutParams.MatchParent
                );
                albumScrollParams.SetMargins((displayWidth / 2) - 20, 100, 0, 0);
                albumScroll.LayoutParameters = albumScrollParams;
            }

            LinearLayout albumLin = AlbumTiles();
            albumScroll.AddView(albumLin);
            mainLayout?.AddView(albumScroll);
            
            
            
        }
    }
}