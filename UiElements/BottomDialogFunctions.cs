using Android.Graphics;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.FloatingActionButton;
using MWP.DatatypesAndExtensions;

namespace MWP
{
    /// <summary>
    /// Trieda obsahuje metódy potrebné na vytvorenie BottomSheetDialog-u. V rámci dialógu má používateľ k dispozícii
    /// prezeranie si rôzne možnosti metadát pre istú skladby. Pri prvotnom načítaní aplikácie má používateľ možnosť
    /// si vybrať automatické vyhľadávanie metadát pre skladby v rámci zariadenia, čo spôsobí vyhľadávanie metadát skladieb na internete
    /// ak je internet dostupný. V prípade, že sa nájdu viaceré zdroje metadát pre skladbu. Používateľ je prerušený z používania aplikácie
    /// a je prezentovanú dialógom pre výber z niekoľkých možností náhľadového obrázka a iného názvu skladby a interpreta ak sú dostupné. Používateľ
    /// má možnosť ponechať dosterajšie dáta alebo prijať nové, čím sa prepíšu stré metadáta. 
    /// </summary>
    public static class BottomDialogFunctions
    {
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
        /// Metóda slúži na obnovenie už existujúcej inštancie BottomSheetDialog-u, keďže v priebehu existencie dialógu sa načítavajú nové metadáta získané z
        /// chromaprint-u, ktoré treba následne bez nuestáleho vypínania a zapínania dialógu meniť keď použóvateľ klikne na tlačidlo do predu a chce tak načítať
        /// nové metadáta dostupné pre danú skladbu. Ak sa tieto dáta raz načítajú, už sa nebudú načítavať zas a prechádzanie pomedzi možnosti je tak plynulejšie.
        /// </summary>
        /// <param name="songNameIn">nový názov skladby</param>
        /// <param name="songArtistIn">nový názov interpreta</param>
        /// <param name="songAlbumIn">nový názov albumu z ktorého skladba pochádza</param>
        /// <param name="originalTitle">prvotný názov skladby</param>
        /// <param name="originalAuthor">prvotný názov albumu</param>
        /// <param name="forw">parameter hovoriaci o tom či na aktuálnom výber sa smie ísť do predu</param>
        /// <param name="back">parameter hovoriaci o tom či na aktuálnom výber sa smie ísť do zadu</param>
        /// <param name="imgArr">list všetkých možných náhľadových obrázkov skladby</param>
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
        /// Metóda slúži na vytvorenie spodného dialógu, čiže dialóg ktorých sa vytvára na spodpku obrazovky.
        /// Dialóg slúži na výber metadát skladby pri prvtonom načítavaní aplikácie kedy sa vyhľiadávajú metadáta automaticky za pomoci
        /// chromaprint-u. Dialóg sa zobrazí ak skladba ma viacero možností čo sa týka názvu alebo náhľadového obrázka.
        /// Dialóg obsahuje základny element ImageView v ktorom sa nachádza obrázok skladby. Na ľavo a na pravo od obrázka sa objavia tlačidlá
        /// so šipkami v prípade ak je možné ísť do daného smeru. Nad obrázkom sa nachádza pôvodný autor a pôvodný názov skladby.
        /// A pod obrázok sa nachádza nový názov skladby a nový názov autora. Používateľ si následne prechádza jednotlivými možnosťami obrázka
        /// ak sú k dispozícií. Má možnosť prijať isté metadáta alebo ich odmietnuť a ponechať štandartné.
        /// </summary>
        /// <param name="songNameIn">nový názov skladby</param>
        /// <param name="songArtistIn">nový názov interpreta</param>
        /// <param name="songAlbumIn">nový názov albumu z ktorého skladba pochádza</param>
        /// <param name="originalTitle">prvotný názov skladby</param>
        /// <param name="originalAuthor">prvotný názov albumu</param>
        /// <param name="forw">parameter hovoriaci o tom či na aktuálnom výber sa smie ísť do predu</param>
        /// <param name="back">parameter hovoriaci o tom či na aktuálnom výber sa smie ísť do zadu</param>
        /// <param name="imgArr">list všetkých možných náhľadových obrázkov skladby</param>
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

        
    }
}