using Android.App;
using AndroidApp = Android.App.Application;
using Android.Content;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using Android.Content.PM;
using Android.Graphics;
using Android.Support.V4.Media.Session;
using Android.Support.V4.Media;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain
{
	[Service(ForegroundServiceType = ForegroundService.TypeMediaPlayback, Label = "@string/service_name")]
	//TODO: https://developer.android.com/training/cars/media
	public class MediaService : Service, AudioManager.IOnAudioFocusChangeListener
	{
		///<summary>
		///Requests focus and starts playing new song or resumes playback if playback was paused
		///</summary>
		public const string ActionPlay = "ActionPlay";

		///<summary>
		///Pauses playback
		///</summary>
		public const string ActionPause = "ActionPause";

		///<summary>
		///Stops playing and abandons focus
		///</summary>
		public const string ActionStop = "ActionStop";
		
		///<summary>
		///Shuffles or unshuffles queue and updates shuffling for all new queues oposite to last state
		///</summary>
		public const string ActionShuffle = "ActionShuffle";

		///<summary>
		///Changes current loop state based on last state
		///</summary>
		public const string ActionToggleLoop = "ActionToggleLoop";

		///<summary>
		///Toggles between playing and being paused
		///</summary>
		public const string ActionTogglePlay = "ActionTogglePlay";

		///<summary>
		///Plays next song in queue
		///</summary>
		public const string ActionNextSong = "ActionNextSong";

		///<summary>
		///Plays previous song in queue
		///</summary>
		public const string ActionPreviousSong = "ActionPreviousSong";

		///<summary>
		///Clears queue and resets index to 0
		///</summary>
		public const string ActionClearQueue = "ActionClearQueue";
		///<summary>
		///Moves playback of current song intent extra int time in milliseconds
		///</summary>
		public const string ActionSeekTo = "ActionSeekTo";

		private MediaSessionCompat? session;
		
		/// <summary>
		/// handle for current media session
		/// </summary>
		public MediaSessionCompat Session
		{
			get
			{
				if (session == null)
				{
					InnitSession();
				}

				return session!;
			}
		}
		
		public MediaPlayer? mediaPlayer { get; private set; }
		private AudioManager? audioManager;
		private AudioFocusRequestClass? audioFocusRequest;
		private readonly Local_notification_service notificationService = new Local_notification_service();
		public long Actions { get; private set; }
		private bool isFocusGranted;
		private bool isUsed;
		private bool lostFocusDuringPlay;
		public bool IsPaused { get; private set; }
		public bool IsShuffled { get; private set; }
		public bool loopAll { get; private set; }
		public bool loopSingle { get; private set; }
		private bool isSkippingToNext;
		private bool isSkippingToPrevious;
		private bool isBuffering = true;
		public int LoopState { get; private set; }
		private int i;
		public int Index
		{
			get => i;
			private set { i = value.KeepPositive(); MainActivity.stateHandler.setIndex(ref i); }
		}
		// private List<Song> q = new List<Song>();
		public List<Song> Queue = new List<Song>();
        private List<Song> originalQueue = new List<Song>();
        internal readonly AutoResetEvent shuffling = new AutoResetEvent(true);

        public Song Current
        {
	        get
	        {
		        try
		        {
			        return Queue[Index];
		        }
		        catch
		        {
			        return new Song("No Name", new DateTime(), "Default");
		        }
	        }
        }

        public override void OnCreate()
		{
			base.OnCreate();
			InnitPlayer();
			InnitAudioManager();
			InnitSession();
			InnitFocusRequest();
			InnitNotification();
#if DEBUG
            MyConsole.WriteLine("CREATING NEW SESSION");
#endif
		}
		public override void OnDestroy()
		{
			CleanUp();
			base.OnDestroy();
		}

		///<summary>
		///Creates new MediaPlayer
		///</summary>
		private void InnitPlayer()
		{
			mediaPlayer = new MediaPlayer();
			mediaPlayer.Prepared += delegate
			{
				isUsed = true;
			};
			mediaPlayer.Completion += delegate
			{
				NextSong();
			};
			mediaPlayer.BufferingUpdate += (_, e) =>
			{
				isBuffering = e.Percent != 100;
			};
			mediaPlayer.SeekComplete += delegate
			{
                UpdatePlaybackState();
            };
			MediaPlayer m = mediaPlayer;
			MainActivity.stateHandler.setMediaPlayer(ref m);
        }

		///<summary>
		///Creates new AudioManager
		///</summary>
		private void InnitAudioManager()
		{
			if (ApplicationContext != null) audioManager = AudioManager.FromContext(ApplicationContext);
		}

		///<summary>
		///Creates new MediaSession
		///</summary>
		private void InnitSession()
		{
			if (mediaPlayer == null)
			{
				InnitPlayer();
			}
			session = new MediaSessionCompat(AndroidApp.Context, "MusicService");
			session.SetCallback(new MediaSessionCallback());
			session.SetFlags((int)(MediaSessionFlags.HandlesMediaButtons | MediaSessionFlags.HandlesTransportControls));
			//session.SetSessionActivity()
		}

		///<summary>
		///Creates new FocusRequest
		///</summary>
		private void InnitFocusRequest()
		{
			audioFocusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
											   .SetAudioAttributes(new AudioAttributes.Builder().SetLegacyStreamType(Android.Media.Stream.Music)?.SetUsage(AudioUsageKind.Media)?.Build()!)
											   .SetOnAudioFocusChangeListener(this)
											   .Build();
		}

		private void InnitNotification()
		{

			if (session == null)
			{
				InnitSession();
			}
			MediaMetadataCompat.Builder metadataBuilder = new MediaMetadataCompat.Builder();
			try
			{
				metadataBuilder.PutBitmap(MediaMetadataCompat.MetadataKeyArt, Queue[0].Image);
			}
			catch (Exception)
			{
				if (Application.Context.Assets != null)
				{
					metadataBuilder.PutBitmap(MediaMetadataCompat.MetadataKeyArt, MusicBaseClassStatic.placeholder);	
				}
			}
			
			metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyDisplayTitle, "No Song");
			metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyDisplaySubtitle, "No Artist");
			session?.SetMetadata(metadataBuilder.Build());
			
			const long position = PlaybackState.PlaybackPositionUnknown;
			const PlaybackStateCode state = PlaybackStateCode.None;

			PlaybackStateCompat.Builder? stateBuilder = new PlaybackStateCompat.Builder()
					.SetActions(GetAvailableActions());
			if (stateBuilder != null)
			{
				stateBuilder.SetState((int)state, position, 1.0f);
				int icon = LoopState switch
				{
					0 => Resource.Drawable.no_repeat,
					1 => Resource.Drawable.repeat,
					_ => Resource.Drawable.repeat_one
				};
				stateBuilder.AddCustomAction("loop", "loop", icon);
				stateBuilder.AddCustomAction("shuffle", "shuffle", IsShuffled ? Resource.Drawable.no_shuffle2 : Resource.Drawable.shuffle2);
				session?.SetPlaybackState(stateBuilder.Build());
			}

			if (session?.SessionToken != null) notificationService.song_control_notification(session.SessionToken);
			StartForeground(notificationService.NotificationId, notificationService.Notification, ForegroundService.TypeMediaPlayback);
		}

		public void OnAudioFocusChange([GeneratedEnum] AudioFocus focusChange)
		{
			switch (focusChange)
			{
				case AudioFocus.Gain:
					mediaPlayer?.SetVolume(1.0f, 1.0f);
					if (lostFocusDuringPlay)
					{
						mediaPlayer?.Start();
						lostFocusDuringPlay = false;
					}
					break;
				case AudioFocus.Loss:
					//CleanUp();
                    Pause();
					isFocusGranted = false;
                    break;
				case AudioFocus.LossTransient:
					if (mediaPlayer is { IsPlaying: true })
					{
						lostFocusDuringPlay = true;
					}
					Pause();
					break;
				case AudioFocus.LossTransientCanDuck:
					mediaPlayer?.SetVolume(0.25f, 0.25f);
					break;
			}
		}

		/// <inheritdoc />
		public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
		{
			switch (intent?.Action)
			{
				case ActionPlay:
					Play();
					break;
				case ActionPause:
					Pause();
					break;
				case ActionStop:
					Stop();
					break;
				case ActionShuffle:
					Shuffle(intent.GetBooleanExtra("shuffle", false));
					break;
				case ActionToggleLoop:
					ToggleLoop(intent.GetIntExtra("loopState", 0));
#if DEBUG
                    MyConsole.WriteLine("TOGGLE LOOP SWITCH");
#endif
                    break;
				case ActionTogglePlay:
					if ((bool)mediaPlayer?.IsPlaying)
					{
						Pause();
					}
					else
					{
						if (isUsed)
						{
							Play();
						}
					}
					break;
				case ActionNextSong:
					NextSong();
					break;
				case ActionPreviousSong:
					PreviousSong();
					break;
				case ActionSeekTo:
					SeekTo(intent.GetIntExtra("millis", 0));
					break;
				case ActionClearQueue:
					ClearQueue();
					break;
			}
            intent?.Dispose();
			return StartCommandResult.Sticky;
		}
		private long GetAvailableActions()
		{
			if (mediaPlayer == null)
			{
				InnitPlayer();
			}
			if (mediaPlayer is { IsPlaying: true })
			{
				Actions = PlaybackState.ActionPause | PlaybackState.ActionStop;
			}else{
				Actions = PlaybackState.ActionPlay;
			}
			
			if (Queue.Count == 0)
			{
				return Actions;
			}
			
			Actions |= PlaybackState.ActionSeekTo;
			
			if ((Index > 0 || loopAll) && !loopSingle)
			{
				Actions |= PlaybackState.ActionSkipToPrevious;
			}
			if ((Index < Queue.Count -1 || loopAll) && !loopSingle)
			{
				Actions |= PlaybackState.ActionSkipToNext;
			}
			return Actions;
		}

		private void UpdatePlaybackState()
		{
			if (session == null)
			{
				InnitSession();
			}
			long position = PlaybackState.PlaybackPositionUnknown;
			PlaybackStateCode state;
			if (mediaPlayer is { IsPlaying: true })
			{
				position = mediaPlayer.CurrentPosition;
				state = PlaybackStateCode.Playing;
				side_player.SetStopButton(MainActivity.stateHandler.view);
				MainActivity.stateHandler.cts.Cancel();
				MainActivity.stateHandler.cts = new CancellationTokenSource();
				side_player.StartMovingProgress(MainActivity.stateHandler.cts.Token, MainActivity.stateHandler.view);
			}
			else if (IsPaused)
			{
				state = PlaybackStateCode.Paused;
				position = mediaPlayer?.CurrentPosition ?? 0;
				side_player.SetPlayButton(MainActivity.stateHandler.view);
				MainActivity.stateHandler.cts.Cancel();
			}
			else if (isSkippingToNext)
			{
				state = PlaybackStateCode.SkippingToNext;
			}
			else if (isSkippingToPrevious)
			{
				state = PlaybackStateCode.SkippingToPrevious;
			}
			else if (isBuffering)
			{
				state = PlaybackStateCode.Buffering;
			}
			else
			{
				state = PlaybackStateCode.None;
			}

			PlaybackStateCompat.Builder? stateBuilder = new PlaybackStateCompat.Builder()
				.SetActions(GetAvailableActions());
			if (stateBuilder != null)
			{
				stateBuilder.SetState((int)state, position, 1.0f);
				int icon = LoopState switch
				{
					0 => Resource.Drawable.no_repeat,
					1 => Resource.Drawable.repeat,
					_ => Resource.Drawable.repeat_one
				};
				stateBuilder.AddCustomAction("loop", "loop", icon);
				stateBuilder.AddCustomAction("shuffle", "shuffle", IsShuffled ? Resource.Drawable.no_shuffle2 : Resource.Drawable.shuffle2);
				session?.SetPlaybackState(stateBuilder.Build());
			}

			if (Assets != null) side_player.populate_side_bar(MainActivity.stateHandler.view, Assets);
			notificationService.Notify();
		}

		private void UpdateMetadata()
		{
			if (mediaPlayer == null || Queue.Count <= 0) return;
			if (session == null)
			{
				InnitSession();
			}
			MediaMetadataCompat.Builder metadataBuilder = new MediaMetadataCompat.Builder();
			Song song = Queue[Index];
			Bitmap image = song.Image;
			metadataBuilder.PutBitmap(MediaMetadataCompat.MetadataKeyArt,
				image);
			metadataBuilder.PutBitmap(MediaMetadataCompat.MetadataKeyAlbumArt,
				image);
			// To provide most control over how an item is displayed set the
			// display fields in the metadata
			metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyDisplayTitle,
				song.Title);
			// And at minimum the title and artist for legacy support
			metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyTitle,
				song.Title);
			metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyDisplaySubtitle,
				song.Album + song.Artist.Title);
			metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyAlbum,
				song.Album.Title);
			metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyAlbumArtist,
				song.Artist.Title);
			metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyArtist,
				song.Artist.Title);
			metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyAuthor,
				song.Artist.Title);
			metadataBuilder.PutLong(MediaMetadataCompat.MetadataKeyDuration,
				mediaPlayer.Duration);
			// Add any other fields you have for your data as well
			session?.SetMetadata(metadataBuilder.Build());

			UpdatePlaybackState();
		}



		///<summary>
		///Requests focus and starts playing new song or resumes playback if playback was paused
		///</summary>
		public void Play()
		{
			if (Queue.Count == 0)
			{
				try
				{
					GenerateQueue(MainActivity.stateHandler.Songs, 0, false);
				}
				catch (Exception e)
				{
#if DEBUG
					MyConsole.WriteLine(e);
#endif
					return;
				}
			}
			if (!RequestFocus())
			{
				return;
			}
			if (session == null)
			{
				InnitSession();
			}
			if (!session!.Active)
			{
				session.Active = true;
			}
			if (!notificationService.IsCreated)
			{
				if (session.SessionToken != null) notificationService.song_control_notification(session.SessionToken);
				//StartForeground(notificationService.NotificationId, notificationService.Notification, ForegroundService.TypeMediaPlayback);
				//android:foregroundServiceType="mediaPlayback"
				StartForeground(notificationService.NotificationId, notificationService.Notification);
			}
			if (mediaPlayer == null)
			{
				InnitPlayer();
			}
			if (!IsPaused)
			{
				mediaPlayer!.Reset();
#if DEBUG
                MyConsole.WriteLine($"SERVICE INDEX {Index}");
                MyConsole.WriteLine($"SERVICE QUEUE {Queue.Count}");
#endif


                // if (!File.Exists(Queue[Index].Path))
                // {
                // 	NextSong();
                // 	return;
                // }
                mediaPlayer.SetDataSource(Current.Path);
				mediaPlayer.Prepare();
			}
			mediaPlayer!.Start();

			if (!IsPaused)
			{
				UpdateMetadata();
			}
			else
			{
				IsPaused = false;
				UpdatePlaybackState();
			}

			isSkippingToNext = false;
			isSkippingToPrevious = false;
		}
		
		///<summary>
		///Pauses playback
		///</summary>
		public void Pause()
		{
			mediaPlayer?.Pause();
			IsPaused = true;
			UpdatePlaybackState();
		}

		///<summary>
		///Stops playing and abandons focus
		///</summary>
		private void Stop()
		{
			mediaPlayer?.Stop();
			AbandonFocus();
			//updateMetadata();
			notificationService.destroy_song_control();
		}

		///<summary>
		///Plays next song in queue
		///</summary>
		public void NextSong()
		{
			if (mediaPlayer == null) return;
			if (loopSingle)
			{
				Play();
				return;
			}
			isSkippingToNext = true;
			IsPaused = false;
			if (Queue.Count -1 > Index)
			{
				Index++;
				Play();
			}
			else if (loopAll)
			{
				Index = 0;
				Play();
			}
			isSkippingToNext = false;
		}

		///<summary>
		///Plays previous song in queue
		///</summary>
		private void PreviousSong()
		{
			if (mediaPlayer == null) return;
			if (mediaPlayer.CurrentPosition > 10000)//if current song is playing longer than 10 seconds
			{
				mediaPlayer.SeekTo(0);
				return;
			}
			isSkippingToPrevious = true;
			IsPaused = false;
			Index--;
			if(Index < 0 && loopAll){
				Index = Queue.Count -1;
			}
#if DEBUG
            MyConsole.WriteLine($"Index in previous song: {Index}");
#endif

            Play();
			isSkippingToPrevious = false;
		}

		private void SeekTo(int millis)
		{
#if DEBUG
            MyConsole.WriteLine("SEEEEEKING");
#endif

            mediaPlayer.SeekTo(millis);
		}

		///<summary>
		///Generates new queue from single song path or directory path and set index to 0
		///</summary>
		// public void GenerateQueue(string source)
		// {
		// 	Index = 0;
  //           if (FileManager.IsDirectory(source))
		// 	{
		// 		GenerateQueue(FileManager.GetSongs(source));
		// 	}
		// 	else
		// 	{
		// 		Queue = new List<string> { source };
  //               Play();
		// 	}
		// }

		///<summary>
		///Generates new queue from list and resets index to 0
		///</summary>
		// public void GenerateQueue(List<string> source, int i = 0)
		// {
		// 	Queue = source;
  //           Index = i;
  //           Shuffle(IsShuffled);
		// 	Play();
		// }

		public void GenerateQueue(Song source, int? ind = null)
		{
			Queue = new List<Song> { source };
			Index = ind ?? 0;
			Play();
		}
		
		public void GenerateQueue(IEnumerable<Song> source, int? ind = null, bool play = true)
		{
			Queue = source.ToList();
			Index = ind ?? 0;
			if (IsShuffled)
			{
				Shuffle(true, ind);
			}
			if (play)
			{
				Play();
			}
		}
		
		public void GenerateQueue(MusicBaseContainer source, int? ind = null)
		{
			Queue = source.Songs;
			Index = ind ?? 0;
			Play();
		}

		///<summary>
		///Clears queue and resets index to 0
		///</summary>
		public void ClearQueue()
		{
			Queue = new List<Song>();
            Index = 0;
        }

		///<summary>
		///Adds single track or entire album/author to the end of queue from <paramref name="addition"/> path
		///</summary>
		public void AddToQueue(Song addition)
		{
			Queue.Add(addition);
            if (IsShuffled)
			{
				originalQueue.Add(addition);
			}
		}

		///<summary>
		///Adds list of songs to the end of queue from <paramref name="addition"/>
		///</summary>
		public void AddToQueue(List<Song> addition)
		{
			Queue.AddRange(addition);
            if (IsShuffled)
			{
				originalQueue.AddRange(addition);
			}
		}

		public void AddToQueue(MusicBaseContainer obj)
		{
			AddToQueue(obj.Songs);
		}

		///<summary>
		///Prepends song or entire album/author to queue
		///</summary>
		public void PlayNext(Song addition)
		{
			Queue.Insert(Index+1, addition);
            if (IsShuffled)
			{
				originalQueue.Insert(Index+1, addition);
			}

		}

		///<summary>
		///Adds song list as first to queue
		///</summary>
		public void PlayNext(List<Song> addition)
		{
			if (IsShuffled)
			{
				//TODO: opravit podla inej logiky nizsie
				addition.AddRange(originalQueue);
				originalQueue = addition;
			}
			if(Index >= Queue.Count)
			{
                AddToQueue(addition);
			}
			else
			{
				List<Song> tmp = Queue.GetRange(0, Index+1);
				tmp.AddRange(addition);
				tmp.AddRange(Queue.Skip(Index + 1));
                Queue = tmp;
            }
        }

		public void PlayNext(MusicBaseContainer addition)
		{
			PlayNext(addition.Songs);
		}

		///<summary>
		///Shuffles or unshuffles queue and updates shuffling for all new queues based on <paramref name="newShuffleState"/> state
		///</summary>
		public void Shuffle(bool newShuffleState, int? indx = null)
		{
            if(Queue.Count == 0) { return; }

            int ind = indx ?? Index;

            shuffling.WaitOne();
			if (newShuffleState)
			{
				originalQueue = Queue.ToList();
				Song tmp = Queue.Pop(ind);
				Index = 0;
                Queue.Shuffle();
				Queue = Queue.Prepend(tmp).ToList();
            }
			else
			{
				if (originalQueue.Count > 0)
				{
					Index = originalQueue.IndexOf(Queue[Index]);
                    Queue = originalQueue;
                    originalQueue = new List<Song>();
				}
			}
			IsShuffled = newShuffleState;
			MainActivity.stateHandler.shuffle = newShuffleState;
			UpdatePlaybackState();
			if (Assets != null) side_player.populate_side_bar(MainActivity.stateHandler.view, Assets);
			shuffling.Set();
		}

		///<summary>
		///Cycles through loop states based on <paramref name="state"/> value
		///</summary>
		public void ToggleLoop(int state)
		{
			state %= 3;
			LoopState = state;
			switch (state)
			{
				case 0:
					loopAll = false;
					loopSingle = false;
					break;
				case 1:
					loopAll = true;
					loopSingle = false;
					break;
				case 2:
					loopAll = false;
					loopSingle = true;
					break;
			}
			//mediaPlayer.Looping = loopSingle;
			MainActivity.stateHandler.loopState = LoopState;
            UpdatePlaybackState();
            if (Assets != null) side_player.populate_side_bar(MainActivity.stateHandler.view, Assets);
#if DEBUG
            MyConsole.WriteLine("TOGGLE LOOP");
#endif
        }

		///<summary>
		///Requests audio focus
		///</summary>
		private bool RequestFocus()
		{
			if (audioManager == null)
			{
				InnitAudioManager();
			}

			if (isFocusGranted) return true;
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
			{
				if (audioFocusRequest == null)
				{
					InnitFocusRequest();
				}

				if (audioManager == null) return false;
				if (audioFocusRequest != null)
				{
					AudioFocusRequest request = audioManager.RequestAudioFocus(audioFocusRequest);
					if (!request.Equals(AudioFocusRequest.Granted))
					{
						// handle any failed requests
#if DEBUG
						MyConsole.WriteLine("No focus");
						MyConsole.WriteLine(request.ToString());
#endif

						return false;
					}
				}

				isFocusGranted = true;
				return true;
			}

			if (audioManager == null) return false;
#pragma warning disable CS0618 // Type or member is obsolete
			AudioFocusRequest request2 = audioManager.RequestAudioFocus(this, Android.Media.Stream.Music, AudioFocus.Gain);
#pragma warning restore CS0618 // Type or member is obsolete
			if (request2 != AudioFocusRequest.Granted)
			{
				// handle any failed requests
#if DEBUG
				MyConsole.WriteLine("No focus");
				MyConsole.WriteLine(request2.ToString());
#endif
				return false;
			}
			isFocusGranted = true;
			return true;

		}

		///<summary>
		///Abandons/Releases audio focus
		///</summary>
		private void AbandonFocus()
		{
			if (audioManager == null)
			{
				InnitAudioManager();
			}

			if (!isFocusGranted) return;
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
			{
				if (audioFocusRequest == null)
				{
					InnitFocusRequest();
				}
				if (audioManager == null) return;
				if (audioFocusRequest == null) return;
				AudioFocusRequest abandon = audioManager.AbandonAudioFocusRequest(audioFocusRequest);
				if (!abandon.Equals(AudioFocus.Gain))
				{
#if DEBUG
					MyConsole.WriteLine("No abandon");
#endif
					// handle any failed requests
				}
				else
				{
					isFocusGranted = false;
				}
			}
			else
			{
				if (audioManager == null) return;
#pragma warning disable CS0618 // Type or member is obsolete
				AudioFocusRequest abandon = audioManager.AbandonAudioFocus(this);
#pragma warning restore CS0618 // Type or member is obsolete
				if (abandon != AudioFocusRequest.Granted)
				{
					// handle any failed requests
#if DEBUG
					MyConsole.WriteLine("No abandon");
#endif
				}
				else
				{
					isFocusGranted = false;
				}
			}
		}

		///<summary>
		///Releases all resources associated with service
		///</summary>
		private void CleanUp()
		{
			if (mediaPlayer != null)
			{
				Stop();
				mediaPlayer.Release();
				mediaPlayer.Dispose();
				mediaPlayer = null;
			}
			if(session != null)
			{
				session.Release();
				session.Dispose();
				session = null;
			}
			if(audioManager != null)
			{
				audioManager.Dispose();
				audioManager = null;
			}
			if(audioFocusRequest != null)
			{
				audioFocusRequest.Dispose();
				audioFocusRequest = null;
			}

			isFocusGranted = false;
			isUsed = false;
			lostFocusDuringPlay = false;
			IsPaused = false;
			isSkippingToNext = false;
			isSkippingToPrevious = false;
			isBuffering = true;
		}

		/// <inheritdoc />
		public override IBinder OnBind(Intent? intent)
		{
			//TODO: publish interface?
			return new MediaServiceBinder(this);
		}

		protected override void Dispose(bool disposing)
		{
			CleanUp();
			base.Dispose(disposing);
		}

		/*public void Dispose()
		{
			throw new NotImplementedException();
		}*/
	}
}