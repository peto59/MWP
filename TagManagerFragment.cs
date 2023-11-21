using System;
using System.Collections.Generic;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Ass_Pain.BackEnd;
using Ass_Pain.Helpers;
using Google.Android.Material.FloatingActionButton;
using Java.IO;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace Ass_Pain
{
    /// <inheritdoc />
    public class TagManagerFragment : Fragment
    {
        private const int ActionScrollViewHeight = 200;
        private float scale;
        private readonly Context context;
        private RelativeLayout? mainLayout;
        private Typeface? font;
        private AssetManager? assets;
        private MusicBaseClass? song;

        private ImageView? songCover;
        
        private string? initalTitleIn;
        private string? initialAlbumIn;
        private string? initialAuthorIn;
        private string? initialAlauIn;
        private EditText? titleIn;
        private EditText? albumIn;
        private EditText? authorIn;
        private EditText? alauIn;
        
        
        private FloatingActionButton? saveChanges;
        private TextView? backButton;
        

        
        /// <inheritdoc cref="context"/>
        public TagManagerFragment(Context context, AssetManager? assets, MusicBaseClass? song)
        {
            this.song = song;
            this.context = context;
            this.assets = assets;
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
            if (context.Resources is { DisplayMetrics: not null }) scale = context.Resources.DisplayMetrics.Density;

            TagManager tagManager = new TagManager((Song) song);
            
            initialAlauIn = tagManager.Album;
            initialAuthorIn = tagManager.Artist;
            initalTitleIn = tagManager.Title;
            initialAlbumIn = tagManager.Album;
            
            tagManager.Dispose();
        }


    

        /// <inheritdoc />
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.tag_manager_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.tag_manager_main);

            ((MainActivity)Activity).Title = "Tag Manager";
            
            /*
             * changing fonts 
             */
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_album_label);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_aual_label);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_author_label);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_title_label);
            
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_alau_field);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_album_field);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_author_field);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_title_field);

            SetGenericFont<TextView>(view, Resource.Id.tagmngr_back_button);
            
            /*
             * Back Button handle
             */
            backButton = view?.FindViewById<TextView>(Resource.Id.tagmngr_back_button);
            if (backButton != null)
                backButton.Click += (sender, args) =>
                {
                    ParentFragmentManager.PopBackStack();
                };

            /*
             * load image
             */ 
            songCover = view?.FindViewById<ImageView>(Resource.Id.song_cover_tag_manager);
            songCover?.SetImageBitmap(song?.Image);
            try
            {
                var stream = assets?.Open("music_placeholder.png");  
                var imgBitmap = BitmapFactory.DecodeStream(stream);  
            }
            catch (IOException e)
            {
                return view;
            }

            
            /*
             * Save Button
             */
            saveChanges = mainLayout?.FindViewById<FloatingActionButton>(Resource.Id.tag_manager_savebtn);
            if (saveChanges != null)
            {
                saveChanges.Click += delegate { AreYouSure(); };
                saveChanges.Visibility = ViewStates.Gone;
                
                if (BlendMode.Multiply != null)
                    saveChanges?.Background?.SetColorFilter(
                        new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                    );

            }
            

            // if (createPlaylist != null) createPlaylist.Click += CreatePlaylistPopup;
            
            
            
            /*
             * Inputs, handle on change, appearance of save button
             */
            titleIn = view?.FindViewById<EditText>(Resource.Id.tagmngr_title_field);
            albumIn = view?.FindViewById<EditText>(Resource.Id.tagmngr_album_field);
            authorIn = view?.FindViewById<EditText>(Resource.Id.tagmngr_author_field);
            alauIn = view?.FindViewById<EditText>(Resource.Id.tagmngr_alau_field);
            
            if (view != null)
            {
                if (initalTitleIn   != null) HandleSaveButtonAppear(titleIn, initalTitleIn);
                if (initialAlauIn   != null) HandleSaveButtonAppear(alauIn, initialAlauIn);
                if (initialAuthorIn != null) HandleSaveButtonAppear(authorIn, initialAuthorIn);
                if (initialAlbumIn  != null) HandleSaveButtonAppear(albumIn, initialAlbumIn);
            }

            if (titleIn != null) titleIn.Text = initalTitleIn;
            if (albumIn != null) albumIn.Text = initialAlbumIn;
            if (authorIn != null) authorIn.Text = initialAuthorIn;
            if (alauIn != null) alauIn.Text = initialAlauIn;

            return view;
        }

        
        private void HandleSaveButtonAppear(EditText? input, string initial)
        {
            if (input != null)
                input.TextChanged += (_, _) =>
                {
                    if (initial?.Equals(input.Text) == false && saveChanges != null)
                        saveChanges.Visibility = ViewStates.Visible;
                    if (initial?.Equals(input.Text) == true && saveChanges != null)
                        saveChanges.Visibility = ViewStates.Gone;
                };
        }

        private void SetGenericFont<T>(View? view, int id)
        {
            if (view?.FindViewById(id) is TextView label) label.Typeface = font;
            if (view?.FindViewById(id) is EditText input) input.Typeface = font;
        }

        private void SaveChanges()
        {
            TagManager tagManager = new TagManager((Song) song);

            tagManager.Artist = authorIn?.Text ?? string.Empty;
            tagManager.Album = albumIn?.Text ?? string.Empty;
            tagManager.Title = titleIn?.Text ?? string.Empty;
      
            tagManager.Save();
            tagManager.Dispose();
        }

        private void AreYouSure()
        {
            LayoutInflater? ifl = LayoutInflater.From(context);
            View? popupView = ifl?.Inflate(Resource.Layout.share_are_you_sure, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(popupView);

            TextView? title = popupView?.FindViewById<TextView>(Resource.Id.share_are_you_sure_title);
            TextView? yes = popupView?.FindViewById<TextView>(Resource.Id.share_are_you_sure_yes);
            TextView? no = popupView?.FindViewById<TextView>(Resource.Id.share_are_you_sure_no);

            if (title != null) title.Typeface = font;
            if (yes != null) yes.Typeface = font;
            if (no != null) no.Typeface = font;

            AlertDialog? dialog = alert.Create();
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));

            if (title != null) title.Text = "Are you sure";
            if (yes != null)
            {
                yes.Click += (_, _) =>
                {   
                    SaveChanges();
                };
            }
            
            if (no != null) no.Click += (_, _) => dialog?.Cancel();
            dialog?.Show();
            

        }
    }
    
    
        
}