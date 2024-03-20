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
using Android.Text.Style;
using Android.Util;
using Java.Util;
using MWP.Helpers;
using Fragment = AndroidX.Fragment.App.Fragment;
using Orientation = Android.Widget.Orientation;


namespace MWP
{
    
    /// <summary>
    /// Trieda AlbumAuthorFragment slúži na vytvorenie rozhrania poskytujúce zoznam albumov a interpretov skladieb. Pri načítani fragmentu je
    /// používateľ prezenotvaný s dvoma stĺpcami, album a authors, sú to dva vertikalné ScrollView elementy, v každom z nich sa nachádzaju zoznamy
    /// políčok reprezentujúce príslušne albumy alebo interpretov.
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AlbumAuthorFragment : Fragment
    {
        private readonly float scale;
        private readonly Context context;
        private RelativeLayout? mainLayout;
        private AssetManager? assets;

        private Dictionary<LinearLayout?, object> albumButtons = new Dictionary<LinearLayout?, object>();

        // Dictionaries potrebné na Lazy Loading obrázkov albumov a autorov
        private Dictionary<string, LinearLayout?> albumTilesBuffer;
        private Dictionary<string, LinearLayout?> authorTilesBuffer;
        private ObservableDictionary<string, Bitmap> albumImagesBuffer;
        private ObservableDictionary<string, Bitmap> authorImagesBuffer;
        
        // inštancie Fragmentov do ktorých je používateľ premiestnený po kliknutí na políčko albumu alebo autora
        private AlbumFragment albumFragment;
        private AuthorFragment authorFragment;
        
        
        /// <summary>
        /// Inicializácia dátových položiek
        /// </summary>
        /// <param name="ctx">Kontext hlanvje aktivity (takže "this")</param>
        public AlbumAuthorFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            this.assets = assets;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
            
            albumFragment = new AlbumFragment(context, this.assets);
            authorFragment = new AuthorFragment(context, this.assets);

            albumTilesBuffer = new Dictionary<string, LinearLayout?>();
            albumImagesBuffer = new ObservableDictionary<string, Bitmap>();

            authorImagesBuffer = new ObservableDictionary<string, Bitmap>();
            authorTilesBuffer = new Dictionary<string, LinearLayout?>();
            
        }


