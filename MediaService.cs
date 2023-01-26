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
using Java.Lang;
using Java.Security;
using System.Runtime.Remoting.Contexts;
using static Android.Drm.DrmStore;
using TagLib.Flac;
using Java.Util.Jar;
using System.IO;
using Android.Graphics;
using Android.Icu.Text;

namespace Ass_Pain
{
	[Service]
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

        private MediaSession session = null ;
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
        private bool isNotificationCreated = false;
		private int loopState = 0;
        private int index = 0;
		private List<string> queue = new List<string>();
		private List<string> orignalQueue = new List<string>();

		public override void OnCreate()
		{
			base.OnCreate();
			InnitPlayer();
			InnitAudioManager();
			InnitSession();
			InnitFocusRequest();
            MainActivity.stateHandler.setQueue(ref queue);
			MainActivity.stateHandler.setIndex(ref index);
			Console.WriteLine("CREATING NEW SESSION");
        }
		public override void OnDestroy()
		{
			base.OnDestroy();
			CleanUp();
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
			session = new MediaSession(AndroidApp.Context, "MusicService");
            session.SetCallback(new MediaSessionCallback());
            session.SetFlags(MediaSessionFlags.HandlesMediaButtons | MediaSessionFlags.HandlesTransportControls);
            //session.SetMediaButtonBroadcastReceiver()
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
					CleanUp();
					isFocusGranted = false;
					break;
				case AudioFocus.LossTransient:
					Pause();
					lostFocusDuringPlay = true;
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
            if (queue == null || queue.Count == 0)
            {
                return actions;
            }
            if (mediaPlayer.IsPlaying)
            {
                actions |= PlaybackState.ActionPause | PlaybackState.ActionStop;
            }
            if (index > 0)
            {
                actions |= PlaybackState.ActionSkipToPrevious;
            }
            if (index < queue.Count - 1)
            {
                actions |= PlaybackState.ActionSkipToNext;
            }
            return actions;
        }

