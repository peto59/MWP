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
using Java.Util;
using Fragment = AndroidX.Fragment.App.Fragment;
using Orientation = Android.Widget.Orientation;
#if DEBUG
using MWP.Helpers;
#endif


namespace MWP
{
    /// <summary>
    /// Fragment slúžiaci na zobrazenie skladieb nachádzajúcich sa v príslušnom albume. Do fragmentu sa používateľ dostáva prostredníctvom
    /// stlačenia dlaždice akéhokoľvek albumu z vertikálneho zoznamu vo fragmente AlbumAuthorFragment. Fragment v samotnom jadre pozostáva z
    /// vertikálneho ScrollView elementu v ktorom sa nachádzaju dlaždice pre každú skladbu prislúchajúcu k danému albumu. Obrázky
    /// skladieb sú načitavané pomocou Lazy Loading-u tak ako vo zvyšku aplikácie.
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class AlbumFragment : Fragment
    {
        private readonly float scale;
        private readonly Context context;
        private RelativeLayout? mainLayout;
        private Album album;
        private AssetManager? assets;
        
        private Dictionary<LinearLayout?, Guid> SongButtons = new Dictionary<LinearLayout?, Guid>();

        private Dictionary<string, LinearLayout?>? songTilesBuffer;
        private ObservableDictionary<string, Bitmap>? songImagesBuffer;


        /// <summary>
        /// Inicializácia dátových položiek.
        /// </summary>
        /// <param name="ctx">Main Activity context (insert "this")</param>
        public AlbumFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            this.assets = assets;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;

            songTilesBuffer = new Dictionary<string, LinearLayout?>();
            songImagesBuffer = new ObservableDictionary<string, Bitmap>();
        }

        /// <summary>
        /// Override metódy OnCreateView. Volá metódu RenderSongs, ktorá vytvorý samotné rozhranie, takže vytvorý
        /// dlaždice pre skladby albumu. Takisto sa aj pridá udalosť ktorá sa má sputiť v prípade ak sa pridá novy element do
        /// ObservableDictionary pre načítavanie obrázkov. Ak sa pridá obrázok do dict., tak sa načíta do UI.
        /// </summary>
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.album_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.album_fragment_main);

            // získavanie názov albumu na ktorý používateľ klikol. Názov je pridaný do bundle v metóde PopuplateVertical
            // nachádzajúcu sa v UIRenderFunctions.cs.
            string? title = Arguments?.GetString("title");
            List<Album> retreivedSongs = MainActivity.StateHandler.Albums.Search(title);
            if (retreivedSongs.Count > 0)
            {
                album = retreivedSongs[0];
                UiRenderFunctions.FragmentPositionObject = album;
            }


            songImagesBuffer.ValueChanged += delegate
            {
                ((Activity)context).RunOnUiThread(() =>
                {
                    string last = songImagesBuffer.Items.Keys.Last();
                    LinearLayout? child = songTilesBuffer?[last] ?? new LinearLayout(context);
                    if (assets != null)
                        UiRenderFunctions.LoadSongImageFromBuffer(child, songImagesBuffer, assets);
                });
            };
            
            RenderSongs();

            return view;
        }

        
        /// <summary>
        /// Obrázku pre jednotlivé dlaždice sa začnú načitavať až po tom čo sa vytvorý fragment.
        /// </summary>
        public override void OnViewCreated(View view, Bundle? savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            Task.Run(async () =>
            {
                await UiRenderFunctions.LoadSongImages(album.Songs, songImagesBuffer,
                    UiRenderFunctions.LoadImageType.SONG);
                
                UiRenderFunctions.FillImageHoles(context, songTilesBuffer, songImagesBuffer, assets);
            });
           
        }
        
        
        
        /// <summary>
        /// Metóda slúźiaca na vytvorenie dlaždíc každej skladby zo zoznamu skladieb pre album na ktorý používateľ klikol.
        /// </summary>
        private void RenderSongs()
        {
            songTilesBuffer = new Dictionary<string, LinearLayout?>();
            
            ScrollView songsScroll = new ScrollView(context);
            RelativeLayout.LayoutParams songsScrollParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            songsScrollParams.SetMargins(0, 20, 0, 0);
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

            
            
            for (int i = 0; i < album.Songs.Count; i++)
            {

                LinearLayout? lnIn = UiRenderFunctions.PopulateHorizontal(
                    album.Songs[i], scale,
                    150, 100,
                    buttonMargins, nameMargins, cardMargins,
                    17,
                     context, SongButtons, UiRenderFunctions.SongMediaType.AlbumSong, assets, ParentFragmentManager, lnMain
                );
                if (songTilesBuffer.TryAdd(album.Songs[i].Title, lnIn)) 
                    lnMain.AddView(lnIn);
                    
                    
            }

           
            songsScroll.AddView(lnMain);
            mainLayout?.AddView(songsScroll);
                
        }
        

    }
}