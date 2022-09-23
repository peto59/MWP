using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.View;
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

namespace Ass_Pain
{
    public static class side_player
    {

        
        public static void populate_side_bar(AppCompatActivity context, Slovenska_prostituka player)
        {

            TextView ttt = context.FindViewById<TextView>(Resource.Id.song_cover_name);
            ttt.Text = "Song Name";

            ImageView song_image = context.FindViewById<ImageView>(Resource.Id.song_cover);
            

            
        }

    }
}