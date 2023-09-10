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
using System.Threading;
using Java.Util;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain
{
	public static class side_player
	{

		static Dictionary<LinearLayout, string> player_buttons = new Dictionary<LinearLayout, string>();

		static ImageView play_image;

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

					if (MainActivity.stateHandler.IsPlaying)
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
							if (MainActivity.stateHandler.IsShuffling)
							{
								last_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("shuffle_on.png")));
							}
							else
							{
								last_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("shuffle.png")));
							}
							
							break;
						case "repeat":
							last_image_params = new LinearLayout.LayoutParams(
							 (int)(20 * scale + 0.5f),
							 (int)(20 * scale + 0.5f)
							);
							last_image.LayoutParameters = last_image_params;
							switch (MainActivity.stateHandler.LoopState)
							{
								case 0:
									last_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("no_repeat.png")));
									break;
								case 1:
									last_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("repeat.png")));
									break;
								case 2:
									last_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("repeat_one.png")));
									break;
							}
							break;
					}



					cube.AddView(last_image);

					break;
			}

			return cube;
		}

		private static void pause_play(Object sender, EventArgs e, AppCompatActivity context)
		{
			/*context.StartService(
				new Intent(MediaService.ActionTogglePlay, null, context, typeof(MediaService))
			);*/
			if (MainActivity.ServiceConnection.Connected)
			{
				if (MainActivity.ServiceConnection.Binder.Service.mediaPlayer.IsPlaying)
				{
					MainActivity.ServiceConnection.Binder.Service.Pause();
				}
				else
				{
					MainActivity.ServiceConnection.Binder.Service.Play();
				}
			}
		}

		public static void SetPlayButton(AppCompatActivity context)
		{
			
			play_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("play.png")));
		}

		public static void SetStopButton(AppCompatActivity context)
		{
			play_image.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets.Open("pause.png")));
		}

	  
		public static void populate_side_bar(AppCompatActivity context, AssetManager assets)
		{
			// basic  vars
			string? path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic)?.AbsolutePath;
			float scale = context.Resources.DisplayMetrics.Density;
			Typeface font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
			
			// song title image
			ImageView song_image = context.FindViewById<ImageView>(Resource.Id.song_cover);

			// texts
			TextView? songTitle = context.FindViewById<TextView>(Resource.Id.song_cover_name);
			TextView? songAuthor = context.FindViewById<TextView>(Resource.Id.side_author);
			TextView? songAlbum = context.FindViewById<TextView>(Resource.Id.side_album);

			
			if (songTitle != null) songTitle.Typeface = font;
			if (songAuthor != null) songAuthor.Typeface = font;
			if (songAlbum != null) songAlbum.Typeface = font;


			LinearLayout.LayoutParams song_title_params = new LinearLayout.LayoutParams(
				LinearLayout.LayoutParams.WrapContent,
				(int)(30 * scale + 0.5f)
			);
			song_title_params.SetMargins(
				0, (int)(10 * scale + 0.5f), 0, (int)(10 * scale + 0.5f)
			);


			songTitle.LayoutParameters = song_title_params;
			songTitle.Text = MainActivity.ServiceConnection?.Binder?.Service?.Current.Title;
			

			songAuthor.Text = MainActivity.ServiceConnection?.Binder?.Service?.Current.Artist.Title;
			songAuthor.Click += (sender, e) =>
			{
				/*
				Intent intent = new Intent(context, typeof(AllSongs));
				int? x = MainActivity.ServiceConnection?.Binder?.Service?.Current.Artist.GetHashCode();
				if (x is not { } hash) return;
				intent.PutExtra("link_author", hash);
				context.StartActivity(intent);
				*/
			};
				
			songAlbum.Text = MainActivity.ServiceConnection?.Binder?.Service?.Current.Album.Title;

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
					ImageView shuffleImg = (ImageView)shuffle.GetChildAt(0);
					shuffleImg?.SetImageBitmap(MainActivity.stateHandler.IsShuffling
						? BitmapFactory.DecodeStream(context.Assets?.Open("shuffle.png"))
						: BitmapFactory.DecodeStream(context.Assets?.Open("shuffle_on.png")));
					context.StartService(
						new Intent(MediaService.ActionShuffle, null, context, typeof(MediaService))
						.PutExtra("shuffle", !MainActivity.stateHandler.IsShuffling)
					);

				};

				LinearLayout repeat = cube_creator("small", scale, context, "repeat");
				player_buttons.Add(repeat, "repeat");
				repeat.Click += delegate
				{
					ImageView repeatImg = (ImageView)repeat.GetChildAt(0);
					switch (MainActivity.stateHandler.LoopState)
					{
						case 0:
							repeatImg?.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("repeat.png")));
							context.StartService(
								new Intent(MediaService.ActionToggleLoop, null, context, typeof(MediaService))
								.PutExtra("loopState", 1)
							);
							break;
						case 1:
							repeatImg?.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("repeat_one.png")));
                            context.StartService(
                            new Intent(MediaService.ActionToggleLoop, null, context, typeof(MediaService))
								.PutExtra("loopState", 2)
							);
                            break;
						case 2:
							repeatImg?.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("no_repeat.png")));
							context.StartService(
								new Intent(MediaService.ActionToggleLoop, null, context, typeof(MediaService))
								.PutExtra("loopState", 0)
							);
							break;
					}

					
				};


				LinearLayout last = cube_creator("small", scale, context, "left");
				last.Click += delegate
				{
					context.StartService(
						new Intent(MediaService.ActionPreviousSong, null, context, typeof(MediaService))
					);
				};
				player_buttons.Add(last, "last");

				LinearLayout play_pause = cube_creator("big", scale, context);
				play_pause.Click += (sender, e) => { pause_play(sender, e, context);  };
				player_buttons.Add(play_pause, "play_pause");

				LinearLayout next = cube_creator("small", scale, context, "right");
				next.Click += delegate
				{
					context.StartService(
						new Intent(MediaService.ActionNextSong, null, context, typeof(MediaService))
					);
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

			// song_image.SetImageResource(Resource.Mipmap.ic_launcher);

			// LinearLayout.LayoutParams song_image_params = new LinearLayout.LayoutParams(
			// 	(int)(120 * scale + 0.5f),
			// 	(int)(120 * scale + 0.5f)
			// );
			// song_image.LayoutParameters = song_image_params;

			// set the image
			song_image.SetImageBitmap(
				MainActivity.ServiceConnection?.Binder?.Service?.Current.Image
			);


            // progress song

            TextView prog_time = context.FindViewById<TextView>(Resource.Id.progress_time);
			prog_time.Click += delegate
			{
				MainActivity.stateHandler.ProgTimeState = !MainActivity.stateHandler.ProgTimeState;

            };

            if (MainActivity.stateHandler.IsPlaying) {
                TextView const_time = context.FindViewById<TextView>(Resource.Id.end_time);
                SeekBar sek = context.FindViewById<SeekBar>(Resource.Id.seek);
                const_time.Text = converts_millis_to_seconds_and_minutes(MainActivity.stateHandler.Duration);
                sek.Max = MainActivity.stateHandler.Duration / 1000;
                sek.SetProgress((MainActivity.stateHandler.CurrentPosition / 1000), true);
                if (MainActivity.stateHandler.ProgTimeState)
                {
                    prog_time.Text = "-" + converts_millis_to_seconds_and_minutes(MainActivity.stateHandler.Duration - (sek.Progress * 1000));
                }
                else
                {
                    prog_time.Text = converts_seconds_to_seconds_and_minutes(sek.Progress);
                }
                sek.ProgressChanged += (object sender, SeekBar.ProgressChangedEventArgs e) =>
                {
                    if (e.FromUser)
                    {
#if DEBUG
                        MyConsole.WriteLine(sek.Progress.ToString());
#endif
						context.StartService(
							new Intent(MediaService.ActionSeekTo, null, context, typeof(MediaService))
							.PutExtra("millis", sek.Progress * 1000)
						);
                    }
                };
				StartMovingProgress(MainActivity.stateHandler.cts.Token, context);
            }         
		}

		public static void StartMovingProgress(CancellationToken token, AppCompatActivity context)
		{
            SeekBar sek = context.FindViewById<SeekBar>(Resource.Id.seek);
            TextView prog_time = context.FindViewById<TextView>(Resource.Id.progress_time);
            _ = Interval.SetIntervalAsync(() =>
			{
                sek.SetProgress((MainActivity.stateHandler.CurrentPosition / 1000), true);
                if (MainActivity.stateHandler.ProgTimeState)
				{
                    prog_time.Text = "-"+converts_millis_to_seconds_and_minutes(MainActivity.stateHandler.Duration - (sek.Progress * 1000));
                }
				else
				{
					prog_time.Text = converts_seconds_to_seconds_and_minutes(sek.Progress);
				}

            }, 1000, token);
		}
        private static string converts_millis_to_seconds_and_minutes(int millis)
        {
            return $"{millis / (1000 * 60) % 60}:"+(millis / 1000 % 60).ToString().PadLeft(2, '0');
        }

        private static string converts_seconds_to_seconds_and_minutes(int seconds)
        {
            return $"{seconds / 60}:" + (seconds % 60).ToString().PadLeft(2, '0');
        }

    }
}