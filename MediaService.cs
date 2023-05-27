using Android.App;
using AndroidApp = Android.App.Application;
using Android.Content;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
//using Java.Lang;
using Java.Security;
using System.Runtime.Remoting.Contexts;
using TagLib.Flac;
using Java.Util.Jar;
using System.IO;
using Android.Content.PM;
using Android.Graphics;
using Android.Icu.Text;
using Android.Support.V4.Media.Session;
using Android.Support.V4.Media;

namespace Ass_Pain
{
	[Service(ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
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
		///Generates new queue based on Intent extra params and sets index to Intent extra params or resets index to 0
		///</summary>
		public const string ActionGenerateQueue = "ActionGenerateQueue";

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
		///Adds song or list of songs from either list or directory path based on Intent extra params to the end of the queue
		///</summary>
		public const string ActionAddToQueue = "ActionAddToQueue";

		///<summary>
		///Adds song or list of songs from either list or directory path based on Intent extra params to the start of the queue
		///</summary>
		public const string ActionPlayNext = "ActionPlayNext";

		///<summary>
		///Clears queue and resets index to 0
		///</summary>
		public const string ActionClearQueue = "ActionClearQueue";
		///<summary>
		///Moves playback of current song intent extra int time in milliseconds
		///</summary>
		public const string ActionSeekTo = "ActionSeekTo";

		private MediaSessionCompat session = null ;
		private MediaPlayer mediaPlayer = null;
		private AudioManager audioManager = null;
		private AudioFocusRequestClass audioFocusRequest = null;
		private Local_notification_service notificationService = new Local_notification_service();
		private bool isFocusGranted = false;
		private bool isUsed = false;
		private bool lostFocusDuringPlay = false;
		private bool isPaused = false;
		private bool shuffle = false;
		private bool loopAll = false;
		private bool loopSingle = false;
		private bool isSkippingToNext = false;
		private bool isSkippingToPrevious = false;
		private bool isBuffering = true;
		private bool isShuffling = false;
		private int loopState = 0;
        private int i = 0;
		private int Index
		{
			get { return i; }
			set { i = value.KeepPositive(); MainActivity.stateHandler.setIndex(ref i); }
		}
		private List<string> q = new List<string>();
        private List<string> Queue
		{
			get { return q; }
			set { q = value; MainActivity.stateHandler.setQueue(ref q); }
		}
        private List<string> orignalQueue = new List<string>();

		public override void OnCreate()
		{
			base.OnCreate();
			InnitPlayer();
			InnitAudioManager();
			InnitSession();
			InnitFocusRequest();
			InnitNotification();
			Console.WriteLine("CREATING NEW SESSION");
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
			mediaPlayer.BufferingUpdate += new EventHandler<MediaPlayer.BufferingUpdateEventArgs>((object sender, MediaPlayer.BufferingUpdateEventArgs e) =>
			{
				if (e.Percent == 100)
				{
					isBuffering = false;
				}
				else
				{
					isBuffering = true;
				}
			});
			mediaPlayer.SeekComplete += delegate
			{
                UpdatePlaybackState();
            };
			MainActivity.stateHandler.setMediaPlayer(ref mediaPlayer);
        }

		///<summary>
		///Creates new AudioManager
		///</summary>
		private void InnitAudioManager()
		{
			audioManager = AudioManager.FromContext(ApplicationContext);
		}

		///<summary>
		///Creates new MediaSession
		///</summary>
		private void InnitSession()
		{
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
											   .SetAudioAttributes(new AudioAttributes.Builder().SetLegacyStreamType(Android.Media.Stream.Music).SetUsage(AudioUsageKind.Media).Build())
											   .SetOnAudioFocusChangeListener(this)
											   .Build();
		}

		private void InnitNotification()
		{
			notificationService.song_control_notification(session.SessionToken);
			StartForeground(notificationService.NotificationId, notificationService.Notification, ForegroundService.TypeMediaPlayback);
			//android:foregroundServiceType="mediaPlayback"
			//StartForeground(notificationService.NotificationId, notificationService.Notification);
		}

		public void OnAudioFocusChange([GeneratedEnum] AudioFocus focusChange)
		{
			switch (focusChange)
			{
				case AudioFocus.Gain:
					mediaPlayer.SetVolume(1.0f, 1.0f);
					if (lostFocusDuringPlay)
					{
						mediaPlayer.Start();
						lostFocusDuringPlay = false;
					}
					break;
				case AudioFocus.Loss:
					//CleanUp();
                    Pause();
					isFocusGranted = false;
                    break;
				case AudioFocus.LossTransient:
					if (mediaPlayer != null)
					{
                        if (mediaPlayer.IsPlaying)
                        {
                            lostFocusDuringPlay = true;
                        }
                    }
					Pause();
					break;
				case AudioFocus.LossTransientCanDuck:
					mediaPlayer.SetVolume(0.25f, 0.25f);
					break;
			}
		}

		public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
		{
			switch (intent.Action)
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
				case ActionGenerateQueue:
					string source = intent.GetStringExtra("source");
					if(source != null)
					{
						GenerateQueue(source);
					}
					else
					{
						List<string> sourceList = intent.GetStringArrayExtra("sourceList").ToList();
						if(sourceList == null)
						{
							throw new ArgumentException("You need to specify either string or list source");
						}
						int i = intent.GetIntExtra("i", 0);
						GenerateQueue(sourceList, i);
					}
					break;
				case ActionShuffle:
					Shuffle(intent.GetBooleanExtra("shuffle", false));
					break;
				case ActionToggleLoop:
					ToggleLoop(intent.GetIntExtra("loopState", 0));
                    Console.WriteLine("TOGLELOOP SWITCH");
                    break;
				case ActionTogglePlay:
					if (mediaPlayer.IsPlaying)
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
				case ActionAddToQueue:
					string addition = intent.GetStringExtra("addition");
					if (addition != null)
					{
						AddToQueue(addition);
					}
					else
					{
						List<string> additionList = intent.GetStringArrayExtra("additionList").ToList();
						if (additionList == null)
						{
							throw new ArgumentException("You need to specify either string or list addition");
						}
						AddToQueue(additionList);
					}
					break;
				case ActionPlayNext:
					string prepend = intent.GetStringExtra("prepend");
					if (prepend != null)
					{
						PlayNext(prepend);
					}
					else
					{
						List<string> prependList = intent.GetStringArrayExtra("prependList").ToList();
						if (prependList == null)
						{
							throw new ArgumentException("You need to specify either string or list prepend");
						}
						PlayNext(prependList);
					}
					break;
				case ActionSeekTo:
					SeekTo(intent.GetIntExtra("millis", 0));
					break;
				case ActionClearQueue:
					ClearQueue();
					break;
			}
            intent.Dispose();
			return StartCommandResult.Sticky;
		}
		private long GetAvailableActions()
		{
			long actions = PlaybackState.ActionPlay | PlaybackState.ActionSeekTo ;
			if (Queue == null || Queue.Count == 0)
			{
				return actions;
			}
			if (mediaPlayer.IsPlaying)
			{
				actions |= PlaybackState.ActionPause | PlaybackState.ActionStop;
			}
			if (Index > 0)
			{
				actions |= PlaybackState.ActionSkipToPrevious;
			}
			if (Queue.Count > Index)
			{
				actions |= PlaybackState.ActionSkipToNext;
			}
			return actions;
		}

		private void UpdatePlaybackState()
		{
			long position = PlaybackState.PlaybackPositionUnknown;
			PlaybackStateCode state;
			if (mediaPlayer != null && mediaPlayer.IsPlaying)
			{
				Console.WriteLine("A");
				position = mediaPlayer.CurrentPosition;
				state = PlaybackStateCode.Playing;
				side_player.SetStopButton(MainActivity.stateHandler.view);
				MainActivity.stateHandler.cts.Cancel();
				MainActivity.stateHandler.cts = new CancellationTokenSource();
				side_player.StartMovingProgress(MainActivity.stateHandler.cts.Token, MainActivity.stateHandler.view);
			}
			else if (isPaused)
			{
                Console.WriteLine("B");
                state = PlaybackStateCode.Paused;
                position = mediaPlayer.CurrentPosition;
                side_player.SetPlayButton(MainActivity.stateHandler.view);
				MainActivity.stateHandler.cts.Cancel();
			}
			else if (isSkippingToNext)
			{
                Console.WriteLine("C");
                state = PlaybackStateCode.SkippingToNext;
			}
			else if (isSkippingToPrevious)
			{
                Console.WriteLine("D");
                state = PlaybackStateCode.SkippingToPrevious;
			}
			else if (isBuffering)
			{
                Console.WriteLine("E");
                state = PlaybackStateCode.Buffering;
			}
			else
			{
                Console.WriteLine("F");
                state = PlaybackStateCode.None;
			}

			PlaybackStateCompat.Builder stateBuilder = new PlaybackStateCompat.Builder()
					.SetActions(GetAvailableActions());
			stateBuilder.SetState((int)state, position, 1.0f);
			session.SetPlaybackState(stateBuilder.Build());
			side_player.populate_side_bar(MainActivity.stateHandler.view);
			notificationService.Notify();
		}

		private void UpdateMetadata()
		{
			if (mediaPlayer != null && Queue.Count > 0)
			{
				MediaMetadataCompat.Builder metadataBuilder = new MediaMetadataCompat.Builder();
                using var tfile = TagLib.File.Create(Queue[Index]);
                using MemoryStream ms = new MemoryStream(tfile.Tag.Pictures.FirstOrDefault().Data.Data);
                // To provide most control over how an item is displayed set the
                // display fields in the metadata
                metadataBuilder.PutString(MediaMetadata.MetadataKeyDisplayTitle,
                        tfile.Tag.Title);
                // And at minimum the title and artist for legacy support
                metadataBuilder.PutString(MediaMetadata.MetadataKeyTitle,
                        tfile.Tag.Title);
                metadataBuilder.PutString(MediaMetadata.MetadataKeyDisplaySubtitle,
                        tfile.Tag.Album ?? tfile.Tag.Performers.FirstOrDefault());
                // A small bitmap for the artwork is also recommended
                metadataBuilder.PutBitmap(MediaMetadata.MetadataKeyArt,
                        BitmapFactory.DecodeStream(ms));
                metadataBuilder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt,
                        BitmapFactory.DecodeStream(ms));
                metadataBuilder.PutString(MediaMetadata.MetadataKeyAlbum,
                        tfile.Tag.Album);
                metadataBuilder.PutString(MediaMetadata.MetadataKeyAlbumArtist,
                        tfile.Tag.Performers.FirstOrDefault());
                //Possible error in implementation
                metadataBuilder.PutLong(MediaMetadata.MetadataKeyDuration,
                        mediaPlayer.Duration);
                // Add any other fields you have for your data as well
                session.SetMetadata(metadataBuilder.Build());

                UpdatePlaybackState();
                /*tfile.Dispose();
                ms.Dispose();*/
            }
		}



