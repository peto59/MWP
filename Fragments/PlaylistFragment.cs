using System;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content.Res;
using Android.Graphics;
using MWP.BackEnd;
using Fragment = AndroidX.Fragment.App.Fragment;
using Orientation = Android.Widget.Orientation;


namespace MWP
{
    /// <summary>
    /// Trieda PlaylistFragment slúźi na vytvorenie zoznamu skladieb istého playlistu používateľa. Po kliknutí na playlist
    /// v zozname playlistov je používateľ presunutý do nového fragmentu v rámci ktorého je prezentovaný so zoznamom skladieb prislúchajúcich
    /// k danému playlistu, ktoré si tam používateľ pridal. Fragment sa skladá zo ScrollView elementu s dlaždicami pre každú skladbu. Obrázky
    /// skladieb sa načítavajú pomocou metódy Lazy Loading tak isto ako v iných častiach aplikácie. Dlaždice sú vytvárané prostredníctvom
    /// metódy UiRenderFunctions.PopulateHorizontal pre horizontálne dlaždice. Jednotlivé skladby sú získavané z pozadia aplikácie.
    /// </summary>
    public class PlaylistFragment : Fragment
    {
        private readonly Context context;
        private RelativeLayout? mainLayout;
        private const int ActionScrollViewHeight = 20;
        private readonly float scale;
        private AssetManager? assets;
        
        // Dictionaries potrebné pre realizáciu Lazy Loadingu.
        // _lazyBuffer slúži na ukladanie dlaždíc
        // _lazyImageBuffer slúži na ukladanie obrázkov ktoré sa načítavaju osobitne
        private static Dictionary<string, LinearLayout> _lazyBuffer;
        private static ObservableDictionary<string, Bitmap>? _lazyImageBuffer;
        
        /*
         * songButtons slúži pre neskoršiu diferenciáciu medzi tým, aká skladba bola spustená a aké ID skladby sa
         * má spustiť
         */
        private Dictionary<LinearLayout?, Guid> songButtons = new Dictionary<LinearLayout?, Guid>();
        
            
        /// <summary>
        /// Táto metóda sa volá pri vytvorení view pre playlist fragment.
        /// Najprv sa inflatuje layout pre získanie štýlu definováneho v xml súvore playlist_fragment.xml.
        /// Potom sa získava referencia na hlavný layout fragmentu.
        /// Následne sa získa názov playlistu z argumentov a asynchrónne sa načítavajú obrázky skladieb pre daný playlist.
        /// Po načítaní sa vyplnia medzery v obrázkoch a prekreslí sa zobrazenie playlistu.
        /// </summary>
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.playlist_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.playlistttt_fragment_main);
            
            string? title = Arguments?.GetString("title");
            Task.Run(async () =>
            {
                await UiRenderFunctions.LoadSongImages(FileManager.GetPlaylist(title), _lazyImageBuffer, UiRenderFunctions.LoadImageType.SONG);
                // Metóda slúži ako druhá vlna načítavania obrázkov, ak sa náhodou nejaké obrázku nenačítali v prvom Lazy Loadingu, skúsia sa načítať ešte raz
                // avšak už len tie ktoré nie sú načitané aby sa zabránilo spomaľovaniu zariadenia ešte viac.
                UiRenderFunctions.FillImageHoles(context, _lazyBuffer, _lazyImageBuffer, assets);
            });
            
            if (_lazyImageBuffer != null)
                _lazyImageBuffer.ValueChanged += (_, _) =>
                {
                    ((Activity)context).RunOnUiThread(() => {
                        string last = _lazyImageBuffer.Items.Keys.Last();

                        LinearLayout? child = _lazyBuffer?[last] ?? new LinearLayout(context);
                        if (assets != null)
                            UiRenderFunctions.LoadSongImageFromBuffer(child, _lazyImageBuffer, assets);
                    });
                };
            
            
            if (title != null) RenderPlaylists(title);
                
            return view;
        }
        
       

        /// <summary>
        /// Constructor for SongsFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (e.g. "this")</param>
        /// <param name="assets">Main Acitivity assets (e.g. "this.Assets")</param>
        public PlaylistFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            this.assets = assets;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
            
            _lazyImageBuffer = new ObservableDictionary<string, Bitmap>();

        }

        
        /// <summary>
        /// Táto metóda slúži na vykreslenie zoznamu skladieb v playliste.
        /// Najprv sa vytvára buffer pre ukladanie odkazov na jednotlivé playlisty.
        /// Potom sa vytvára ScrollView pre zobrazenie zoznamu skladieb v playliste.
        /// Následne sa vytvára LinearLayout pre usporiadanie jednotlivých položiek playlistu vo vertikálnom smere.
        /// Pre každú pieseň v playliste sa vytvára horizontálny layout obsahujúci tlačidlo pre zmenu stavu skladby, názov skladby, a tlačidlo pre zobrazenie možností.
        /// Tieto layouty sa pridávajú do hlavného LinearLayoutu a zároveň sa ukladajú do bufferu.
        /// Nakoniec sa celý zoznam skladieb pridáva do ScrollView a ScrollView sa pridáva do hlavného layoutu fragmentu.
        /// </summary>
        /// <param name="title">názov playlistu na základe ktorého sa získavajú skladby</param>
        private void RenderPlaylists(string title)
        {
            _lazyBuffer = new Dictionary<string, LinearLayout>();
            
            ScrollView inPlaylistScroll = new ScrollView(context);
            RelativeLayout.LayoutParams inPlaylistScrollParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            inPlaylistScrollParams.SetMargins(0, ActionScrollViewHeight, 0, 0);
            inPlaylistScroll.LayoutParameters = inPlaylistScrollParams;
            
            LinearLayout inPlaylistLnMain = new LinearLayout(context);
            inPlaylistLnMain.Orientation = Orientation.Vertical;
            RelativeLayout.LayoutParams inPlaylistLnMainParams = new RelativeLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent
            );
            inPlaylistLnMainParams.SetMargins(20, 20, 20, 20);
            inPlaylistLnMain.LayoutParameters = inPlaylistLnMainParams;

            int[] inPlaylistButtonMargins = { 50, 50, 50, 50 };
            int[] inPlaylistNameMargins = { 50, 50, 50, 50 };
            int[] inPlaylistCardMargins = { 0, 50, 0, 0 };


            List<Song> playlistSongs = FileManager.GetPlaylist(title);
            UiRenderFunctions.FragmentPositionObject = playlistSongs;
            
            for (int i = 0; i < playlistSongs.Count; i++)
            {
                LinearLayout? lnIn = UiRenderFunctions.PopulateHorizontal(
                    playlistSongs[i], scale,
                    150, 100,
                    inPlaylistButtonMargins, inPlaylistNameMargins, inPlaylistCardMargins,
                    17,
                    context, songButtons, UiRenderFunctions.SongMediaType.PlaylistSong, assets, ParentFragmentManager, inPlaylistLnMain
                );
                if (_lazyBuffer.TryAdd(playlistSongs[i].Title, lnIn))
                {
                    inPlaylistLnMain?.AddView(lnIn);
                }
            }
           

            inPlaylistScroll.AddView(inPlaylistLnMain);
            mainLayout?.AddView(inPlaylistScroll);
        }
        
        
    }
}