using Android.App;
using Android.Content;
using Android.Media;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Player
{
    /// <inheritdoc />
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { AudioManager.ActionAudioBecomingNoisy })]
    public class MyBroadcastReceiver :  BroadcastReceiver
    {
        /// <inheritdoc />
        public override void OnReceive(Context? context, Intent? intent)
        {
#if DEBUG
            MyConsole.WriteLine("noisy");
#endif
            MainActivity.ServiceConnection.Binder?.Service.Pause();
        }
    }


    /// <summary>
    /// Broadcast receiver for basic media playback actions
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = false)]
    [IntentFilter(new[] { PLAY, PAUSE, SHUFFLE, TOGGLE_LOOP, NEXT_SONG, PREVIOUS_SONG })]
    public class MyMediaBroadcastReceiver : BroadcastReceiver
    {
        ///<summary>
        ///Requests focus and starts playing new song or resumes playback if playback was paused
        ///</summary>
        public const string PLAY = "ActionPlay";

        ///<summary>
        ///Pauses playback
        ///</summary>
        public const string PAUSE = "ActionPause";
        
		
        ///<summary>
        ///Shuffles or unshuffles queue and updates shuffling for all new queues oposite to last state
        ///</summary>
        public const string SHUFFLE = "ActionShuffle";

        ///<summary>
        ///Changes current loop state based on last state
        ///</summary>
        public const string TOGGLE_LOOP = "ActionToggleLoop";
        

        ///<summary>
        ///Plays next song in queue
        ///</summary>
        public const string NEXT_SONG = "ActionNextSong";

        ///<summary>
        ///Plays previous song in queue
        ///</summary>
        public const string PREVIOUS_SONG = "ActionPreviousSong";


        /// <inheritdoc />
        public override void OnReceive(Context? context, Intent? intent)
        {
            switch (intent?.Action)
            {
                case PLAY:
                    MainActivity.ServiceConnection.Binder?.Service.Play();
                    break;
                case PAUSE:
                    MainActivity.ServiceConnection.Binder?.Service.Pause();
                    break;
                case SHUFFLE:
                    MainActivity.ServiceConnection.Binder?.Service.Shuffle(intent.GetBooleanExtra("shuffle", false));
                    break;
                case NEXT_SONG:
                    MainActivity.ServiceConnection.Binder?.Service.NextSong();
                    break;
                case PREVIOUS_SONG:
                    MainActivity.ServiceConnection.Binder?.Service.PreviousSong();
                    break;
            }
        }
    }
}