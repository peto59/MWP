using System.Collections.Generic;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;

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

            return view;
        }
    }
}