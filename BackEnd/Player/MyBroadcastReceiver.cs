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


    [BroadcastReceiver(Enabled = true, Exported = false)]
    [IntentFilter(new[] { ActionPlay, ActionPause, ActionShuffle, ActionToggleLoop, ActionNextSong, ActionPreviousSong })]
    public class MyMediaBroadcastReceiver : BroadcastReceiver
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
        ///Shuffles or unshuffles queue and updates shuffling for all new queues oposite to last state
        ///</summary>
        public const string ActionShuffle = "ActionShuffle";

        ///<summary>
        ///Changes current loop state based on last state
        ///</summary>
        public const string ActionToggleLoop = "ActionToggleLoop";
        

        ///<summary>
        ///Plays next song in queue
        ///</summary>
        public const string ActionNextSong = "ActionNextSong";

        ///<summary>
        ///Plays previous song in queue
        ///</summary>
        public const string ActionPreviousSong = "ActionPreviousSong";

        
        public override void OnReceive(Context? context, Intent? intent)
        {
            switch (intent?.Action)
            {
                case ActionPlay:
                    MainActivity.ServiceConnection.Binder?.Service.Play();
                    break;
                case ActionPause:
                    MainActivity.ServiceConnection.Binder?.Service.Pause();
                    break;
                case ActionShuffle:
                    MainActivity.ServiceConnection.Binder?.Service.Shuffle(intent.GetBooleanExtra("shuffle", false));
                    break;
                case ActionNextSong:
                    MainActivity.ServiceConnection.Binder?.Service.NextSong();
                    break;
                case ActionPreviousSong:
                    MainActivity.ServiceConnection.Binder?.Service.PreviousSong();
                    break;
            }
        }
    }
}