using System;
using System.Collections.Generic;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
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

        private EditText? titleIn;
        private EditText? albumIn;
        private EditText? authorIn;
        private EditText? alauIn;
    

        /// <inheritdoc cref="context"/>
        public TagManagerFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            this.assets = assets;
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
        }


    

        /// <inheritdoc />
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.tag_manager_fragment, container, false);

            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.tag_manager_main);

            
          

            SetGenericFont<TextView>(view, Resource.Id.tagmngr_album_label);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_aual_label);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_author_label);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_title_label);
            
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_alau_field);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_album_field);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_author_field);
            SetGenericFont<TextView>(view, Resource.Id.tagmngr_title_field);

            
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
            
            return view;
        }


        private void SetFont(View? view, int id)
        {
            TextView? label = view?.FindViewById<TextView>(id);
            if (label != null) label.Typeface = font;
        }

        private void SetGenericFont<T>(View? view, int id)
        {
            if (view?.FindViewById(id) is TextView label) label.Typeface = font;
            if (view?.FindViewById(id) is EditText input) input.Typeface = font;
        }

    }
}