		///<summary>
		///Requests focus and starts playing new song or resumes playback if playback was paused
		///</summary>
		public void Play()
		{
			if (Queue.Count == 0)
			{
				GenerateQueue(FileManager.GetSongs());
			}
			if (!RequestFocus())
			{
				return;
			}
			if (session == null)
			{
				InnitSession();
			}
			if (!session.Active)
			{
				session.Active = true;
			}
			if (!notificationService.IsCreated)
			{
				notificationService.song_control_notification(session.SessionToken);
				//StartForeground(notificationService.NotificationId, notificationService.Notification, ForegroundService.TypeMediaPlayback);
				//android:foregroundServiceType="mediaPlayback"
				StartForeground(notificationService.NotificationId, notificationService.Notification);
			}
			if (mediaPlayer == null)
			{
				InnitPlayer();
			}
			if (!isPaused)
			{
				mediaPlayer.Reset();
				Console.WriteLine($"SERVICE INDEX {Index}");
				Console.WriteLine($"SERVICE QUEUE {Queue.Count}");
				
				mediaPlayer.SetDataSource(Queue[Index]);
				mediaPlayer.Prepare();
			}
			mediaPlayer.Start();
		
			if (isPaused)
			{
				isPaused = false;
				UpdatePlaybackState();
			}
			else
			{
				UpdateMetadata();
			}
			isSkippingToNext = false;
			isSkippingToPrevious = false;
		}
		
