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
using System.Runtime.CompilerServices;
using Xamarin.Essentials;

namespace Ass_Pain
{
    public static class side_player
    {

        static Dictionary<LinearLayout, string> player_buttons = new Dictionary<LinearLayout, string>();

        static bool is_playing = true;
        static ImageView play_image;

        private static LinearLayout cube_creator(string size, float scale, AppCompatActivity context, bool 演じる, bool 左右 = false)
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
                    cube.SetGravity(GravityFlags.Center);
                    cube.Orientation = Android.Widget.Orientation.Horizontal;
                    cube.SetBackgroundResource(Resource.Drawable.rounded_light);


                    play_image = new ImageView(context);
                    LinearLayout.LayoutParams play_image_params = new LinearLayout.LayoutParams(
                        (int)(40 * scale + 0.5f),
                        (int)(40 * scale + 0.5f)
                    );
                    play_image.LayoutParameters = play_image_params;

                    if (演じる == true)
                        play_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("play.png")));
                    else
                        play_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("play.png")));


                    cube.AddView(play_image);

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

        private static void pause_play(Object sender, EventArgs e, AppCompatActivity context)
        {
            if (is_playing)
            {
                MainActivity.player.Stop(sender, e);
                play_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("pause.png")));
                is_playing = false;
            }
            else
            {
                MainActivity.player.Resume(sender, e);
                play_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("play.png")));
                is_playing = true;
            }
        }


        
        public static void populate_side_bar(AppCompatActivity context)
        {
            // basic  vars
            string current_song_path = MainActivity.player.NowPlaying();
            float scale = context.Resources.DisplayMetrics.Density;

            // sing title image
            ImageView song_image = context.FindViewById<ImageView>(Resource.Id.song_cover);

            // texts
            TextView song_title = context.FindViewById<TextView>(Resource.Id.song_cover_name);
            TextView song_author = context.FindViewById<TextView>(Resource.Id.side_author);
            TextView song_album = context.FindViewById<TextView>(Resource.Id.side_album);

            
            LinearLayout.LayoutParams song_title_params = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WrapContent,
                (int)(30 * scale + 0.5f)
            );
            song_title_params.SetMargins(
                0, (int)(10 * scale + 0.5f), 0, (int)(10 * scale + 0.5f)
            );


            song_title.LayoutParameters = song_title_params;
            song_title.Text = FileManager.GetSongTitle(current_song_path);
            
            if (FileManager.GetSongArtist(current_song_path).Length != 0)
            {
                song_author.Text = FileManager.GetSongArtist(current_song_path)[0];
                song_author.Click += (sender, e) =>
                {
                    Intent intent = new Intent(context, typeof(all_songs));
                    intent.PutExtra("link_author", "");
                    context.StartActivity(intent);
                };
            }
                
            song_album.Text = FileManager.GetSongAlbum(current_song_path);

            /* 
             * player buttons
             */
            LinearLayout buttons_main_lin = context.FindViewById<LinearLayout>(Resource.Id.player_buttons);
            buttons_main_lin.RemoveAllViews();
            player_buttons.Clear();
            {
                LinearLayout last = cube_creator("small", scale, context, false, false);
                last.Click += delegate
                {
                    // pass
                };
                player_buttons.Add(last, "last");

                LinearLayout play_pause = cube_creator("big", scale, context, is_playing);
                play_pause.Click += (sender, e) => { pause_play(sender, e, context);  };
                player_buttons.Add(play_pause, "play_pause");

                LinearLayout next = cube_creator("small", scale, context, false, true);
                next.Click += delegate
                {
                    MainActivity.player.NextSong();
                };
                player_buttons.Add(next, "next");


                buttons_main_lin.AddView(last);
                buttons_main_lin.AddView(play_pause);
                buttons_main_lin.AddView(next);
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
            
            
            // progress song
          

            
        }

       
    }
}