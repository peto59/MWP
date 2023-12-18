using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using AndroidApp = Android.App.Application;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Player
{
	/// <summary>
	/// Media service for playing songs
	/// </summary>
	[Service(ForegroundServiceType = ForegroundService.TypeMediaPlayback, Label = "@string/service_name")]
	//TODO: https://developer.android.com/training/cars/media
	public class MediaService : Service, AudioManager.IOnAudioFocusChangeListener
	{
		private MediaSessionCompat? session;

		private int boundServices = 0;

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


		/// <summary>
		/// Whether <see cref="mediaPlayer"/> is currently playing
		/// </summary>
		public bool IsPlaying => mediaPlayer?.IsPlaying ?? false;
		/// <summary>
		/// Duration of currently playing <see cref="Song"/>
		/// </summary>
		public int Duration => mediaPlayer?.Duration ?? 0;
		/// <summary>
		/// Current position of <see cref="mediaPlayer"/>
		/// </summary>
		public int CurrentPosition => mediaPlayer?.CurrentPosition ?? 0;
		private MediaPlayer? mediaPlayer;
		private AudioManager? audioManager;
		private AudioFocusRequestClass? audioFocusRequest;
		private readonly Local_notification_service notificationService = new Local_notification_service();
		private static MyMediaBroadcastReceiver? _mediaReceiver = new MyMediaBroadcastReceiver();
		private static MyBroadcastReceiver? _receiver = new MyBroadcastReceiver();

		/// <summary>
		/// Queue object taking care of queue
		/// </summary>
		public MyMediaQueue QueueObject => queueObject ??= new MyMediaQueue(Session);
		private MyMediaQueue? queueObject;
		private long actions;
		private bool isFocusGranted;
		private bool lostFocusDuringPlay;
		private bool isPaused;
		
		private bool isSkippingToNext;
		private bool isSkippingToPrevious;
		private bool isBuffering = true;

		/// <inheritdoc />
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
			mediaPlayer.Completion += delegate
			{
				NextSong(false);
			};
			mediaPlayer.BufferingUpdate += (_, e) =>
			{
				isBuffering = e.Percent != 100;
			};
			mediaPlayer.SeekComplete += delegate
			{
                UpdatePlaybackState();
            };
		}

		///<summary>
		///Creates new AudioManager
		///</summary>
		private void InnitAudioManager()
		{
			if (ApplicationContext != null) audioManager = AudioManager.FromContext(ApplicationContext);
		}

		/// <inheritdoc />
		public override void OnCreate()
		{
			base.OnCreate();
			RegisterReceivers();
			InnitPlayer();
			InnitAudioManager();
			InnitSession();
			InnitFocusRequest();
			InnitNotification();
		}

		private void RegisterReceivers()
		{
			IntentFilter intentFilter = new IntentFilter();
			intentFilter.AddAction(MyMediaBroadcastReceiver.PLAY);
			intentFilter.AddAction(MyMediaBroadcastReceiver.PAUSE);
			intentFilter.AddAction(MyMediaBroadcastReceiver.SHUFFLE);
			intentFilter.AddAction(MyMediaBroadcastReceiver.TOGGLE_LOOP);
			intentFilter.AddAction(MyMediaBroadcastReceiver.NEXT_SONG);
			intentFilter.AddAction(MyMediaBroadcastReceiver.PREVIOUS_SONG);
			RegisterReceiver(_mediaReceiver, intentFilter);
			
			RegisterReceiver(_receiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));
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
			session.SetFlags((int)(MediaSessionFlags.HandlesMediaButtons | MediaSessionFlags.HandlesTransportControls));
			session.SetCallback(new MediaSessionCallback(new MediaServiceBinder(this)));
			session.Active = true;
			//session.SetSessionActivity()
		}

		///<summary>
		///Creates new FocusRequest
		///</summary>
		private void InnitFocusRequest()
		{
			audioFocusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
											   .SetAudioAttributes(new AudioAttributes.Builder().SetLegacyStreamType(Stream.Music)?.SetUsage(AudioUsageKind.Media)?.Build()!)
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
				metadataBuilder.PutBitmap(MediaMetadataCompat.MetadataKeyArt, QueueObject.Queue[0].Image);
			}
			catch (Exception)
			{
				if (Application.Context.Assets != null)
				{
					metadataBuilder.PutBitmap(MediaMetadataCompat.MetadataKeyArt, MusicBaseClassStatic.Placeholder);	
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
				int icon = QueueObject.LoopState switch
				{
					LoopState.All => Resource.Drawable.repeat,
					LoopState.Single => Resource.Drawable.repeat_one,
					LoopState.None => Resource.Drawable.no_repeat,
					_ => Resource.Drawable.no_repeat
				};
				stateBuilder.AddCustomAction("loop", "loop", icon);
				stateBuilder.AddCustomAction("shuffle", "shuffle", QueueObject.IsShuffled ? Resource.Drawable.no_shuffle2 : Resource.Drawable.shuffle2);
				session?.SetPlaybackState(stateBuilder.Build());
			}

			if (session?.SessionToken != null) notificationService.song_control_notification(session.SessionToken);
			StartForeground(notificationService.NotificationId, notificationService.Notification, ForegroundService.TypeMediaPlayback);
		}

		/// <inheritdoc />
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
				case AudioFocus.GainTransient:
				case AudioFocus.GainTransientExclusive:
				case AudioFocus.GainTransientMayDuck:
				case AudioFocus.None:
				default:
					//ignored
					break;
			}
		}

		/// <inheritdoc />
		public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
		{
#if DEBUG
			MyConsole.WriteLine("CREATING NEW SESSION");
#endif
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
				actions = PlaybackState.ActionPause | PlaybackState.ActionStop;
			}else{
				actions = PlaybackState.ActionPlay;
			}
			
			if (QueueObject.QueueCount == 0)
			{
				return actions;
			}
			
			actions |= PlaybackState.ActionSeekTo;
			
			if (QueueObject.HasPrevious)
			{
				actions |= PlaybackState.ActionSkipToPrevious;
			}
			if (QueueObject.HasNext)
			{
				actions |= PlaybackState.ActionSkipToNext;
			}
			return actions;
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
				MainActivity.StateHandler.SongProgressCts.Cancel();
				MainActivity.StateHandler.SongProgressCts = new CancellationTokenSource();
				SidePlayer.SetStopButton();
				WidgetServiceHandler.SetPauseButton();
				SidePlayer.StartMovingProgress(MainActivity.StateHandler.SongProgressCts.Token, MainActivity.StateHandler.view);
			}
			else if (isPaused)
			{
				state = PlaybackStateCode.Paused;
				position = mediaPlayer?.CurrentPosition ?? 0;
				SidePlayer.SetPlayButton();
				WidgetServiceHandler.SetPlayButton();
				MainActivity.StateHandler.SongProgressCts.Cancel();
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
				int icon = QueueObject.LoopState switch
				{
					LoopState.All => Resource.Drawable.repeat,
					LoopState.Single => Resource.Drawable.repeat_one,
					LoopState.None => Resource.Drawable.no_repeat,
					_ => Resource.Drawable.no_repeat
				};
				stateBuilder.AddCustomAction("loop", "loop", icon);
				stateBuilder.AddCustomAction("shuffle", "shuffle", QueueObject.IsShuffled ? Resource.Drawable.no_shuffle2 : Resource.Drawable.shuffle2);
				stateBuilder.SetActiveQueueItemId(QueueObject.Index);
				session?.SetPlaybackState(stateBuilder.Build());
			}

			if (Assets != null) SidePlayer.populate_side_bar(MainActivity.StateHandler.view, Assets);
			WidgetServiceHandler.UpdateWidgetViews();
			notificationService.Notify();
		}

		private void UpdateMetadata()
		{
			if (mediaPlayer == null || QueueObject.QueueCount <= 0) return;
			if (session == null)
			{
				InnitSession();
			}
			MediaMetadataCompat.Builder metadataBuilder = new MediaMetadataCompat.Builder();
			Song song = QueueObject.Current;
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
				song.Artist.Title);
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
		public void Play(bool reset = false)
		{
			if (QueueObject.QueueCount == 0)
			{
				try
				{
					GenerateQueue(StateHandler.Songs, null, false);
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
				StartForeground(notificationService.NotificationId, notificationService.Notification, ForegroundService.TypeMediaPlayback);
			}
			if (mediaPlayer == null)
			{
				InnitPlayer();
			}
			if (!isPaused || reset)
			{
				mediaPlayer!.Reset();
#if DEBUG
                MyConsole.WriteLine($"SERVICE INDEX {QueueObject.Index}");
                MyConsole.WriteLine($"SERVICE QUEUE {QueueObject.QueueCount}");
#endif
				if (!System.IO.File.Exists(QueueObject.Current.Path))
				{
					return;
				}
				mediaPlayer.SetDataSource(QueueObject.Current.Path);
				mediaPlayer.Prepare();
			}
			mediaPlayer!.Start();

			if (!isPaused || reset)
			{
				UpdateMetadata();
			}
			else
			{
				UpdatePlaybackState();
			}
			isPaused = false;

			isSkippingToNext = false;
			isSkippingToPrevious = false;
		}
		
		///<summary>
		///Pauses playback
		///</summary>
		public void Pause()
		{
			mediaPlayer?.Pause();
			isPaused = true;
			UpdatePlaybackState();
		}

		///<summary>
		///Stops playing and abandons focus
		///</summary>
		public void Stop()
		{
			mediaPlayer?.Stop();
			AbandonFocus();
			//updateMetadata();
			notificationService.destroy_song_control();
		}

		///<summary>
		///Plays next song in <see cref="MyMediaQueue.Queue"/>
		///</summary>
		public void NextSong(bool user = true)
		{
			if (mediaPlayer == null) return;
			isSkippingToNext = true;
			isPaused = false;
			if (!user && QueueObject.LoopState == LoopState.Single)
			{
				Play();
			}
			else if (QueueObject.IncrementIndex())
			{
				Play();
			}
			isSkippingToNext = false;
		}

		///<summary>
		///Plays previous song in <see cref="MyMediaQueue.Queue"/>
		///</summary>
		public void PreviousSong()
		{
			if (mediaPlayer == null) return;
			if (mediaPlayer.CurrentPosition > 10000)//if current song is playing longer than 10 seconds
			{
				mediaPlayer.SeekTo(0);
				return;
			}
			isSkippingToPrevious = true;
			isPaused = false;
			QueueObject.DecrementIndex();
            Play();
			isSkippingToPrevious = false;
		}

		/// <summary>
		/// Seeks to <paramref name="millis"/>
		/// </summary>
		/// <param name="millis">Position to seek to in milliseconds</param>
		public void SeekTo(int millis)
		{
            mediaPlayer?.SeekTo(millis);
		}

		/// <summary>
		/// Generates new <see cref="MyMediaQueue.Queue"/> in <see cref="queueObject"/>
		/// </summary>
		/// <param name="source">Content of new <see cref="MyMediaQueue.Queue"/></param>
		/// <param name="play">Whether to start playback</param>
		public void GenerateQueue(Song source, bool play = true)
		{
			GenerateQueue(new List<Song> { source }, null, play);
		}

		/// <summary>
		/// Generates new <see cref="MyMediaQueue.Queue"/> in <see cref="queueObject"/>
		/// </summary>
		/// <param name="source">Content of new <see cref="MyMediaQueue.Queue"/></param>
		/// <param name="id">id of songs object to be played for <see cref="MyMediaQueue.Index"/> lookup purposes</param>
		/// <param name="play">Whether to start playback</param>
		public void GenerateQueue(IEnumerable<Song> source, Guid? id = null, bool play = true)
		{
			QueueObject.GenerateQueue(source, id);
			if (play)
			{
				Play(true);
			}
		}
		
		/// <summary>
		/// Generates new <see cref="MyMediaQueue.Queue"/> in <see cref="queueObject"/>
		/// </summary>
		/// <param name="source">Content of new <see cref="MyMediaQueue.Queue"/></param>
		/// <param name="id">id of songs object to be played for <see cref="MyMediaQueue.Index"/> lookup purposes</param>
		/// <param name="play">Whether to start playback</param>
		public void GenerateQueue(MusicBaseContainer source, Guid? id = null, bool play = true)
		{
			GenerateQueue(source.Songs, id, play);
		}
		
		/// <summary>
		/// Generates new <see cref="MyMediaQueue.Queue"/> in <see cref="queueObject"/>
		/// </summary>
		/// <param name="source">Content of new <see cref="MyMediaQueue.Queue"/></param>
		/// <param name="id">id of songs object to be played for <see cref="MyMediaQueue.Index"/> lookup purposes</param>
		/// <param name="play">Whether to start playback</param>
		public void GenerateQueue(IEnumerable<MusicBaseContainer> source, Guid? id = null, bool play = true)
		{
			IEnumerable<Song> songs = source.SelectMany(s => s.Songs);
			GenerateQueue(songs, id, play);
		}

		///<summary>
		///Adds single <see cref="Song"/> to the end of <see cref="MyMediaQueue.Queue"/> from <paramref name="addition"/>
		///</summary>
		public void AddToQueue(Song addition)
		{
			AddToQueue(new List<Song>{addition});
		}

		///<summary>
		///Adds <see cref="IEnumerable{T}"/> of <see cref="Song"/> to the end of <see cref="MyMediaQueue.Queue"/> from <paramref name="addition"/>
		///</summary>
		public void AddToQueue(IEnumerable<Song> addition)
		{
			QueueObject.AppendToQueue(addition);
		}

		///<summary>
		///Adds <see cref="IEnumerable{T}"/> of <see cref="Song"/> to the end of <see cref="MyMediaQueue.Queue"/> from <paramref name="obj"/>
		///</summary>
		public void AddToQueue(MusicBaseContainer obj)
		{
			AddToQueue(obj.Songs);
		}

		///<summary>
		///Prepends <see cref="Song"/> to <see cref="MyMediaQueue.Queue"/>
		///</summary>
		public void PlayNext(Song addition)
		{
			PlayNext(new List<Song>{addition});

		}

		///<summary>
		///Adds <see cref="IEnumerable{T}"/> of <see cref="Song"/> to start of <see cref="MyMediaQueue.Queue"/>
		///</summary>
		public void PlayNext(List<Song> addition)
		{
			QueueObject.PrependToQueue(addition);
        }

		/// <summary>
		/// Adds <see cref="IEnumerable{T}"/> of <see cref="Song"/> to start of <see cref="MyMediaQueue.Queue"/> from <paramref name="addition"/>
		/// </summary>
		/// <param name="addition">Content to be added</param>
		public void PlayNext(MusicBaseContainer addition)
		{
			PlayNext(addition.Songs);
		}

		///<summary>
		///Shuffles or unshuffles <see cref="MyMediaQueue.Queue"/> and updates shuffling for all new <see cref="MyMediaQueue.Queue"/>s based on <paramref name="newShuffleState"/> state
		///</summary>
		public void Shuffle(bool newShuffleState)
		{
			QueueObject.IsShuffled = newShuffleState;
			UpdatePlaybackState();
			if (Assets != null) SidePlayer.populate_side_bar(MainActivity.StateHandler.view, Assets);
		}

		///<summary>
		///Cycles through loop states based on <paramref name="state"/> value
		///</summary>
		public void ToggleLoop(int state)
		{
			QueueObject.ToggleLoop(state);
            UpdatePlaybackState();
            if (Assets != null) SidePlayer.populate_side_bar(MainActivity.StateHandler.view, Assets);
#if DEBUG
            MyConsole.WriteLine("TOGGLE LOOP");
#endif
        }

		///<summary>
		///Cycles through loop states based on <paramref name="loopState"/> value
		///</summary>
		public void ToggleLoop(LoopState loopState)
		{
			ToggleLoop((int)loopState);
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
			AudioFocusRequest request2 = audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
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

			UnregisterReceivers();

			isFocusGranted = false;
			lostFocusDuringPlay = false;
			isPaused = false;
			isSkippingToNext = false;
			isSkippingToPrevious = false;
			isBuffering = true;
		}

		private void UnregisterReceivers()
		{
			UnregisterReceiver(_mediaReceiver);
			UnregisterReceiver(_receiver);
		}

		/// <inheritdoc />
		public override IBinder OnBind(Intent? intent)
		{
#if DEBUG
			MyConsole.WriteLine("I'm being bound");
#endif
			boundServices++;
			return new MediaServiceBinder(this);
		}

		/// <inheritdoc />
		public override bool OnUnbind(Intent? intent)
		{
			boundServices--;
			return false;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (boundServices > 0)
			{
				//TODO: rework
#if DEBUG
				MyConsole.WriteLine($"Not disposing still, have {boundServices} binds");
#endif
				return;
			}
			CleanUp();
		}

		/*public void Dispose()
		{
			throw new NotImplementedException();
		}*/
	}
}