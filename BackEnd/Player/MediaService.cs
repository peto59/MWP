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
using MWP.BackEnd.Player;
using MWP.Helpers;
#if DEBUG
#endif

namespace MWP
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
		public readonly MyMediaQueue QueueObject = new MyMediaQueue();
		public long Actions { get; private set; }
		private bool isFocusGranted;
		private bool isUsed;
		private bool lostFocusDuringPlay;
		public bool IsPaused { get; private set; }
		
		private bool isSkippingToNext;
		private bool isSkippingToPrevious;
		private bool isBuffering = true;
		


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
				metadataBuilder.PutBitmap(MediaMetadataCompat.MetadataKeyArt, QueueObject.Queue[0].Image);
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
				int icon = QueueObject.LoopState switch
				{
					Enums.LoopState.All => Resource.Drawable.repeat,
					Enums.LoopState.Single => Resource.Drawable.repeat_one,
					Enums.LoopState.None => Resource.Drawable.no_repeat,
					_ => Resource.Drawable.no_repeat
				};
				stateBuilder.AddCustomAction("loop", "loop", icon);
				stateBuilder.AddCustomAction("shuffle", "shuffle", QueueObject.IsShuffled ? Resource.Drawable.no_shuffle2 : Resource.Drawable.shuffle2);
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
			
			if (QueueObject.QueueCount == 0)
			{
				return Actions;
			}
			
			Actions |= PlaybackState.ActionSeekTo;
			
			if (QueueObject.HasPrevious)
			{
				Actions |= PlaybackState.ActionSkipToPrevious;
			}
			if (QueueObject.HasNext)
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
				int icon = QueueObject.LoopState switch
				{
					Enums.LoopState.All => Resource.Drawable.repeat,
					Enums.LoopState.Single => Resource.Drawable.repeat_one,
					Enums.LoopState.None => Resource.Drawable.no_repeat,
					_ => Resource.Drawable.no_repeat
				};
				stateBuilder.AddCustomAction("loop", "loop", icon);
				stateBuilder.AddCustomAction("shuffle", "shuffle", QueueObject.IsShuffled ? Resource.Drawable.no_shuffle2 : Resource.Drawable.shuffle2);
				session?.SetPlaybackState(stateBuilder.Build());
			}

			if (Assets != null) side_player.populate_side_bar(MainActivity.stateHandler.view, Assets);
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
			if (QueueObject.QueueCount == 0)
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
				StartForeground(notificationService.NotificationId, notificationService.Notification, ForegroundService.TypeMediaPlayback);
				//android:foregroundServiceType="mediaPlayback"
				//StartForeground(notificationService.NotificationId, notificationService.Notification);
			}
			if (mediaPlayer == null)
			{
				InnitPlayer();
			}
			if (!IsPaused)
			{
				mediaPlayer!.Reset();
#if DEBUG
                MyConsole.WriteLine($"SERVICE INDEX {QueueObject.Index}");
                MyConsole.WriteLine($"SERVICE QUEUE {QueueObject.QueueCount}");
#endif


                // if (!File.Exists(Queue[Index].Path))
                // {
                // 	NextSong();
                // 	return;
                // }
                mediaPlayer.SetDataSource(QueueObject.Current.Path);
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
			isSkippingToNext = true;
			IsPaused = false;
			if (QueueObject.IncrementIndex())
			{
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
			QueueObject.DecrementIndex();
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

		public void GenerateQueue(Song source, int ind = 0, bool play = true)
		{
			GenerateQueue(new List<Song> { source }, ind, play);
		}
		
		public void GenerateQueue(IEnumerable<Song> source, int ind = 0, bool play = true)
		{
			QueueObject.GenerateQueue(source, ind);
			if (play)
			{
				//todo: reset
				Play();
			}
		}
		
		public void GenerateQueue(MusicBaseContainer source, int ind = 0, bool play = true)
		{
			GenerateQueue(source.Songs, ind, play);
		}

		///<summary>
		///Adds single track or entire album/author to the end of queue from <paramref name="addition"/> path
		///</summary>
		public void AddToQueue(Song addition)
		{
			AddToQueue(new List<Song>{addition});
		}

		///<summary>
		///Adds list of songs to the end of queue from <paramref name="addition"/>
		///</summary>
		public void AddToQueue(List<Song> addition)
		{
			QueueObject.AppendToQueue(addition);
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
			PlayNext(new List<Song>{addition});

		}

		///<summary>
		///Adds song list as first to queue
		///</summary>
		public void PlayNext(List<Song> addition)
		{
			QueueObject.PrependToQueue(addition);
        }

		public void PlayNext(MusicBaseContainer addition)
		{
			PlayNext(addition.Songs);
		}

		///<summary>
		///Shuffles or unshuffles queue and updates shuffling for all new queues based on <paramref name="newShuffleState"/> state
		///</summary>
		public void Shuffle(bool newShuffleState)
		{
			QueueObject.IsShuffled = newShuffleState;
			UpdatePlaybackState();
			if (Assets != null) side_player.populate_side_bar(MainActivity.stateHandler.view, Assets);
		}

		///<summary>
		///Cycles through loop states based on <paramref name="state"/> value
		///</summary>
		public void ToggleLoop(int state)
		{
			QueueObject.ToggleLoop(state);
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