using System;
using Android.Content;
using Android.Views;
using AndroidX.AppCompat.App;
using System.Collections.Generic;
using Android.Widget;
using Android.Graphics;
using Android.Content.Res;
using System.Threading;
using Java.Util;
using MWP.BackEnd.Player;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP
{
	public static class SidePlayer
	{
		//private static Dictionary<LinearLayout, string> _playerButtons = new Dictionary<LinearLayout, string>();

		static ImageView? _playImage;

		private static LinearLayout cube_creator(string size, float scale, AppCompatActivity context, string sides = "idk")
		{

			LinearLayout cube = new LinearLayout(context);

			switch (size)
			{
				case "big":
					LinearLayout.LayoutParams bigCubeParams = new LinearLayout.LayoutParams(
						(int)(50 * scale + 0.5f),
						(int)(50 * scale + 0.5f)
					);
					bigCubeParams.SetMargins(
						(int)(10 * scale + 0.5f), (int)(10 * scale + 0.5f), // left, top
						(int)(10 * scale + 0.5f), (int)(10 * scale + 0.5f) // right, bottom
					);

					cube.LayoutParameters = bigCubeParams;
					cube.SetGravity(GravityFlags.Center);
					cube.Orientation = Android.Widget.Orientation.Horizontal;
					cube.SetBackgroundResource(Resource.Drawable.rounded_light);


					_playImage = new ImageView(context);
					LinearLayout.LayoutParams playImageParams = new LinearLayout.LayoutParams(
						(int)(40 * scale + 0.5f),
						(int)(40 * scale + 0.5f)
					);
					_playImage.LayoutParameters = playImageParams;

					if (MainActivity.ServiceConnection.Binder?.Service.mediaPlayer?.IsPlaying ?? false)
						_playImage.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("pause.png")));
					else
						_playImage.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("play.png")));


					cube.AddView(_playImage);

					break;
				case "small":
					LinearLayout.LayoutParams smallCubeParams = new LinearLayout.LayoutParams(
						(int)(30 * scale + 0.5f),
						(int)(30 * scale + 0.5f)
					);
					smallCubeParams.SetMargins (
					   (int)(10 * scale + 0.5f), (int)(10 * scale + 0.5f), // left, top
					   (int)(10 * scale + 0.5f), (int)(10 * scale + 0.5f) // right, bottom
					);

					cube.LayoutParameters = smallCubeParams;
					cube.SetGravity(GravityFlags.Center);
					cube.Orientation = Android.Widget.Orientation.Horizontal;
					

					ImageView lastImage = new ImageView(context);
					LinearLayout.LayoutParams lastImageParams;


					switch (sides)
					{
						case "right":
							lastImage.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("right.png")));
							lastImageParams = new LinearLayout.LayoutParams(
							   ViewGroup.LayoutParams.MatchParent,
							   ViewGroup.LayoutParams.MatchParent
							);
							lastImage.LayoutParameters = lastImageParams;
							cube.SetBackgroundResource(Resource.Drawable.rounded_light);
							break;
						case "left":
							lastImageParams = new LinearLayout.LayoutParams(
							   ViewGroup.LayoutParams.MatchParent,
							   ViewGroup.LayoutParams.MatchParent
							);
							lastImage.LayoutParameters = lastImageParams;
							lastImage.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("left.png")));
							cube.SetBackgroundResource(Resource.Drawable.rounded_light);
							break;
						case "shuffle":
							lastImageParams = new LinearLayout.LayoutParams(
							  (int)(20 * scale + 0.5f),
							  (int)(20 * scale + 0.5f)
							);
							lastImage.LayoutParameters = lastImageParams;
							if (MainActivity.ServiceConnection.Binder?.Service.QueueObject.IsShuffled ?? false)
							{
								lastImage.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("shuffle_on.png")));
							}
							else
							{
								lastImage.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("shuffle.png")));
							}
							
							break;
						case "repeat":
							lastImageParams = new LinearLayout.LayoutParams(
							 (int)(20 * scale + 0.5f),
							 (int)(20 * scale + 0.5f)
							);
							lastImage.LayoutParameters = lastImageParams;
							switch (MainActivity.ServiceConnection.Binder?.Service.QueueObject.LoopState ?? Enums.LoopState.None)
							{
								case Enums.LoopState.None:
									lastImage.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("no_repeat.png")));
									break;
								case Enums.LoopState.All:
									lastImage.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("repeat.png")));
									break;
								case Enums.LoopState.Single:
									lastImage.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("repeat_one.png")));
									break;
							}
							break;
					}



					cube.AddView(lastImage);

					break;
			}

			return cube;
		}

		private static void pause_play(object sender, EventArgs e, AppCompatActivity context)
		{
			if (!MainActivity.ServiceConnection.Connected) return;
			if (MainActivity.ServiceConnection.Binder?.Service.mediaPlayer?.IsPlaying ?? false)
			{
				MainActivity.ServiceConnection.Binder?.Service.Pause();
			}
			else
			{
				MainActivity.ServiceConnection.Binder?.Service.Play();
			}
		}

		public static void SetPlayButton(AppCompatActivity context)
		{
			_playImage?.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("play.png")));
		}

		public static void SetStopButton(AppCompatActivity context)
		{
			_playImage?.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("pause.png")));
		}

	  
		public static void populate_side_bar(AppCompatActivity context, AssetManager assets)
		{
			// basic  vars
			if (context.Resources is { DisplayMetrics: not null })
			{
				float scale = context.Resources.DisplayMetrics.Density;
				Typeface? font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
			
				// song title image
				ImageView? songImage = context.FindViewById<ImageView>(Resource.Id.song_cover);

				// texts
				TextView? songTitle = context.FindViewById<TextView>(Resource.Id.song_cover_name);
				TextView? songAuthor = context.FindViewById<TextView>(Resource.Id.side_author);
				TextView? songAlbum = context.FindViewById<TextView>(Resource.Id.side_album);

			
				if (songTitle != null) songTitle.Typeface = font;
				if (songAuthor != null) songAuthor.Typeface = font;
				if (songAlbum != null) songAlbum.Typeface = font;


				LinearLayout.LayoutParams songTitleParams = new LinearLayout.LayoutParams(
					ViewGroup.LayoutParams.WrapContent,
					(int)(30 * scale + 0.5f)
				);
				songTitleParams.SetMargins(
					0, (int)(10 * scale + 0.5f), 0, (int)(10 * scale + 0.5f)
				);


				if (songTitle != null)
				{
					songTitle.LayoutParameters = songTitleParams;
					songTitle.Text = MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Title ?? "No Name";
				}


				if (songAuthor != null)
				{
					songAuthor.Text = MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Artist.Title ??
					                  "No Artist";
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
				}

				if (songAlbum != null)
					songAlbum.Text = MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Album.Title ?? "No Album";

				/*
			 * player buttons
			 */
				LinearLayout? buttonsMainLin = context.FindViewById<LinearLayout>(Resource.Id.player_buttons);
				buttonsMainLin?.RemoveAllViews();
				//_playerButtons.Clear();
				{

				

					LinearLayout shuffle = cube_creator("small", scale, context, "shuffle");
					//_playerButtons.Add(shuffle, "shuffle");
					shuffle.Click += delegate
					{
						ImageView? shuffleImg = (ImageView?)shuffle.GetChildAt(0);
						shuffleImg?.SetImageBitmap(MainActivity.ServiceConnection.Binder?.Service.QueueObject.IsShuffled ?? false
							? BitmapFactory.DecodeStream(context.Assets?.Open("shuffle.png"))
							: BitmapFactory.DecodeStream(context.Assets?.Open("shuffle_on.png")));
					
						MainActivity.ServiceConnection.Binder?.Service.Shuffle(!MainActivity.ServiceConnection.Binder?.Service.QueueObject.IsShuffled ?? false);
					};

					LinearLayout repeat = cube_creator("small", scale, context, "repeat");
					//_playerButtons.Add(repeat, "repeat");
					repeat.Click += delegate
					{
						ImageView? repeatImg = (ImageView?)repeat.GetChildAt(0);
						switch (MainActivity.ServiceConnection.Binder?.Service.QueueObject.LoopState ?? Enums.LoopState.None)
						{
							case Enums.LoopState.None:
								repeatImg?.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("repeat.png")));
								MainActivity.ServiceConnection.Binder?.Service.ToggleLoop(Enums.LoopState.All);
								break;
							case Enums.LoopState.All:
								repeatImg?.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("repeat_one.png")));
								MainActivity.ServiceConnection.Binder?.Service.ToggleLoop(Enums.LoopState.Single);
								break;
							case Enums.LoopState.Single:
								repeatImg?.SetImageBitmap(BitmapFactory.DecodeStream(context.Assets?.Open("no_repeat.png")));
								MainActivity.ServiceConnection.Binder?.Service.ToggleLoop(Enums.LoopState.None);
								break;
						}

					
					};


					LinearLayout last = cube_creator("small", scale, context, "left");
					last.Click += delegate
					{
						MainActivity.ServiceConnection.Binder?.Service.PreviousSong();
					};
					//_playerButtons.Add(last, "last");

					LinearLayout playPause = cube_creator("big", scale, context);
					playPause.Click += (sender, e) => { pause_play(sender, e, context);  };
					//_playerButtons.Add(playPause, "play_pause");

					LinearLayout next = cube_creator("small", scale, context, "right");
					next.Click += delegate
					{
						MainActivity.ServiceConnection.Binder?.Service.NextSong();
					};
					//_playerButtons.Add(next, "next");


					buttonsMainLin?.AddView(shuffle);
					buttonsMainLin?.AddView(last);
					buttonsMainLin?.AddView(playPause);
					buttonsMainLin?.AddView(next);
					buttonsMainLin?.AddView(repeat);
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
				songImage?.SetImageBitmap(
					MainActivity.ServiceConnection.Binder?.Service.QueueObject.Current.Image ?? new Song("No Name", new DateTime(), "Default").Image
				);
			}


			// progress song

            TextView? progTime = context.FindViewById<TextView>(Resource.Id.progress_time);
            if (progTime != null)
            {
	            progTime.Click += delegate
	            {
		            MainActivity.stateHandler.ProgTimeState = !MainActivity.stateHandler.ProgTimeState;
	            };

	            if (!(MainActivity.ServiceConnection.Binder?.Service.mediaPlayer?.IsPlaying ?? false)) return;
	            TextView? constTime = context.FindViewById<TextView>(Resource.Id.end_time);
	            SeekBar? sek = context.FindViewById<SeekBar>(Resource.Id.seek);
	            if (constTime != null)
		            constTime.Text = converts_millis_to_seconds_and_minutes(MainActivity.ServiceConnection.Binder?.Service.mediaPlayer?.Duration ?? 0);
	            if (sek != null)
	            {
		            sek.Max = (MainActivity.ServiceConnection.Binder?.Service.mediaPlayer?.Duration ?? 0) / 1000;
		            sek.SetProgress((MainActivity.ServiceConnection.Binder?.Service.mediaPlayer?.CurrentPosition ?? 0) / 1000, true);
		            if (MainActivity.stateHandler.ProgTimeState)
		            {
			            progTime.Text = "-" +
			                            converts_millis_to_seconds_and_minutes((MainActivity.ServiceConnection.Binder?.Service.mediaPlayer?.Duration ?? 0) - sek.Progress * 1000);
		            }
		            else
		            {
			            progTime.Text = converts_seconds_to_seconds_and_minutes(sek.Progress);
		            }

		            sek.ProgressChanged += (_, e) =>
		            {
			            if (!e.FromUser) return;
			            MainActivity.ServiceConnection.Binder?.Service.SeekTo(sek.Progress * 1000);
		            };
	            }

	            StartMovingProgress(MainActivity.stateHandler.SongProgressCts.Token, context);
            }
		}

		public static void StartMovingProgress(CancellationToken token, AppCompatActivity context)
		{
            SeekBar? sek = context.FindViewById<SeekBar>(Resource.Id.seek);
            TextView? progTime = context.FindViewById<TextView>(Resource.Id.progress_time);
            _ = Interval.SetIntervalAsync(() =>
			{
                sek?.SetProgress((MainActivity.ServiceConnection.Binder?.Service.mediaPlayer?.CurrentPosition ?? 0) / 1000, true);
                if (MainActivity.stateHandler.ProgTimeState)
                {
	                if (progTime == null) return;
	                if (sek != null)
		                progTime.Text = "-" +
		                                converts_millis_to_seconds_and_minutes(
			                                (MainActivity.ServiceConnection.Binder?.Service.mediaPlayer?.Duration ?? 0) - (sek.Progress * 1000));
                }
				else
                {
	                if (progTime == null) return;
	                if (sek != null)
		                progTime.Text = converts_seconds_to_seconds_and_minutes(sek.Progress);
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