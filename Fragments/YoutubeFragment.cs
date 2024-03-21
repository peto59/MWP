using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidX.Fragment.App;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.FloatingActionButton;
using MWP.BackEnd;
using MWP.DatatypesAndExtensions;


namespace MWP
{
    public class YoutubeFragment : Fragment
    {
        private readonly Context context;
        private RelativeLayout? mainLayout;
        private readonly float scale;
        private WebView? webView;

        private static FloatingActionButton? _previous;
        private static FloatingActionButton? _next;
        private static TextView? _accept;
        private static TextView? _reject;

        private static BottomSheetDialog? _bottomDialog;
        private static ImageView? _songImage;
        private static TextView? _songName;
        private static TextView? _songArtist;
        private static TextView? _songAlbum;
        private static TextView? _originalArtist;
        private static TextView? _originalTitle;

        private static ProgressBar? _ssImageLoading;


        /// <inheritdoc />
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.youtube_fragment, container, false);
            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.YoutubeFragmentMainRelative);
            return view;
        }
        
       
        

        /// <summary>
        /// Constructor for SongsFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (e.g. "this")</param>
        public YoutubeFragment(Context ctx)
        {
            context = ctx;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
        }

        
    }
}