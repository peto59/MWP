using System;
using AndroidX.DrawerLayout.Widget;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Navigation;
using Google.Android.Material.Snackbar;
using Android.Webkit;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using AndroidX.AppCompat.Graphics.Drawable;
using Android.Widget;
using Android.Graphics;
using Java.Util.Jar;
using Android.Text;
using Android.Icu.Number;
using Android.Content.Res;
using System.Runtime.Remoting.Contexts;
using Android.Content.Res.Loader;
using System.Runtime.CompilerServices;
using Xamarin.Essentials;
using AndroidX.AppCompat.App;
using Java.Util.Functions;
using Android.Views;
using Android.Content;

namespace Ass_Pain
{
    public static class accept_songs
    {
        static FloatingActionButton fab;


        private static void BtnDrapDrop_Drag(object sender, View.DragEventArgs e)
        {
            RelativeLayout lay = (RelativeLayout)sender;

            if (e.Event is { Action: DragAction.Started })
            {
                if (e.Event.Action != DragAction.Ended)
                {
                    float x = e.Event.GetX();
                    float y = e.Event.GetY();
                    fab.TranslationX = x;
                    fab.TranslationY = y;
                    // lay.Invalidate();
                }
            }
        }

        [Obsolete("Obsolete")]
        public static void wake_drag_button(AppCompatActivity context, int rel)
        {
            RelativeLayout? main = context.FindViewById<RelativeLayout>(rel);

            // floating button
            fab = new FloatingActionButton(context);
            RelativeLayout.LayoutParams fabParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            
            fab.LayoutParameters = fabParams;
            if (PorterDuff.Mode.Multiply != null)
                fab.Background?.SetColorFilter(
                    Color.Rgb(255, 76, 41),
                    PorterDuff.Mode.Multiply
                );

            fab.LongClick += (_, _) =>
            {
                ClipData? dragData = ClipData.NewPlainText("", "");
                View.DragShadowBuilder myShadow = new View.DragShadowBuilder(fab);
                fab.StartDrag(dragData, myShadow, null, 0);

            };


            if (main != null)
            {
                main.Drag += BtnDrapDrop_Drag;

                main.AddView(fab);
            }
        }

    }
}