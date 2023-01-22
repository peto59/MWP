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
using Android.Views.Inspectors;

namespace Ass_Pain
{
    public static class side_player
    {

        static Dictionary<LinearLayout, string> player_buttons = new Dictionary<LinearLayout, string>();

        static ImageView play_image;
        static Int16 repeat_state = 0;
        static bool shuffle_state = false;

        private static LinearLayout cube_creator(string size, float scale, AppCompatActivity context, string 型 = "idk")
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

                    if (MainActivity.player.isPlaying())
                        play_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("pause.png")));
                    else
                        play_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("play.png")));


                    cube.AddView(play_image);

                    break;
                case "small":
                    LinearLayout.LayoutParams small_cube_params = new LinearLayout.LayoutParams(
                        (int)(30 * scale + 0.5f),
                        (int)(30 * scale + 0.5f)
                    );
                    small_cube_params.SetMargins (
                       (int)(10 * scale + 0.5f), (int)(10 * scale + 0.5f), // left, top
                       (int)(10 * scale + 0.5f), (int)(10 * scale + 0.5f) // right, bottom
                    );

                    cube.LayoutParameters = small_cube_params;
                    cube.SetGravity(GravityFlags.Center);
                    cube.Orientation = Android.Widget.Orientation.Horizontal;
                    

                    ImageView last_image = new ImageView(context);
                    LinearLayout.LayoutParams last_image_params;


                    switch (型)
                    {
                        case "right":
                            last_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("right.png")));
                            last_image_params = new LinearLayout.LayoutParams(
                               LinearLayout.LayoutParams.MatchParent,
                               LinearLayout.LayoutParams.MatchParent
                            );
                            last_image.LayoutParameters = last_image_params;
                            cube.SetBackgroundResource(Resource.Drawable.rounded_light);
                            break;
                        case "left":
                            last_image_params = new LinearLayout.LayoutParams(
                               LinearLayout.LayoutParams.MatchParent,
                               LinearLayout.LayoutParams.MatchParent
                            );
                            last_image.LayoutParameters = last_image_params;
                            last_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("left.png")));
                            cube.SetBackgroundResource(Resource.Drawable.rounded_light);
                            break;
                        case "shuffle":
                            last_image_params = new LinearLayout.LayoutParams(
                              (int)(20 * scale + 0.5f),
                              (int)(20 * scale + 0.5f)
                            );
                            last_image.LayoutParameters = last_image_params;
                            last_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("shuffle.png")));
                            break;
                        case "repeat":
                            last_image_params = new LinearLayout.LayoutParams(
                             (int)(20 * scale + 0.5f),
                             (int)(20 * scale + 0.5f)
                            );
                            last_image.LayoutParameters = last_image_params;
                            last_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("no_repeat.png")));
                            
                            break;
                    }



                    cube.AddView(last_image);

                    break;
            }

            return cube;
        }

        private static void pause_play(Object sender, EventArgs e, AppCompatActivity context)
        {
            MainActivity.player.TogglePlayButton(context);
        }

        public static void SetPlayButton(AppCompatActivity context)
        {
            
            play_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("play.png")));
        }

        public static void SetStopButton(AppCompatActivity context)
        {
            play_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("pause.png")));
        }

      
        public static void populate_side_bar(AppCompatActivity context)
        {
            // basic  vars
            string path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
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

            (string album, string autor) = FileManager.GetAlbumAuthorFromPath(current_song_path);
            if (FileManager.GetSongArtist(current_song_path).Length != 0)
            {
                song_author.Text = FileManager.GetSongArtist(current_song_path)[0];
                song_author.Click += (sender, e) =>
                {
                    Intent intent = new Intent(context, typeof(all_songs));
                    intent.PutExtra("link_author", $"{path}/{autor}");
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

                

                LinearLayout shuffle = cube_creator("small", scale, context, "shuffle");
                player_buttons.Add(shuffle, "shuffle");
                shuffle.Click += delegate
                {
                    ImageView shuffle_img = (ImageView)shuffle.GetChildAt(0);
                    if (!shuffle_state)
                    {
                        shuffle_img.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("shuffle_on.png")));
                        shuffle_state = true;

                        MainActivity.player.Shuffle(shuffle_state);
                    }
                    else
                    {
                        shuffle_img.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("shuffle.png")));
                        shuffle_state = false;
                    }
                };

                LinearLayout repeat = cube_creator("small", scale, context, "repeat");
                player_buttons.Add(repeat, "repeat");
                repeat.Click += delegate
                {
                    ImageView repeat_img = (ImageView)repeat.GetChildAt(0);
                    switch (repeat_state)
                    {
                        case 0:
                            repeat_img.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("repeat.png")));
                            repeat_state = 1;
                            break;
                        case 1:
                            repeat_img.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("repeat_one.png")));
                            repeat_state = 2;
                            break;
                        case 2:
                            repeat_img.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("no_repeat.png")));
                            repeat_state = 0;
                            break;
                    }

                    MainActivity.player.ToggleLoop(repeat_state);
                };


                LinearLayout last = cube_creator("small", scale, context, "left");
                last.Click += delegate
                {
                    MainActivity.player.PreviousSong();
                };
                player_buttons.Add(last, "last");

                LinearLayout play_pause = cube_creator("big", scale, context);
                play_pause.Click += (sender, e) => { pause_play(sender, e, context);  };
                player_buttons.Add(play_pause, "play_pause");

                LinearLayout next = cube_creator("small", scale, context, "right");
                next.Click += delegate
                {
                    MainActivity.player.NextSong();
                };
                player_buttons.Add(next, "next");


                buttons_main_lin.AddView(shuffle);
                buttons_main_lin.AddView(last);
                buttons_main_lin.AddView(play_pause);
                buttons_main_lin.AddView(next);
                buttons_main_lin.AddView(repeat);
            }

            /*
             * Get Image from image to display
             */

            Bitmap image = null;
            TagLib.File tagFile;

            song_image.SetImageResource(Resource.Mipmap.ic_launcher);

            try
            {
                Console.WriteLine($"now playing: {MainActivity.player.NowPlaying()}");
                tagFile = TagLib.File.Create(
                    MainActivity.player.NowPlaying()
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