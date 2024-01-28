using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Android.Content.Res;
using Android.Graphics;
using Bitmap = Android.Graphics.Bitmap;
using Color = Android.Graphics.Color;
using Fragment = AndroidX.Fragment.App.Fragment;
using Orientation = Android.Widget.Orientation;
#if DEBUG
using MWP.Helpers;
#endif


namespace MWP
{
    /// <summary>
    /// Author fragment, initiated after one of the author buttons is clicked in the AlbumAuthorFragment
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AuthorFragment : Fragment
    {
        private readonly float scale;
        private readonly Context context;
        private LinearLayout? mainLayout;
        private Artist artist = null!;
        private AssetManager? assets;
        private Typeface? font;

        private Dictionary<LinearLayout?, object> albumButtons = new Dictionary<LinearLayout?, object>();
        private Dictionary<LinearLayout?, Guid> songButtons = new Dictionary<LinearLayout?, Guid>();

        private ObservableDictionary<string, Bitmap>? albumImagesBuffer;
        private ObservableDictionary<string, Bitmap>? songImagesBuffer;
        private Dictionary<string, LinearLayout?>? albumTilesBuffer;
        private Dictionary<string, LinearLayout?>? songTilesBuffer;

        private AlbumFragment albumFragment;

        /// <summary>
        /// Constructor for AuthorFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (insert "this")</param>
        /// <param name="assets"></param>
        public AuthorFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            this.assets = assets;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
            albumFragment = new AlbumFragment(ctx, this.assets);
            
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
            
            albumImagesBuffer = new ObservableDictionary<string, Bitmap>();
            songImagesBuffer = new ObservableDictionary<string, Bitmap>();
            albumTilesBuffer = new Dictionary<string, LinearLayout?>();
            songTilesBuffer = new Dictionary<string, LinearLayout?>();
        }

