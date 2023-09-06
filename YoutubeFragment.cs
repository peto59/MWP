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


namespace Ass_Pain
{
    public class YoutubeFragment : Fragment
    {
        private readonly Context context;
        private RelativeLayout mainLayout;
        private readonly float scale;
        private WebView webView;

        private static FloatingActionButton _previous;
        private static FloatingActionButton _next;
        private static TextView _accept;
        private static TextView _reject;

        private static BottomSheetDialog _bottomDialog;
        private static ImageView _songImage;
        private static TextView _songName;
        private static TextView _songArtist;
        private static TextView _songAlbum;
        
            
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inflater"></param>
        /// <param name="container"></param>
        /// <param name="savedInstanceState"></param>
        /// <returns></returns>
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.youtube_fragment, container, false);
            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.YoutubeFragmentMainRelative);
            
            webView = view?.FindViewById<WebView>(Resource.Id.webview);
            webView.Settings.JavaScriptEnabled = true;
            webView.SetWebViewClient(new HelloWebViewClient());
            webView.LoadUrl("https://www.youtube.com/");
       

            FloatingActionButton downloadPopupShow = view?.FindViewById<FloatingActionButton>(Resource.Id.fab);
            Button mp4 = view?.FindViewById<Button>(Resource.Id.mp4_btn);
            Button musicBrainz = view?.FindViewById<Button>(Resource.Id.music_brainz_btn);
            Button downloadCasual = view?.FindViewById<Button>(Resource.Id.download_basic_btn);

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
                    else
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

            if (downloadCasual != null) downloadCasual.Click += delegate(object sender, EventArgs args)
            {
                Downloader.Download(sender, args, webView.Url, DownloadActions.DownloadOnly);
            };
            if (mp4 != null) mp4.Click += delegate(object sender, EventArgs args)
            {
                Downloader.Download(sender, args, webView.Url, DownloadActions.Downloadmp4);
            };
            if (musicBrainz != null) musicBrainz.Click += delegate(object sender, EventArgs args)
            {
                Downloader.Download(sender, args, webView.Url, DownloadActions.DownloadWithMbid);
            };
            
            
            return view;
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
        public static void SongSelectionDialog(string songNameIn, string songArtistIn, string songAlbumIn, byte[] imgArr, string originalAuthor, string originalTitle, bool forw, bool back)
        {

            _bottomDialog = new BottomSheetDialog(MainActivity.stateHandler.view);
            LayoutInflater ifl = LayoutInflater.From(MainActivity.stateHandler.view);
            View view = ifl?.Inflate(Resource.Layout.song_download_selection_dialog, null);
            if (view != null) _bottomDialog.SetContentView(view);
            
            if (BlendMode.Multiply != null)
            {
                _previous = _bottomDialog?.FindViewById<FloatingActionButton>(Resource.Id.previous_download);
                _previous?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
                _next = _bottomDialog?.FindViewById<FloatingActionButton>(Resource.Id.next_download);
                _next?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
            }

            if (!forw) { if (_next != null) _next.Visibility = ViewStates.Invisible; }
            else { if (_next != null) _next.Visibility = ViewStates.Visible; }
            
            if (!back) { if (_previous != null) _previous.Visibility = ViewStates.Invisible; }
            else { if (_previous != null) _previous.Visibility = ViewStates.Visible; }

            
            _accept = _bottomDialog?.FindViewById<TextView>(Resource.Id.accept_download);
            _reject = _bottomDialog?.FindViewById<TextView>(Resource.Id.reject_download);

            _songImage = _bottomDialog?.FindViewById<ImageView>(Resource.Id.to_download_song_image);
            _songName = _bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_name);
            _songArtist = _bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_artist);
            _songAlbum = _bottomDialog?.FindViewById<TextView>(Resource.Id.song_to_download_album);
            
            if (_songArtist != null) _songArtist.Text = songArtistIn;
            if (_songAlbum != null) _songAlbum.Text = songAlbumIn;
            if (_songName != null) _songName.Text = songNameIn;

            _previous.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Previous;
                MainActivity.stateHandler.ResultEvent.Set();
                _bottomDialog?.Hide();
            };
            _next.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Next;
                MainActivity.stateHandler.ResultEvent.Set();
                _bottomDialog?.Hide();
            };
            
            _accept.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Accept;
                MainActivity.stateHandler.ResultEvent.Set();
                _bottomDialog?.Hide();
            };
            _reject.Click += delegate
            {
                MainActivity.stateHandler.songSelectionDialogAction = SongSelectionDialogActions.Cancel;
                MainActivity.stateHandler.ResultEvent.Set();
                _bottomDialog?.Hide();
            };
            
            //Picasso.Get().Load(imgUrl).Placeholder(Resource.Drawable.no_repeat).Error(Resource.Drawable.shuffle2).Into(_songImage);
            using Bitmap img = BitmapFactory.DecodeByteArray(imgArr, 0, imgArr.Length);
            _songImage.SetImageBitmap(img);
            //Glide.With(MainActivity.stateHandler.view).Load(imgUrl).Error(Resource.Drawable.shuffle2).Into(_songImage);

            _bottomDialog?.Show();


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