using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using Android.Content.Res;
using Android.Graphics;
using Android.Text.Style;
using Android.Util;
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

        private Dictionary<LinearLayout, object> albumButtons = new Dictionary<LinearLayout, object>();

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
        }
        
        /// <summary>
        ///  AlbumAuthorFragment On create view
        /// </summary>
        /// <param name="inflater"></param>
        /// <param name="container"></param>
        /// <param name="savedInstanceState"></param>
        /// <returns></returns>
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.album_author_fragment, container, false);
            
            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.album_author_fragment_main);

            SetupStructure();
            
            return view;
        }
        
        
        
        
        
        
        
        private LinearLayout AlbumTiles()
        {

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

            for (int i = 0; i < MainActivity.StateHandler.Albums.Count; i++)
            {
                LinearLayout lnIn = UIRenderFunctions.PopulateVertical(
                    MainActivity.StateHandler.Albums[i], scale, cardMargins, 15, i, context, albumButtons, 
                    ParentFragmentManager, assets, albumFragment, authorFragment);
                UIRenderFunctions.SetTilesImage(
                    lnIn, MainActivity.StateHandler.Albums[i], 150, 100,
                    buttonMargins, 17,
                    nameMargins, scale, context
                );
                
                lin.AddView(lnIn);
            }
            

            return lin;
        }


        private LinearLayout AuthorTiles()
        {
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


            for (int i = 0; i < MainActivity.StateHandler.Artists.Count; i++)
            {
                LinearLayout lnIn = UIRenderFunctions.PopulateVertical(
                    MainActivity.StateHandler.Artists[i], scale, cardMargins, 15, i, context, albumButtons, 
                    ParentFragmentManager, assets, albumFragment, authorFragment);
                UIRenderFunctions.SetTilesImage(
                    lnIn, MainActivity.StateHandler.Artists[i], 150, 100,
                    buttonMargins, 17,
                    nameMargins, scale, context
                );
                //全部加える
                lin.AddView(lnIn);

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
                
                LinearLayout labelsForScrolls = new LinearLayout(context);
                LinearLayout.LayoutParams labelForScrollsParams = new LinearLayout.LayoutParams(
                    displayWidth,
                    (int)(500 * scale + 0.5f)
                );
                labelForScrollsParams.SetMargins(50, 40, 0, 150);
                labelsForScrolls.LayoutParameters = labelForScrollsParams;
                labelsForScrolls.SetHorizontalGravity(GravityFlags.Center);
                TextView authorLabel = new TextView(context);
                authorLabel.SetPadding(0, 0, 175, 0);
                authorLabel.Text = "Authors";
                authorLabel.TextSize = 20;
                authorLabel.SetTextColor(Color.White);
                TextView albumsLabel = new TextView(context);
                albumsLabel.SetPadding(175, 0, 0, 0);
                albumsLabel.Text = "Albums";
                albumsLabel.TextSize = 20;
                albumsLabel.SetTextColor(Color.White);
                    
                labelsForScrolls.AddView(authorLabel);
                labelsForScrolls.AddView(albumsLabel);
                mainLayout?.AddView(labelsForScrolls);
                
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