        /// <summary>
        ///  AuthorFragment On create view
        /// </summary>
        /// <param name="inflater"></param>
        /// <param name="container"></param>
        /// <param name="savedInstanceState"></param>
        /// <returns></returns>
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.author_fragment, container, false);

            mainLayout = view?.FindViewById<LinearLayout>(Resource.Id.author_fragment_main);

            string? title = Arguments?.GetString("title");
            List<Artist> retreivedSongs = MainActivity.StateHandler.Artists.Search(title);
            if (retreivedSongs.Count > 0)
            {
                artist = retreivedSongs[0];
            }
            
            RenderAlbumsSongs();

            // NOTE: It's just loading song images it does not have ui for, soo just comment this thread if
            // you are commenting uncategorized songs, or making the ui not load for uncategorized songs. 

            if (songImagesBuffer != null)
                songImagesBuffer.ValueChanged += (_, _) =>
                {
                    ((Activity)context).RunOnUiThread(() =>
                    {
                        string last = songImagesBuffer.Items.Keys.Last();

                        LinearLayout child = songTilesBuffer?[last] ?? new LinearLayout(context);
                        if (assets != null)
                            UIRenderFunctions.LoadSongImageFromBuffer(child, songImagesBuffer, assets);
                    });
                };


            if (albumImagesBuffer != null)
                albumImagesBuffer.ValueChanged += (_, _) =>
                {
                    ((Activity)context).RunOnUiThread(() =>
                    {
                        string last = albumImagesBuffer.Items.Keys.Last();
#if DEBUG
                        MyConsole.WriteLine($"{last}");
#endif
                        if (!last.Equals("Uncategorized"))
                        {
                            LinearLayout child = albumTilesBuffer?[last] ?? new LinearLayout(context);
                            if (assets != null)
                                UIRenderFunctions.LoadSongImageFromBuffer(child, albumImagesBuffer, assets);
                        }
                    });
                };


            return view;
        }

        
        
        /// <inheritdoc />
        public override void OnViewCreated(View view, Bundle? savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            
            Task.Run(async () =>
            {
                await UIRenderFunctions.LoadSongImages(artist.Albums.Select("Uncategorized")[0].Songs, songImagesBuffer,
                    UIRenderFunctions.LoadImageType.SONG);
                
                UIRenderFunctions.FillImageHoles(context, songTilesBuffer, songImagesBuffer, assets);
            });
            
            
            Task.Run(async () =>
            {
                await UIRenderFunctions.LoadSongImages(artist.Albums, albumImagesBuffer,
                    UIRenderFunctions.LoadImageType.ALBUM);
                
                UIRenderFunctions.FillImageHoles(context, albumTilesBuffer, albumImagesBuffer, assets);
            });
        }

        
        
        //TODO: Very big problem error, the key uncategorized cant be found in dictionary, fix!!!
        
        private void UncategorizedSongsRender(bool visible)
        {

            UIRenderFunctions.FragmentPositionObject = artist.Albums.Select("Uncategorized")[0];
            songTilesBuffer = new Dictionary<string, LinearLayout?>();
            
            ScrollView songsScroll = new ScrollView(context);
            LinearLayout.LayoutParams songsScrollParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            songsScrollParams.SetMargins(0, visible ? (int)(0 * scale + 0.5f) : (int)(50 * scale + 0.5f), 0, 0);
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
            int[] cardMargins = { 0, 0, 0, 50 };


            List<Song> uncategorized = artist.Albums.Select("Uncategorized")[0].Songs;
            for (int i = 0; i < uncategorized.Count; i++)
            {
                
                LinearLayout? lnIn = UIRenderFunctions.PopulateHorizontal(
                    uncategorized[i], scale,
                    150, 100,
                    buttonMargins, nameMargins, cardMargins,
                    17,
                    context, songButtons, UIRenderFunctions.SongType.AlbumSong, assets, ParentFragmentManager, lnMain
                );
                if (songTilesBuffer.TryAdd(uncategorized[i].Title, lnIn))
                    lnMain.AddView(lnIn);
            }

           
            songsScroll.AddView(lnMain);
            mainLayout?.AddView(songsScroll);
        }

        private void RenderAlbumsSongs()
        {
            albumTilesBuffer = new Dictionary<string, LinearLayout?>();
            
            HorizontalScrollView hr = new HorizontalScrollView(context);
            int hrHeight = (int)(240 * scale + 0.5f);
            
            LinearLayout.LayoutParams hrParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                hrHeight
            );
            hrParams.SetMargins(0, 20, 0, 0);
            hr.LayoutParameters = hrParams;

            LinearLayout lin = new LinearLayout(context);
            lin.Orientation = Orientation.Horizontal;
            LinearLayout.LayoutParams linParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent
            );
            lin.LayoutParameters = linParams;
            
            int[] buttonMargins = { 50, 50, 50, 0 };
            int[] nameMargins = { 50, 50, 50, 50 };
            int[] cardMargins = { 40, 50, 0, 0 };

            int index = 0;
            foreach (var album in artist.Albums)
            {
                if (album.Showable && artist.Albums.Count > 1)
                {
                    
                    LinearLayout? lnIn = UIRenderFunctions.PopulateVertical(
                        album, scale, 
                        150, 100, buttonMargins, cardMargins, nameMargins, 15, index, 
                        context, albumButtons, ParentFragmentManager, assets, albumFragment);
                    if (albumTilesBuffer.TryAdd(album.Title, lnIn))
                    {
                        lin.AddView(lnIn);
                        index++;
                    }
                }
            }

            TextView labelUncategorized = new TextView(context);
            labelUncategorized.Text = "Uncategorized Songs";
            labelUncategorized.TextSize = 20;
            labelUncategorized.Typeface = font;
            labelUncategorized.SetTextColor(new Color(0, 0, 0));
            LinearLayout.LayoutParams labelUncategorizedParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            labelUncategorizedParams.SetMargins(20, 50, 20, 50);
            labelUncategorized.LayoutParameters = labelUncategorizedParams;
            labelUncategorized.SetBackgroundResource(Resource.Drawable.rounded_view_white);
            labelUncategorized.SetPadding(30, 30, 30, 30);
            
            
            TextView labelAthorsAlbum = new TextView(context);
            labelAthorsAlbum.Text = $"{artist.Title}'s Albums";
            labelAthorsAlbum.TextSize = 20;
            labelAthorsAlbum.Typeface = font;
            labelAthorsAlbum.SetTextColor(new Color(0, 0, 0));
            LinearLayout.LayoutParams labelAthorsAlbumParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            labelAthorsAlbumParams.SetMargins(20, 50, 20, 0);
            labelAthorsAlbum.LayoutParameters = labelAthorsAlbumParams;
            labelAthorsAlbum.SetBackgroundResource(Resource.Drawable.rounded_view_white);
            labelAthorsAlbum.SetPadding(30, 30, 30, 30);
            
            if (artist.Albums.Count > 1)
            {
                hr.AddView(lin);
                mainLayout?.AddView(labelAthorsAlbum);
                mainLayout?.AddView(hr);
                mainLayout?.AddView(labelUncategorized);
                UncategorizedSongsRender(true);
            }
            else
            {
                mainLayout?.AddView(labelUncategorized);
                UncategorizedSongsRender(false);
            }
            
        }

    }
}