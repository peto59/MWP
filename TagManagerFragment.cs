using System;
using System.Collections.Generic;
using Android.Animation;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using Ass_Pain.Helpers;
using Google.Android.Material.FloatingActionButton;
using Java.IO;

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

        private ImageView? songCover;
        
        private string? initalTitleIn;
        private string? initialAlbumIn;
        private string? initialAuthorIn;
        private string? initialAlauIn;
        
        private FloatingActionButton? saveChanges;
        private TextView? backButton;
        

        
        /// <inheritdoc cref="context"/>
        public TagManagerFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            this.assets = assets;
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;

            initialAlauIn = "";
            initialAuthorIn = "";
            initalTitleIn = "google";
            initialAlbumIn = "";
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
            try
            {
                var stream = assets?.Open("music_placeholder.png");  
                var imgBitmap = BitmapFactory.DecodeStream(stream);  
                songCover?.SetImageBitmap(imgBitmap);
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
            if (view != null)
            {
                if (initalTitleIn   != null) HandleSaveButtonAppear(Resource.Id.tagmngr_title_field, initalTitleIn, view);
                if (initialAlauIn   != null) HandleSaveButtonAppear(Resource.Id.tagmngr_alau_field, initialAlauIn, view);
                if (initialAuthorIn != null) HandleSaveButtonAppear(Resource.Id.tagmngr_author_field, initialAuthorIn, view);
                if (initialAlbumIn  != null) HandleSaveButtonAppear(Resource.Id.tagmngr_album_field, initialAlbumIn, view);
            }

            return view;
        }


        private void HandleSaveButtonAppear(int id, string inital, View view)
        {
            EditText? input = view?.FindViewById<EditText>(id);
            if (input != null)
                input.TextChanged += (_, _) =>
                {
                    if (inital?.Equals(input.Text) == false)
                        if (saveChanges != null) saveChanges.Visibility = ViewStates.Visible;
                    if (inital?.Equals(input.Text) == true)
                        if (saveChanges != null) saveChanges.Visibility = ViewStates.Gone;
                };
        }

        private void SetGenericFont<T>(View? view, int id)
        {
            if (view?.FindViewById(id) is TextView label) label.Typeface = font;
            if (view?.FindViewById(id) is EditText input) input.Typeface = font;
        }

    }
}