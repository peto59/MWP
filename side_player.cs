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
using Android.Graphics;
using Java.Util.Jar;
using Com.Arthenica.Ffmpegkit;
using Android.Text;
using Android.Icu.Number;
using Android.Content.Res;
using System.Runtime.Remoting.Contexts;
using Android.Content.Res.Loader;

namespace Ass_Pain
{
    public static class side_player
    {

        static Dictionary<LinearLayout, string> player_buttons = new Dictionary<LinearLayout, string>();

        public static LinearLayout cube_creator(string size, float scale, AppCompatActivity context, bool 左右)
        {

            LinearLayout cube = new LinearLayout(context);

            switch (size)
            {
                case "big":
                    LinearLayout.LayoutParams big_cube_params = new LinearLayout.LayoutParams(
                        (int)(50 * scale + 0.5f),
                        (int)(50 * scale + 0.5f)
                    );
                    big_cube_params.SetMargins(
                        (int)(10 * scale + 0.5f), (int)(10 * scale + 0.5f), // left, top
                        (int)(10 * scale + 0.5f), (int)(10 * scale + 0.5f) // right, bottom
                    );

                    cube.LayoutParameters = big_cube_params;
                    cube.Orientation = Android.Widget.Orientation.Horizontal;
                    cube.SetBackgroundResource(Resource.Drawable.rounded_light);

                    break;
                case "small":
                    LinearLayout.LayoutParams small_cube_params = new LinearLayout.LayoutParams(
                        (int)(30 * scale + 0.5f),
                        (int)(30 * scale + 0.5f)
                    );
                    small_cube_params.SetMargins(
                       (int)(10 * scale + 0.5f), (int)(10 * scale + 0.5f), // left, top
                       (int)(10 * scale + 0.5f), (int)(10 * scale + 0.5f) // right, bottom
                   );

                    cube.LayoutParameters = small_cube_params;
                    cube.Orientation = Android.Widget.Orientation.Horizontal;
                    cube.SetBackgroundResource(Resource.Drawable.rounded_light);

                    ImageView last_image = new ImageView(context);
                    LinearLayout.LayoutParams last_image_params = new LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MatchParent,
                        LinearLayout.LayoutParams.MatchParent
                    );
                    last_image.LayoutParameters = last_image_params;
                    if (左右)
                        last_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("right.png")));
                    else
                        last_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("left.png")));



                    cube.AddView(last_image);

                    break;
            }

            return cube;
        }

        
        public static void populate_side_bar(AppCompatActivity context)
        {
            string current_song_path = MainActivity.player.NowPlaying();

            float scale = context.Resources.DisplayMetrics.Density;

            ImageView song_image = context.FindViewById<ImageView>(Resource.Id.song_cover);
            TextView song_title = context.FindViewById<TextView>(Resource.Id.song_cover_name);
            LinearLayout.LayoutParams song_title_params = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WrapContent,
                (int)(30 * scale + 0.5f)
                );
            song_title_params.SetMargins(
                0, (int)(10 * scale + 0.5f), 0, (int)(10 * scale + 0.5f)
            );
            song_title.LayoutParameters = song_title_params;
            song_title.Text = FileManager.GetSongTitle(current_song_path);

            /* 
             * player buttons
             */
            LinearLayout main_lin = context.FindViewById<LinearLayout>(Resource.Id.player_buttons);
            main_lin.RemoveAllViews();
            player_buttons.Clear();
            {
                LinearLayout last = cube_creator("small", scale, context, false);
                player_buttons.Add(last, "last");
                LinearLayout play_pause = cube_creator("big", scale, context, false);
                player_buttons.Add(play_pause, "play_pause");
                LinearLayout next = cube_creator("small", scale, context, true);
                player_buttons.Add(next, "next");


                main_lin.AddView(last);
                main_lin.AddView(play_pause);
                main_lin.AddView(next);
            }

            /*
             * Get Image from image to display
             */

            Bitmap image = null;
            TagLib.File tagFile;

            song_image.SetImageResource(Resource.Mipmap.ic_launcher);

            try
            {
                tagFile = TagLib.File.Create(
                    MainActivity.player.NowPlaying()//extracts image from first song of album that contains embedded picture
                );
                MemoryStream ms = new MemoryStream(tagFile.Tag.Pictures[0].Data.Data);
                image = BitmapFactory.DecodeStream(ms);

                LinearLayout.LayoutParams song_image_params = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.WrapContent,
                    LinearLayout.LayoutParams.WrapContent
                );
                song_image.LayoutParameters = song_image_params;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine($"Doesnt contain image: {MainActivity.player.NowPlaying()}");
            }

            if (image == null)
            {
                image = BitmapFactory.DecodeStream(context.Assets.Open("music_placeholder.png")); //In case of no cover and no embedded picture show default image from assets 
                LinearLayout.LayoutParams song_image_params = new LinearLayout.LayoutParams(
                   (int)(120 * scale + 0.5f),
                   (int)(120 * scale + 0.5f)
                );
                song_image.LayoutParameters = song_image_params;

            }


            // set the image
            song_image.SetImageBitmap(image);
        }

       
    }
}