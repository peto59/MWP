using Android.App;
using Android.Content;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Java.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static Android.Renderscripts.Sampler;

namespace Ass_Pain
{
    public class StateHandler
    {
        public CancellationTokenSource cts = new CancellationTokenSource();
        public AppCompatActivity view;
        private MediaPlayer mediaPlayer = null;
        public bool shuffle = false;
        public bool loopAll = false;
        public bool loopSingle = false;
        private List<string> queue = new List<string>();
        private int index = 0;
        public int loopState = 0;
        public bool ProgTimeState
        {
            get; set;
        }

        public List<Song> Songs = new List<Song>();
        public List<Artist> Artists = new List<Artist>();
        public List<Album> Albums = new List<Album>();

        ///<summary>
        ///Return current playback position
        ///</summary>
        public int CurrentPosition
        {
            get
            {
                if (mediaPlayer != null)
                {
                    return mediaPlayer.CurrentPosition;
                }
                else
                {
                    return 0;
                }
            }
        }

        ///<summary>
        ///Returns duration of current song
        ///</summary>
        public int Duration
        {
            get
            {
                if (mediaPlayer != null)
                {
                    return mediaPlayer.Duration;
                }
                else
                {
                    return 0;
                }
            }
        }

        ///<summary>
        ///Returns path to currently playing song or empty string if no playback is active
        ///</summary>
        public string NowPlaying
        {
            get {
                Console.WriteLine($"queue count: {queue.Count}");
                Console.WriteLine($"index: {index}");
                if (queue.Count > 0 && queue.Count > index)
                {
                    Console.WriteLine($"my queue: {queue[index]}");
                    return queue[index];
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        ///<summary>
        ///Moves playback of current song to <paramref name="value"/> time in milliseconds
        ///</summary>
        /*public void SeekTo(int value)
        {
            try
            {
                mediaPlayer.SeekTo(value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }*/

        ///<summary>
        ///Returns current loop state
        ///</summary>
        public int LoopState
        {
            get { return loopState; }
        }

        ///<summary>
        ///Return bool based on if shuffling is enabled
        ///</summary>
        public bool IsShuffling
        {
            get { return shuffle; }
        }

        ///<summary>
        ///Return bool whether playback is currently active
        ///</summary>
        public bool IsPlaying
        {
            get {
                try
                {
                    return mediaPlayer.IsPlaying; 
                } 
                catch
                {
                    return false;
                }
            }
        }

        ///<summary>
        ///Sets view to current screen's view
        ///</summary>
        public void SetView(AppCompatActivity new_view)
        {
            view = new_view;
        }

        public void setMediaPlayer(ref MediaPlayer x)
        {
            mediaPlayer = x;
        }

        public void setQueue(ref List<string> x)
        {
            queue = x;
        }

        public void setIndex(ref int x)
        {
            index = x;
        }
    }
}