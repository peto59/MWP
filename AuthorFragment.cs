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
    /// Author fragment, initiated after one of the author buttons is clicked in the AlbumAuthorFragment
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AuthorFragment : Fragment
    {
        private readonly float scale;
        private readonly Context context;
        private RelativeLayout mainLayout;
        private Artist artist;

        private Dictionary<LinearLayout, object> albumButtons = new Dictionary<LinearLayout, object>();
        private Dictionary<LinearLayout, int> songButtons = new Dictionary<LinearLayout, int>();



        /// <summary>
        /// Constructor for AuthorFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (insert "this")</param>
        public AuthorFragment(Context ctx)
        {
            context = ctx;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
        }

        /// <summary>
        ///  AuthorFragment On create view
        /// </summary>
        /// <param name="inflater"></param>
        /// <param name="container"></param>
        /// <param name="savedInstanceState"></param>
        /// <returns></returns>
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.author_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.author_fragment_main);

            string title = Arguments.GetString("title");
            List<Artist> retreivedSongs = MainActivity.stateHandler.Artists.Search(title);
            if (retreivedSongs.Count > 0)
            {
                artist = retreivedSongs[0];
            }
            Console.WriteLine("FOUNDED SEARCHED ARTIST NAME IN FRAGMENT: " + artist.Title);
            
            RenderAlbumsSongs();

            TextView texxx = new TextView(context);
            texxx.TextSize = 30;
            texxx.Text = "idkk just click";
            texxx.Click += (sender, args) =>
                ((AllSongs)Activity).ReplaceFragments(AllSongs.FragmentType.AlbumFrag, "Smile");
            // mainLayout?.AddView(texxx);

            return view;
        }


        private void UncategorizedSongsRender()
        {
            UIRenderFunctions.FragmentPositionObject = artist.Albums.Select("Uncategorized")[0];
            
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

            
            
            for (int i = 0; i < artist.Albums.Select("Uncategorized")[0].Songs.Count; i++)
            {

                LinearLayout lnIn = UIRenderFunctions.PopulateHorizontal(
                    artist.Albums.Select("Uncategorized")[0].Songs[i], scale,
                    150, 100,
                    buttonMargins, nameMargins, cardMargins,
                    17,
                    i, context, songButtons, UIRenderFunctions.SongType.albumSong, lnMain
                );
                UIRenderFunctions.SetTilesImage(
                    lnIn, artist.Albums.Select("Uncategorized")[0].Songs[i],  150, 100,
                    buttonMargins, 17,
                    nameMargins, scale, context
                );
                lnMain.AddView(lnIn);
            }

           
            songsScroll.AddView(lnMain);
            mainLayout?.AddView(songsScroll);
        }

        private void RenderAlbumsSongs()
        {
            HorizontalScrollView hr = new HorizontalScrollView(context);
            int hrHeight = (int)(240 * scale + 0.5f);;

            /*
            if (artist.Albums.Count < 2)
                hrHeight = 0;
            else
                hrHeight = (int)(240 * scale + 0.5f);
            */
            RelativeLayout.LayoutParams hrParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                hrHeight
            );
            hrParams.SetMargins(0, 150, 0, 0);
            hrParams.AddRule(LayoutRules.Below, Resource.Id.toolbar1);
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
            for (int i = 0; i < artist.Albums.Count; i++)
            {
                if (artist.Albums[i].Showable && artist.Albums.Count > 1)
                {
                    LinearLayout lnIn = UIRenderFunctions.PopulateVertical(
                        artist.Albums[i], scale, cardMargins, 15, i, context, albumButtons, Activity);
                    UIRenderFunctions.SetTilesImage(
                        lnIn, artist.Albums[i], 150, 100,
                        buttonMargins, 17,
                        nameMargins, scale, context
                    );
                    lin.AddView(lnIn);
                }

            }

            
            if (artist.Albums.Count > 1)
            {
                hr.AddView(lin);
                mainLayout?.AddView(hr);
                UncategorizedSongsRender();
            }
            else
                UncategorizedSongsRender();
        }

    }
}