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
            
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inflater"></param>
        /// <param name="container"></param>
        /// <param name="savedInstanceState"></param>
        /// <returns></returns>
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.youtube_fragment, container, false);
            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.YoutubeFragmentMainRelative);
            
            webView = view?.FindViewById<WebView>(Resource.Id.webview);
            if (webView == null) return view;
            webView.Settings.JavaScriptEnabled = true;
            webView.SetWebViewClient(new HelloWebViewClient());
            webView.LoadUrl("https://www.youtube.com/");


            FloatingActionButton? downloadPopupShow = view?.FindViewById<FloatingActionButton>(Resource.Id.fab);
            Button? mp4 = view?.FindViewById<Button>(Resource.Id.mp4_btn);
            Button? musicBrainz = view?.FindViewById<Button>(Resource.Id.music_brainz_btn);
            Button? downloadCasual = view?.FindViewById<Button>(Resource.Id.download_basic_btn);

            if (downloadCasual != null) downloadCasual.Visibility = ViewStates.Invisible;
            if (mp4 != null) mp4.Visibility = ViewStates.Invisible;
            if (musicBrainz != null) musicBrainz.Visibility = ViewStates.Invisible;

            if (BlendMode.Multiply != null)
            {
                mp4?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
                musicBrainz?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
                downloadCasual?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
            }

            bool visiState = false;
            if (BlendMode.Multiply != null)
                downloadPopupShow?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
            if (downloadPopupShow != null)
            {
                downloadPopupShow.Click += delegate(object sender, EventArgs args)
                {
                    if (visiState)
                    {
                        if (downloadCasual != null) downloadCasual.Visibility = ViewStates.Invisible;
                        if (mp4 != null) mp4.Visibility = ViewStates.Invisible;
                        if (musicBrainz != null) musicBrainz.Visibility = ViewStates.Invisible;
                        visiState = false;
                    }
                    else if (webView.Url != null)
                        Downloader.Download(sender, args, webView.Url, DownloadActions.DownloadOnly);
                };

                downloadPopupShow.LongClick += delegate(object sender, View.LongClickEventArgs args)
                {
                    if (!visiState)
                    {
                        if (downloadCasual != null) downloadCasual.Visibility = ViewStates.Visible;
                        if (mp4 != null) mp4.Visibility = ViewStates.Visible;
                        if (musicBrainz != null) musicBrainz.Visibility = ViewStates.Visible;
                        visiState = true;
                    }
                    else
                    {
                        if (downloadCasual != null) downloadCasual.Visibility = ViewStates.Invisible;
                        if (mp4 != null) mp4.Visibility = ViewStates.Invisible;
                        if (musicBrainz != null) musicBrainz.Visibility = ViewStates.Invisible;
                    }
                };
            }

            if (downloadCasual != null)
                downloadCasual.Click += delegate(object sender, EventArgs args)
                {
                    if (webView.Url != null)
                        Downloader.Download(sender, args, webView.Url, DownloadActions.DownloadOnly);
                };
            if (mp4 != null)
                mp4.Click += delegate(object sender, EventArgs args)
                {
                    if (webView.Url != null)
                        Downloader.Download(sender, args, webView.Url, DownloadActions.Downloadmp4);
                };
            if (musicBrainz != null)
                musicBrainz.Click += delegate(object sender, EventArgs args)
                {
                    if (webView.Url != null)
                        Downloader.Download(sender, args, webView.Url, DownloadActions.DownloadWithMbid);
                };


            return view;
        }
        
        /// <summary>
        /// Update already existing popup, it won't close the instance
        /// </summary>
        /// <param name="songNameIn"></param>
        /// <param name="songArtistIn"></param>
        /// <param name="songAlbumIn"></param>
        /// <param name="imgArr"></param>
        /// <param name="originalAuthor"></param>
        /// <param name="originalTitle"></param>
        public static void UpdateSsDialog(string songNameIn, string songArtistIn, string songAlbumIn, byte[] imgArr, string originalAuthor, string originalTitle,  bool forw, bool back)
        {
            if (_bottomDialog is { IsShowing: true })
            {
                TextView? newLabel = _bottomDialog.FindViewById<TextView>(Resource.Id.SS_new_label);
                if (newLabel != null) newLabel.Visibility = ViewStates.Visible;

                if (_next != null) _next.Visibility = !forw ? ViewStates.Gone : ViewStates.Visible;

                if (_previous != null) _previous.Visibility = !back ? ViewStates.Gone : ViewStates.Visible;

                if (_ssImageLoading != null) _ssImageLoading.Visibility = ViewStates.Gone;
                if (_songImage != null)
                {
                    _songImage.Visibility = ViewStates.Visible;
                    using Bitmap? img = BitmapFactory.DecodeByteArray(imgArr, 0, imgArr.Length);
                    _songImage.SetImageBitmap(img);
                }

                if (_songArtist != null) _songArtist.Text = songArtistIn;
                if (_songAlbum != null) _songAlbum.Text = songAlbumIn;
                if (_songName != null) _songName.Text = songNameIn;
                if (_originalTitle != null) _originalTitle.Text = originalTitle;
                if (_originalArtist != null) _originalArtist.Text = originalAuthor;
            }
            else
            {
                SongSelectionDialog(songNameIn, songArtistIn, songAlbumIn, imgArr, originalAuthor, originalTitle, forw,
                    back);
            }
        }
        
        /// <summary>
        /// pop up for customizing downloaded song, choosing which image or name of song you want to use to save
        /// the song
        /// </summary>
        /// <param name="songNameIn"></param>
        /// <param name="songArtistIn"></param>
        /// <param name="songAlbumIn"></param>
        /// <param name="imgUrl"></param>
        /// <param name="forw"></param>
        /// <param name="back"></param>
        private static void SongSelectionDialog(string songNameIn, string songArtistIn, string songAlbumIn, byte[] imgArr, string originalAuthor, string originalTitle, bool forw, bool back)
        {
    
            _bottomDialog = new BottomSheetDialog(MainActivity.StateHandler.view);
            _bottomDialog.SetCanceledOnTouchOutside(false);
            LayoutInflater? ifl = LayoutInflater.From(MainActivity.StateHandler.view);
            View? view = ifl?.Inflate(Resource.Layout.song_download_selection_dialog, null);
            if (view != null) _bottomDialog.SetContentView(view);
            
            if (BlendMode.Multiply != null)
            {
                _previous = _bottomDialog.FindViewById<FloatingActionButton>(Resource.Id.previous_download);
                _previous?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
                _next = _bottomDialog.FindViewById<FloatingActionButton>(Resource.Id.next_download);
                _next?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
            }

            LinearLayout.LayoutParams ssfLoatingButtons = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            if (forw && back)
            {
                ssfLoatingButtons.SetMargins(1, 1, 1, 1);
                if (_next != null) _next.LayoutParameters = ssfLoatingButtons;
                if (_previous != null) _previous.LayoutParameters = ssfLoatingButtons;
            }
            else
            {
                ssfLoatingButtons.SetMargins(20, 20, 20, 20);
                if (_next != null) _next.LayoutParameters = ssfLoatingButtons;
                if (_previous != null) _previous.LayoutParameters = ssfLoatingButtons;
                
                if (!forw) { if (_next != null) _next.Visibility = ViewStates.Gone; }
                else { if (_next != null) _next.Visibility = ViewStates.Visible; }
                
                if (!back) { if (_previous != null) _previous.Visibility = ViewStates.Gone; }
                else { if (_previous != null) _previous.Visibility = ViewStates.Visible; }
            }

            

            
            _accept = _bottomDialog.FindViewById<TextView>(Resource.Id.accept_download);
            _reject = _bottomDialog.FindViewById<TextView>(Resource.Id.reject_download);

            _songImage = _bottomDialog.FindViewById<ImageView>(Resource.Id.to_download_song_image);
            _songName = _bottomDialog.FindViewById<TextView>(Resource.Id.song_to_download_name);
            _songArtist = _bottomDialog.FindViewById<TextView>(Resource.Id.song_to_download_artist);
            _songAlbum = _bottomDialog.FindViewById<TextView>(Resource.Id.song_to_download_album);
            _originalArtist = _bottomDialog.FindViewById<TextView>(Resource.Id.SS_original_title);
            _originalTitle = _bottomDialog.FindViewById<TextView>(Resource.Id.SS_orignal_artist);

            _ssImageLoading = _bottomDialog.FindViewById<ProgressBar>(Resource.Id.SS_image_loading);
            LinearLayout.LayoutParams ssLoadingImageParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent
            );
            ssLoadingImageParams.SetMargins(50, 50, 50, 50);
            
            if (_songArtist != null) _songArtist.Text = songArtistIn;
            if (_songAlbum != null) _songAlbum.Text = songAlbumIn;
            if (_songName != null) _songName.Text = songNameIn;
            if (_originalTitle != null) _originalTitle.Text = originalTitle;
            if (_originalArtist != null) _originalArtist.Text = originalAuthor;

            if (_previous != null)
            {
                _previous.Click += delegate
                {
                    MainActivity.StateHandler.songSelectionDialogAction = SongSelectionDialogActions.Previous;
                    MainActivity.StateHandler.ResultEvent.Set();
                };
            }

            if (_next != null)
            {
                
                _next.Click += delegate
                {
                    MainActivity.StateHandler.songSelectionDialogAction = SongSelectionDialogActions.Next;
                    MainActivity.StateHandler.ResultEvent.Set();
                    if (_songImage != null)
                    {
                        _songImage.SetImageResource(Resource.Color.mtrl_btn_transparent_bg_color);
                        _songImage.Visibility = ViewStates.Gone;
                    }
                    if (_ssImageLoading != null)
                    {
                        _ssImageLoading.Visibility = ViewStates.Visible;
                        _ssImageLoading.LayoutParameters = ssLoadingImageParams;
                    }

                    if (_songArtist != null) _songArtist.Text = "";
                    if (_songAlbum != null) _songAlbum.Text = "";
                    if (_songName != null) _songName.Text = "";
                    if (_originalTitle != null) _originalTitle.Text = "";
                    if (_originalArtist != null) _originalArtist.Text = "";

                    _next.Visibility = ViewStates.Gone;
                    if (_previous != null) _previous.Visibility = ViewStates.Gone;

                    TextView? newLabel = _bottomDialog.FindViewById<TextView>(Resource.Id.SS_new_label);
                    if (newLabel != null) newLabel.Visibility = ViewStates.Invisible;
                };
            }

            if (_accept != null)
                _accept.Click += delegate
                {
                    MainActivity.StateHandler.songSelectionDialogAction = SongSelectionDialogActions.Accept;
                    MainActivity.StateHandler.ResultEvent.Set();
                    _bottomDialog?.Hide();
                };
            if (_reject != null)
                _reject.Click += delegate
                {
                    MainActivity.StateHandler.songSelectionDialogAction = SongSelectionDialogActions.Cancel;
                    MainActivity.StateHandler.ResultEvent.Set();
                    _bottomDialog?.Hide();
                };

            //Picasso.Get().Load(imgUrl).Placeholder(Resource.Drawable.no_repeat).Error(Resource.Drawable.shuffle2).Into(_songImage);
            using Bitmap? img = BitmapFactory.DecodeByteArray(imgArr, 0, imgArr.Length);
            _songImage?.SetImageBitmap(img);
            //Glide.With(MainActivity.stateHandler.view).Load(imgUrl).Error(Resource.Drawable.shuffle2).Into(_songImage);

            _bottomDialog.Show();
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