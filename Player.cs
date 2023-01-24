using Android.App;
using Android.Content;
using Android.Media;
using Android.Media.Session;
using AndroidX.AppCompat.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AndroidApp = Android.App.Application;
using static Android.Renderscripts.Sampler;
using static Java.Util.Jar.Attributes;
using Android.Views.Animations;

namespace Ass_Pain

{
	[BroadcastReceiver(Enabled = false, Exported = false)]
	[IntentFilter(new[] { AudioManager.ActionAudioBecomingNoisy })]
	public class Slovenska_prostituka : BroadcastReceiver
	{
		Local_notification_service notification_service = new Local_notification_service();
        public CancellationTokenSource cts = new CancellationTokenSource();
		private static MediaPlayer player = new MediaPlayer();
        MediaSession session = new MediaSession(AndroidApp.Context, "MusicService");
        AudioManager manager = AudioManager.FromContext(AndroidApp.Context);
        private List<string> queue = new List<string>();
		private List<string> orignalQueue = new List<string>();
		private int index = 0;
		private bool used = false;
		AppCompatActivity view;
		Int16 loopState = 0;
		private bool loopSingle = false;
		private bool loopAll = false;
		private bool shuffle = false;
		private bool progTimeState = false;
		/*public bool IsLoopingAll
		{
			get { return loopAll; }
		}*/

		/*public bool IsLoopingSingle
		{
			get { return loopSingle; }
		}*/
		public Int16 LoopState
		{
			get { return loopState; }
		}
		public bool IsShuffling
		{
			get { return shuffle; }
		}
		public bool IsPlaying
		{
			get { return player.IsPlaying; }
		}
		public int Duration
		{
			get {
                try
                {
                    return player.Duration;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return 0;
                }
            }
		}
		public int CurrentPosition
		{
			get {
                try
                {
					return player.CurrentPosition;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
					return 0;
                }
            }
		}
		public int SeekTo
		{
			set { 
				try
				{
					player.SeekTo(value); 
				} 
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
		}
		public bool ProgTimeState
		{
			set { progTimeState = value; }
			get { return progTimeState; }
		}


		public Slovenska_prostituka()
		{

			player.Completion += NextSong;
			player.Prepared += (sender, ea) =>
			{
                var request = manager.RequestAudioFocus(this, Android.Media.Stream.Music, AudioFocus.Gain);
                if (request != AudioFocusRequest.Granted)
                {
                    // handle any failed requests
                }
                if (!session.Active)
                {
                    session.Active = true;
                }
                used = true;
			};
			session.SetCallback(new MediaSessionCallback());
            session.SetFlags(MediaSessionFlags.HandlesMediaButtons |
            MediaSessionFlags.HandlesTransportControls);


        }

        public override void OnReceive(Context context, Intent intent)
		{
			Console.WriteLine("noisy");
			player.Pause();
		}

		public void SetView(AppCompatActivity new_view)
		{
			view = new_view;
		}

		public string NowPlaying()
		{
			try
			{
				return queue[index];
			}
			catch
			{
				return "prostitutka ja nai";
			}
		}

		public void Play()
		{
			if (used)
			{
				player.Reset();
			}
			player.SetDataSource(queue[index]);
			side_player.populate_side_bar(view);
			player.Prepare();
			player.Start();
			side_player.populate_side_bar(view);
			cts.Cancel();
			cts = new CancellationTokenSource();
			side_player.StartMovingProgress(cts.Token, view);
			notification_service.song_control_notification();
		}

		public void NextSong(object sender = null, EventArgs e = null)
		{
			if (queue.Count > index)
			{
				index++;
				Play();
			} else if (loopAll)
			{
				index = 0;
				Play();
			}
		}

		public void PreviousSong(object sender = null, EventArgs e = null)
		{
			try
			{
				if(player.CurrentPosition > 10000)//if current song is playing longer than 10 seconds
				{
					player.SeekTo(0);
					return;
				}
				if (used)
				{
					player.Reset();
				}
				index -= 2;
				player.SetDataSource(queue[index]);
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
				index = 0;
				player.SetDataSource(queue[index]);
			}
				side_player.populate_side_bar(view);
				index++;
				player.Prepare();
				player.Start();
		}

		public void Resume()
		{
			Console.WriteLine("Resumed");
			if (used)
			{
				player.Start();
			}
		}
		public void Stop()
		{
			Console.WriteLine("Stopped");
			player.Pause();
		}

		public void TogglePlayButton(AppCompatActivity context)
		{
			if (player.IsPlaying)
			{
				side_player.SetPlayButton(context);
				cts.Cancel();
				player.Pause();
			}
			else
			{
				if (used)
				{
					player.Start();
					cts.Cancel();
					cts = new CancellationTokenSource();
					side_player.StartMovingProgress(cts.Token, view);
					side_player.SetStopButton(context);
				}
			}
		}

		///<summary>
		///Clears queue and resets index to 0
		///</summary>
		public void ClearQueue(object sender = null, EventArgs e = null)
		{
			queue = new List<string>();
			index = 0;
		}

		///<summary>
		///Generates clear queue from single track or entire album/author and resets index to 0
		///</summary>
		public void GenerateQueue(string source)
		{
			side_player.SetStopButton(view);
			index = 0;
			if (FileManager.IsDirectory(source))
			{
				GenerateQueue(FileManager.GetSongs(source));
			}
			else
			{
				queue = new List<string>{source};
				Play();
			}
		}

		///<summary>
		///Generates clear queue from list and resets index to 0
		///</summary>
		public void GenerateQueue(List<string> source, int i = 0)
		{
			side_player.SetStopButton(view);
			queue = source;
			index = i;
			Shuffle(shuffle);
			Play();
		}

		///<summary>
		///Adds single track or entire album/author to queue
		///</summary>
		public void AddToQueue(string addition)
		{
			if (FileManager.IsDirectory(addition))
			{
				AddToQueue(FileManager.GetSongs(addition));
			}
			else
			{
				queue.Add(addition);
			}
		}

		///<summary>
		///Adds list of songs to queue
		///</summary>
		public void AddToQueue(List<string> addition)
		{
			queue.AddRange(addition);
		}

		///<summary>
		///Adds song or entire album/author as first to queue
		///</summary>
		public void PlayNext(string addition)
		{
			if (FileManager.IsDirectory(addition))
			{
				PlayNext(FileManager.GetSongs(addition));
			}
			else
			{
				queue.Insert(0, addition);
			}
			
		}
		
		///<summary>
		///Adds song list as first to queue
		///</summary>
		public void PlayNext(List<string> addition)
		{
			addition.AddRange(queue);
			queue = addition;
		}

		public void Shuffle(bool newShuffleState)
		{
			if (newShuffleState && queue.Count > 0)
			{
				orignalQueue = queue;
				string tmp = queue.Pop(index);
				index = 0;
				queue.Shuffle();
				queue = queue.Prepend(tmp).ToList();
			}
			else
			{
				if(orignalQueue.Count > 0)
				{
					index = orignalQueue.IndexOf(queue[index]);
					queue = orignalQueue;
				}
				orignalQueue = new List<string>();
			}
			shuffle = newShuffleState;
		}

		public void ToggleLoop(Int16 state)
		{
			loopState = state;
			switch (state) { 
				case 0:
					loopAll = false;
					loopSingle = false;
                    player.Looping = false;
                    break;
				case 1:
					loopAll = true;
					loopSingle = false;
                    player.Looping = false;
                    break;
				case 2:
					loopAll = false;
					loopSingle = true;
					player.Looping = true;
					break;
				default: break;
			}
		}
	}
}