		///<summary>
		///Pauses playback
		///</summary>
		public void Pause()
		{
			mediaPlayer.Pause();
			isPaused = true;
			UpdatePlaybackState();
		}

		///<summary>
		///Stops playing and abandons focus
		///</summary>
		private void Stop()
		{
			mediaPlayer.Stop();
			AbandonFocus();
			//updateMetadata();
			notificationService.destroy_song_control();
		}

		///<summary>
		///Plays next song in queue
		///</summary>
		public void NextSong()
		{
			if(mediaPlayer != null)
			{
				if (loopSingle)
				{
					Play();
					return;
				}
				isSkippingToNext = true;
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
		}

		///<summary>
		///Plays previous song in queue
		///</summary>
		private void PreviousSong(object sender = null, EventArgs e = null)
		{
			if (mediaPlayer != null)
			{
				if (mediaPlayer.CurrentPosition > 10000)//if current song is playing longer than 10 seconds
				{
					mediaPlayer.SeekTo(0);
					return;
				}
				isSkippingToPrevious = true;
				Index--;
				Index = Index.KeepPositive();
				Console.WriteLine($"Index in previous song: {Index}");
                Play();
				isSkippingToPrevious = false;
			}
		}

		private void SeekTo(int millis)
		{
			Console.WriteLine("SEEEEEKING");
			mediaPlayer.SeekTo(millis);
		}

		///<summary>
		///Generates new queue from single song path or directory path and set index to 0
		///</summary>
		public void GenerateQueue(string source)
		{
			Index = 0;
            if (FileManager.IsDirectory(source))
			{
				GenerateQueue(FileManager.GetSongs(source));
			}
			else
			{
				Queue = new List<string> { source };
                Play();
			}
		}

		///<summary>
		///Generates new queue from list and resets index to 0
		///</summary>
		public void GenerateQueue(List<string> source, int i = 0)
		{
			Queue = source;
            Index = i;
            Shuffle(shuffle);
			Play();
		}

		///<summary>
		///Clears queue and resets index to 0
		///</summary>
		public void ClearQueue()
		{
			Queue = new List<string>();
            Index = 0;
        }

		///<summary>
		///Adds single track or entire album/author to the end of queue from <paramref name="addition"/> path
		///</summary>
		public void AddToQueue(string addition)
		{
			if (FileManager.IsDirectory(addition))
			{
				AddToQueue(FileManager.GetSongs(addition));
			}
			else
			{
				Queue.Add(addition);
                if (shuffle)
				{
					orignalQueue.Add(addition);
				}
			}
		}

		///<summary>
		///Adds list of songs to the end of queue from <paramref name="addition"/>
		///</summary>
		public void AddToQueue(List<string> addition)
		{
			Queue.AddRange(addition);
            if (shuffle)
			{
				orignalQueue.AddRange(addition);
			}
		}

		///<summary>
		///Prepends song or entire album/author to queue
		///</summary>
		public void PlayNext(string addition)
		{
			if (FileManager.IsDirectory(addition))
			{
				PlayNext(FileManager.GetSongs(addition));
			}
			else
			{
				Queue.Insert(Index+1, addition);
                if (shuffle)
				{
					orignalQueue.Insert(Index+1, addition);
				}
			}

		}

		///<summary>
		///Adds song list as first to queue
		///</summary>
		public void PlayNext(List<string> addition)
		{
			if (shuffle)
			{
				//opravit podla inej logiky nizsie
				List<string> additionTmp = addition;
				additionTmp.AddRange(orignalQueue);
				orignalQueue = additionTmp;
			}
			if(Index >= Queue.Count)
			{
                AddToQueue(addition);
			}
			else
			{
				List<string> tmp = Queue.GetRange(0, Index+1);
				tmp.AddRange(addition);
				tmp.AddRange(Queue.Skip(Index + 1));
                Queue = tmp;
            }
        }

		///<summary>
		///Shuffles or unshuffles queue and updates shuffling for all new queues based on <paramref name="newShuffleState"/> state
		///</summary>
		public void Shuffle(bool newShuffleState)
		{
            if(Queue.Count == 0) { return; }
            while (isShuffling)
			{
				System.Threading.Thread.Sleep(5);
			}
			isShuffling = true;
			if (newShuffleState)
			{
				orignalQueue = Queue;
				string tmp = Queue.Pop(Index);
				Index = 0;
                Queue.Shuffle();
				Queue = Queue.Prepend(tmp).ToList();
            }
			else
			{
				if (orignalQueue.Count > 0)
				{
					Index = orignalQueue.IndexOf(Queue[Index]);
                    Queue = orignalQueue;
                    orignalQueue = new List<string>();
				}
			}
			shuffle = newShuffleState;
			MainActivity.stateHandler.shuffle = newShuffleState;
			if (notificationService.IsCreated)
			{
				notificationService.Notify();
			}
            side_player.populate_side_bar(MainActivity.stateHandler.view);
			isShuffling = false;
        }

		///<summary>
		///Cycles through loop states based on <paramref name="state"/> value
		///</summary>
		public void ToggleLoop(int state)
		{
			loopState = state;
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
			MainActivity.stateHandler.loopSingle = loopSingle;
			MainActivity.stateHandler.loopAll = loopAll;
			MainActivity.stateHandler.loopState = loopState;
            if (notificationService.IsCreated)
            {
                notificationService.Notify();
            }
            side_player.populate_side_bar(MainActivity.stateHandler.view);
            Console.WriteLine("TOGLELOOP");
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
			if (!isFocusGranted)
			{
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				{
					if (audioFocusRequest == null)
					{
						InnitFocusRequest();
					}
					var request = audioManager.RequestAudioFocus(audioFocusRequest);
					if (!request.Equals(AudioFocusRequest.Granted))
					{
						// handle any failed requests
						Console.WriteLine("No focus");
						Console.WriteLine(request);
						return false;
					}
					else
					{
						isFocusGranted = true;
                        return true;
                    }
				}
				else
				{
					var request = audioManager.RequestAudioFocus(this, Android.Media.Stream.Music, AudioFocus.Gain);
					if (request != AudioFocusRequest.Granted)
					{
                        // handle any failed requests
                        Console.WriteLine("No focus");
                        Console.WriteLine(request);
                        return false;
                    }
					else
					{
						isFocusGranted = true;
						return true;
					}
				}
			}
			else
			{
                return true;
            }
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
			if (isFocusGranted)
			{
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				{
					if (audioFocusRequest == null)
					{
						InnitFocusRequest();
					}
					var abandon = audioManager.AbandonAudioFocusRequest(audioFocusRequest);
					if (!abandon.Equals(AudioFocus.Gain))
					{
					Console.WriteLine("No abandon");
						// handle any failed requests
					}
					else
					{
						isFocusGranted = false;
					}
				}
				else
				{
					var abandon = audioManager.AbandonAudioFocus(this);
					if (abandon != AudioFocusRequest.Granted)
					{
						// handle any failed requests
						Console.WriteLine("No abandon");
					}
					else
					{
						isFocusGranted = false;
					}
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
			isPaused = false;
			isSkippingToNext = false;
			isSkippingToPrevious = false;
			isBuffering = true;
		}

		public override IBinder OnBind(Intent intent)
		{
			Console.WriteLine("CONECTED");
			return new MediaServiceBinder(this);
		}
	}
}