        private void updatePlaybackState()
        {
            long position = PlaybackState.PlaybackPositionUnknown;
            PlaybackStateCode state;
            if (mediaPlayer != null && mediaPlayer.IsPlaying)
            {
                position = mediaPlayer.CurrentPosition;
                state = PlaybackStateCode.Playing;
                side_player.SetStopButton(MainActivity.stateHandler.view);
                MainActivity.stateHandler.cts.Cancel();
                MainActivity.stateHandler.cts = new CancellationTokenSource();
                side_player.StartMovingProgress(MainActivity.stateHandler.cts.Token, MainActivity.stateHandler.view);
            }
            else if (isPaused)
            {
                state = PlaybackStateCode.Paused;
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

            PlaybackState.Builder stateBuilder = new PlaybackState.Builder()
                    .SetActions(GetAvailableActions());
            stateBuilder.SetState(state, position, 1.0f);
            session.SetPlaybackState(stateBuilder.Build());
        }

        private void updateMetadata()
        {
            if (mediaPlayer != null && queue.Count > 0)
            {
                MediaMetadata.Builder metadataBuilder = new MediaMetadata.Builder();
                var tfile = TagLib.File.Create(queue[index]);
                // To provide most control over how an item is displayed set the
                // display fields in the metadata
                metadataBuilder.PutString(MediaMetadata.MetadataKeyDisplayTitle,
                        tfile.Tag.Title);
                // And at minimum the title and artist for legacy support
                metadataBuilder.PutString(MediaMetadata.MetadataKeyTitle,
                        tfile.Tag.Title);
                metadataBuilder.PutString(MediaMetadata.MetadataKeyDisplaySubtitle,
                        tfile.Tag.Album ?? tfile.Tag.Performers.FirstOrDefault());
                MemoryStream ms = new MemoryStream(tfile.Tag.Pictures.FirstOrDefault().Data.Data);
                metadataBuilder.PutBitmap(MediaMetadata.MetadataKeyDisplayIcon,
                        BitmapFactory.DecodeStream(ms));
                // A small bitmap for the artwork is also recommended
                metadataBuilder.PutBitmap(MediaMetadata.MetadataKeyArt,
                        BitmapFactory.DecodeStream(ms));
                ms.Dispose();
                metadataBuilder.PutString(MediaMetadata.MetadataKeyAlbum,
                        tfile.Tag.Album);
                metadataBuilder.PutString(MediaMetadata.MetadataKeyAlbumArtist,
                        tfile.Tag.Performers.FirstOrDefault());
                //Possible error in implementation
                metadataBuilder.PutString(MediaMetadata.MetadataKeyDuration,
                        tfile.Tag.Length);
                // Add any other fields you have for your data as well
                session.SetMetadata(metadataBuilder.Build());
                updatePlaybackState();
                side_player.populate_side_bar(MainActivity.stateHandler.view);
                tfile.Dispose();
            }
        }



        ///<summary>
        ///Requests focus and starts playing new song or resumes playback if playback was paused
        ///</summary>
        private void Play()
		{
            if (queue.Count == 0)
            {
                GenerateQueue(FileManager.GetSongs());
            }
			RequestFocus();
			if(session == null)
			{
				InnitSession();
			}
            if (!session.Active)
            {
                session.Active = true;
            }
            if (mediaPlayer == null)
			{
				InnitPlayer();
			}else if (!isPaused)
			{
				if (isUsed)
				{
					mediaPlayer.Reset();
				}
				mediaPlayer.SetDataSource(queue[index]);
                mediaPlayer.Prepare();
			}
            mediaPlayer.Start();
            
            if (isPaused)
            {
                isPaused = false;
                updatePlaybackState();
            }
            else
            {
                updateMetadata();
            }
            isSkippingToNext = false;
            isSkippingToPrevious = false;
        }

        ///<summary>
        ///Pauses playback
        ///</summary>
        private void Pause()
		{
			mediaPlayer.Pause();
			isPaused = true;
            updatePlaybackState();
        }

        ///<summary>
        ///Stops playing and abandons focus
        ///</summary>
        private void Stop()
		{
			mediaPlayer.Stop();
			AbandonFocus();
            updateMetadata();
        }

        ///<summary>
        ///Plays bext song in queue
        ///</summary>
        private void NextSong()
        {
            if(mediaPlayer != null)
            {
                isSkippingToNext = true;
                if (queue.Count - 1 > index)
                {
                    index++;
                    Play();
                }
                else if (loopAll)
                {
                    index = 0;
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
                index -= 2;
                if(index < 0)
                {
                    index = 0;
                }
                Play();
                isSkippingToPrevious = false;
            }
        }

        private void SeekTo(int millis)
        {
            mediaPlayer.SeekTo(millis);
            updatePlaybackState();
        }

        ///<summary>
        ///Generates new queue from single song path or directory path and set index to 0
        ///</summary>
        public void GenerateQueue(string source)
		{
			index = 0;
			if (FileManager.IsDirectory(source))
			{
				GenerateQueue(FileManager.GetSongs(source));
			}
			else
			{
				queue = new List<string> { source };
				Play();
			}
		}

		///<summary>
		///Generates new queue from list and resets index to 0
		///</summary>
		public void GenerateQueue(List<string> source, int i = 0)
		{
			queue = source;
			index = i;
			Shuffle(shuffle);
			Play();
		}

        ///<summary>
        ///Clears queue and resets index to 0
        ///</summary>
        public void ClearQueue()
        {
            queue = new List<string>();
            index = 0;
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
                queue.Add(addition);
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
            queue.AddRange(addition);
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
                queue.Insert(0, addition);
                if (shuffle)
                {
                    orignalQueue.Insert(0, addition);
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
				List<string> additionTmp = addition;
                additionTmp.AddRange(orignalQueue);
                orignalQueue = additionTmp;
            }
            addition.AddRange(queue);
            queue = addition;
        }

        ///<summary>
        ///Shuffles or unshuffles queue and updates shuffling for all new queues based on <paramref name="newShuffleState"/> state
        ///</summary>
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
				if (orignalQueue.Count > 0)
				{
					index = orignalQueue.IndexOf(queue[index]);
					queue = orignalQueue;
					orignalQueue = new List<string>();
				}
			}
			shuffle = newShuffleState;
			MainActivity.stateHandler.shuffle = newShuffleState;
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
			mediaPlayer.Looping = loopSingle;
			MainActivity.stateHandler.loopSingle = loopSingle;
			MainActivity.stateHandler.loopAll = loopAll;
			MainActivity.stateHandler.loopState = loopState;
		}

        ///<summary>
        ///Requests audio focus
        ///</summary>
        private void RequestFocus()
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
					if (!request.Equals(AudioFocus.Gain))
					{
						// handle any failed requests
					}
					else
					{
						isFocusGranted = true;
					}
				}
				else
				{
					var request = audioManager.RequestAudioFocus(this, Android.Media.Stream.Music, AudioFocus.Gain);
					if (request != AudioFocusRequest.Granted)
					{
						// handle any failed requests
					}
					else
					{
						isFocusGranted = true;
					}
				}
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
            isNotificationCreated = false;
        }

		public override IBinder OnBind(Intent intent)
		{
			return null;
		}
	}
}