        /// <summary>
        /// Override metódy OnCreateView. Volá metódu SetupStructure, ktorá vytvorý samotné rozhranie a nastaví události
        /// pre ObservableDictionaries, ktoré reagujú na zmenu, čiže ak sa pridá nový element pri načítavaní skladieb.
        /// </summary>
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
                        UiRenderFunctions.LoadSongImageFromBuffer(child, albumImagesBuffer, assets);
                });
            };
            
            authorImagesBuffer.ValueChanged += delegate
            {
                ((Activity)context).RunOnUiThread(() => 
                {
                    string last = authorImagesBuffer.lastValueAdded;
                    LinearLayout? child = authorTilesBuffer?[last] ?? new LinearLayout(context);
                    if (assets != null)
                        UiRenderFunctions.LoadSongImageFromBuffer(child, authorImagesBuffer, assets);
                });
            };
            
            
            
            SetupStructure();
            

            
            return view;
        }
        
        /// <summary>
        /// Obrázku pre jednotlivé políčka sa začnú načitavať až po tom čo sa vytvorý fragment.
        /// </summary>
        public override void OnViewCreated(View view, Bundle? savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            
                 
            Task.Run(async () =>
            {
                await UiRenderFunctions.LoadSongImages(MainActivity.StateHandler.Albums, albumImagesBuffer,
                    UiRenderFunctions.LoadImageType.ALBUM);

                ((Activity)context).RunOnUiThread(() =>
                {
                    foreach (var tile in albumTilesBuffer)
                    {
                        ImageView? vv = (ImageView)tile.Value.GetChildAt(0);
                        if (vv?.Drawable == null)
                            UiRenderFunctions.LoadSongImageFromBuffer(tile.Value, albumImagesBuffer, assets);
                    }
                });
            });
            Task.Run(async () =>
            {
                await UiRenderFunctions.LoadSongImages(MainActivity.StateHandler.Artists, authorImagesBuffer,
                    UiRenderFunctions.LoadImageType.AUTHOR);
                
                ((Activity)context).RunOnUiThread(() =>
                {
                    foreach (var tile in authorTilesBuffer)
                    {
                        ImageView? vv = (ImageView)tile.Value.GetChildAt(0);
                        if (vv?.Drawable == null)
                            UiRenderFunctions.LoadSongImageFromBuffer(tile.Value, authorImagesBuffer, assets);
                    }
                });
            });

          
            
        }

        /// <summary>
        /// Metóda slúži na vytvorenie dlaždíc pre všetky albumy, ktoré su získavané z pozadia aplikácie.
        /// Táto metóda vytvára a inicializuje zoznam dlaždíc pre zobrazenie albumov v režime vertikálneho rozloženia.
        /// Vytvorí sa prázdny slovník albumTilesBuffer, ktorý bude slúžiť na ukladanie referencií na dlaždice albumov.
        /// Potom sa vytvorí nový lineárny layout (lin), ktorému sa nastaví orientácia na vertikálnu.
        /// Pre každý album v zozname MainActivity.StateHandler.Albums sa vytvorí dlaždica a pridá sa do lineárneho layoutu.
        /// Taktiež sa pridá referencia dlaždice albumu do slovníka albumTilesBuffer.
        /// Vypočíta sa percentuálny podiel nahratých obrázkov albumov voči celkovému počtu dlaždíc.
        /// Ak tento podiel presahuje 80%, načítajú sa obrázky albumov z albumImagesBuffer do príslušných dlaždíc.
        /// To je len v prípade ak náhodou sa isté obrázky nepodarí načítať. Je to akási druhá vlna načítavania, čím vytvárame
        /// redundanciu načítavania obrázkov.
        /// </summary>
        /// <returns> Na záver sa vráti lineárny layout lin obsahujúci dlaždice albumov.</returns>
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
                LinearLayout? lnIn = UiRenderFunctions.PopulateVertical(
                    albums[i], 
                    scale, 150, 100, buttonMargins,cardMargins, nameMargins, 15,  
                    context, albumButtons, 
                    ParentFragmentManager, assets, albumFragment, authorFragment);
                if (albumTilesBuffer.TryAdd(albums[i].Title, lnIn))
                {
                    lin.AddView(lnIn);
                }
            }
            decimal percentage = 100;
            try
            {
                percentage = ((decimal)albumImagesBuffer.Items.Count / (decimal)albumTilesBuffer.Count) * 100;
            }
            catch(Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                //ignored
            }
            if (percentage > 80)
            {
                for (int i = 0; i < albums.Count; i++)
                {
                    LinearLayout? child = albumTilesBuffer[albums[i].Title];
                    if (assets != null) UiRenderFunctions.LoadSongImageFromBuffer(child, albumImagesBuffer, assets);
                }
            }
            

            return lin;
        }


        /// <summary>
        /// Metóda AuthorTiles funguje na rovnakom princípe ako metóda AlbumTiles avšak vytvára zoznam dlaždíc pre všetkých
        /// interpretov, ktorých zoznam získavam z pozadia aplikácie, rovnako ako pri AlbumTiles.
        /// </summary>
        /// <returns></returns>
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
                LinearLayout? lnIn = UiRenderFunctions.PopulateVertical(
                    MainActivity.StateHandler.Artists[i], scale, 150, 100, buttonMargins ,cardMargins, nameMargins, 15, context, albumButtons, 
                    ParentFragmentManager, assets, albumFragment, authorFragment);
                if (authorTilesBuffer.TryAdd(artists[i].Title, lnIn))
                {
                    lin.AddView(lnIn);
                }
            }

            decimal percentage = 100;
            try
            {
                percentage = ((decimal)authorImagesBuffer.Items.Count / (decimal)authorTilesBuffer.Count) * 100;
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                //ignored
            }
            if (percentage > 80)
            {
                for (int i = 0; i < artists.Count; i++)
                {
                    LinearLayout? child = authorTilesBuffer[artists[i].Title];
                    if (assets != null) UiRenderFunctions.LoadSongImageFromBuffer(child, authorImagesBuffer, assets);
                }
            }

            return lin;
        }


        /// <summary>
        /// Hlavná metóda na vytvorenie samotného rozhrania. Inicializuje ScrollView elementy pre albumy a atorov.
        /// Následne do nich vkladá všetky zoznamu prostrednícvtom metód AtuhorTiles a AlbumTiles.
        /// </summary>
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
                authorScrollParams.SetMargins(0, 185, 0, 0);
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
                albumScrollParams.SetMargins((displayWidth / 2) - 20, 185, 0, 0);
                albumScroll.LayoutParameters = albumScrollParams;
            }

            LinearLayout albumLin = AlbumTiles();
            albumScroll.AddView(albumLin);
            mainLayout?.AddView(albumScroll);
            
            
            
        }
    }
}