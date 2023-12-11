using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Support.V4.Media.Session;

namespace MWP.BackEnd.Player
{
    /// <summary>
    /// Custom media queue for internal tracking of queue
    /// </summary>
    public class MyMediaQueue
    {
        //-----------------------Private helpers--------------------
        private List<Song> queue = new List<Song>();
        private List<Song> originalQueue = new List<Song>();
        private static readonly Song DefaultSong = new Song("No Name", new DateTime(), "Default", false);
        private readonly AutoResetEvent shuffling = new AutoResetEvent(true);
        //private bool loopAll = false;
        //private bool loopSingle = false;
        private bool isShuffled;
        private int index;
        private CancellationTokenSource cancellationToken = new CancellationTokenSource();
        private readonly MediaSessionCompat session;
        //-----------------------Private helpers--------------------

        /// <summary>
        /// Creates new queue object
        /// </summary>
        /// <param name="ses">Session to which send changes</param>
        public MyMediaQueue(MediaSessionCompat ses)
        {
            session = ses;
        }
        
        //-------------------Public interfaces------------
        /// <summary>
        /// Whether current is shuffled or not
        /// </summary>
        public bool IsShuffled
        {
            get => isShuffled;
            set
            {
                if (value == isShuffled) return;
                isShuffled = value;
                if (value)
                    Shuffle(); 
                else
                    UnShuffle();
            }
        }

        /// <summary>
        /// Current loop state
        /// </summary>
        public LoopState LoopState { get; private set; } = LoopState.None;
        /// <summary>
        /// Current queue
        /// </summary>
        public IReadOnlyList<Song> Queue => queue.AsReadOnly();
        /// <summary>
        /// Length of current queue
        /// </summary>
        public int QueueCount => queue.Count;
        /// <summary>
        /// Whether skipping to next is possible
        /// </summary>
        public bool HasNext => Index < QueueCount - 1;
        /// <summary>
        /// Whether to show button to skip to next song
        /// </summary>
        public bool ShowNext => HasNext || LoopState == LoopState.All;
        /// <summary>
        /// Whether previous to next is possible
        /// </summary>
        public bool HasPrevious => Index > 0;
        /// <summary>
        /// Whether to show button to skip to previous song
        /// </summary>
        public bool ShowPrevious => HasPrevious || LoopState == LoopState.All;

        /// <summary>
        /// Currently playing index
        /// </summary>
        public int Index
        {
            get => index;
            private set
            {
                switch (value)
                {
                    case < 0 when LoopState == LoopState.All:
                        index = QueueCount - 1;
                        break;
                    case < 0:
                        index = 0;
                        break;
                    default:
                    {
                        if (value >= QueueCount && LoopState == LoopState.All)
                        {
                            index = 0;
                        }
                        else if (value >= QueueCount)
                        {
                        }
                        else
                        {
                            index = value;
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Currently playing <see cref="Song"/>
        /// </summary>
        public Song Current
        {
            get
            {
                try
                {
                    return Queue[Index] ?? DefaultSong;
                }
                catch
                {
                    return DefaultSong;
                }
            }
        }
        //-------------------Public interfaces------------
        
        
        
        //----------------------Functions------------------
        private void SessionEnqueue()
        {
            new Task(() => {
                long i = 0;
                List<MediaSessionCompat.QueueItem?> tempQueue = queue.Select(s => s.ToQueueItem(i++)).ToList();
                List<MediaSessionCompat.QueueItem> queueLocal = tempQueue.Where(q => q != null).ToList()!;
                session.SetQueue(queueLocal);
            }, cancellationToken.Token).Start();
        }
        /// <summary>
        /// Clears current <see cref="Queue"/> and generates new <see cref="Queue"/>
        /// </summary>
        /// <param name="source">Content of new <see cref="Queue"/></param>
        /// <param name="id">id of songs object to be played for <see cref="Index"/> lookup purposes</param>
        public void GenerateQueue(IEnumerable<Song> source, Guid? id)
        {
            cancellationToken.Cancel();
            queue = source.ToList();
            Index = id != null ? queue.FindIndex(s => s.Id.Equals(id)) : 0;
            if (IsShuffled)
            {
                Shuffle();
            }
            cancellationToken = new CancellationTokenSource();
            SessionEnqueue();
        }

        /// <summary>
        /// Adds <paramref name="addition"/> to end of <see cref="Queue"/>
        /// </summary>
        /// <param name="addition">Content to add to end of <see cref="Queue"/></param>
        public void AppendToQueue(IEnumerable<Song> addition)
        {
            IEnumerable<Song> collection = addition.ToList();
            queue.AddRange(collection);
            if (IsShuffled)
            {
                originalQueue.AddRange(collection);
            }
            SessionEnqueue();
        }
        
        /// <summary>
        /// Adds <paramref name="addition"/> to start of <see cref="Queue"/>
        /// </summary>
        /// <param name="addition">Content to add to start of <see cref="Queue"/></param>
        public void PrependToQueue(List<Song> addition)
        {
            ReadOnlyCollection<Song> readOnlyCollection = addition.AsReadOnly();
            if (IsShuffled)
            {
                List<Song> tmp = new List<Song>(readOnlyCollection);
                tmp.AddRange(originalQueue);
                originalQueue = tmp;
            }
            if(!HasNext)
            {
                AppendToQueue(addition);
            }
            else
            {
                List<Song> tmp = queue.GetRange(0, Index+1);
                tmp.AddRange(addition);
                tmp.AddRange(queue.Skip(Index + 1));
                queue = tmp;
            }
            SessionEnqueue();
        }
        
        private void Shuffle()
        {
            shuffling.WaitOne();
            if(QueueCount == 0) { return; }

            originalQueue = queue.ToList();
            Song tmp = queue.Pop(Index);
            Index = 0;
            queue.Shuffle();
            queue = queue.Prepend(tmp).ToList();
            
            shuffling.Set();
            SessionEnqueue();
        }

        private void UnShuffle()
        {
            shuffling.WaitOne();
            if (originalQueue.Count <= 0) return;
            
            Index = originalQueue.IndexOf(Current);
            queue = originalQueue.ToList();
            originalQueue = new List<Song>();
            
            shuffling.Set();
            SessionEnqueue();
        }
        
        /// <summary>
        /// Sets <see cref="Index"/> to <paramref name="id"/> if possible
        /// </summary>
        /// <param name="id">New <see cref="Index"/></param>
        /// <returns>true if jumping to new index is possible, false otherwise</returns>
        public bool SetIndex(long id)
        {
            if (id >= QueueCount)
            {
                return false;
            }

            Index = (int)id;
            return true;
        }
        
        /// <summary>
        /// Increments <see cref="Index"/> by one
        /// </summary>
        /// <returns>true if incrementing <see cref="Index"/> is possible, false otherwise</returns>
        public bool IncrementIndex()
        {
            if (Index + 1 >= QueueCount && LoopState != LoopState.All)
            {
                return false;
            }

            Index++;
            return true;
        }

        /// <summary>
        /// Decrements <see cref="Index"/> by one, guards against decrementing below 0
        /// </summary>
        public void DecrementIndex()
        {
            Index--;
        }

        /// <summary>
        /// Sets new loop state
        /// </summary>
        /// <param name="state">new loop state as int representing index of new loop state in <see cref="LoopState"/></param>
        public void ToggleLoop(int state)
        {
            state %= 3;
            LoopState = (LoopState)state;
        }
        //------------------------Functions------------------------
    }
}