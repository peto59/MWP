using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace MWP.BackEnd.Player
{
    public class MyMediaQueue
    {
        //-----------------------Private helpers--------------------
        private List<Song> _queue = new List<Song>();
        private List<Song> _originalQueue = new List<Song>();
        private static Song defaultSong = new Song("No Name", new DateTime(), "Default", false);
        private readonly AutoResetEvent shuffling = new AutoResetEvent(true);
        //private bool loopAll = false;
        //private bool loopSingle = false;
        private bool _isShuffled = false;
        private int _index = 0;
        //-----------------------Private helpers--------------------
        
        
        //-------------------Public interfaces------------
        public bool IsShuffled
        {
            get => _isShuffled;
            set
            {
                if (value == _isShuffled) return;
                _isShuffled = value;
                MainActivity.stateHandler.shuffle = value;
                if (value)
                    Shuffle(); 
                else
                    UnShuffle();
            }
        }

        public Enums.LoopState LoopState { get; private set; } = Enums.LoopState.None;
        public IReadOnlyList<Song> Queue => _queue.AsReadOnly();
        public int QueueCount => _queue.Count;
        public bool HasNext => Index < QueueCount - 1;
        public bool ShowNext => HasNext || LoopState == Enums.LoopState.All;
        public bool HasPrevious => Index > 0;
        public bool ShowPrevious => HasPrevious || LoopState == Enums.LoopState.All;

        public int Index
        {
            get => _index;
            private set
            {
                if (value < 0 && LoopState == Enums.LoopState.All)
                {
                    _index = QueueCount - 1;
                }
                else if (value < 0)
                {
                    _index = 0;
                }
                else if (value >= QueueCount && LoopState == Enums.LoopState.All)
                {
                    _index = 0;
                }
                else if (value >= QueueCount)
                {
                    return;
                }
                else
                {
                    _index = value;
                }
            }
        }

        public Song Current
        {
            get
            {
                try
                {
                    return Queue[Index] ?? defaultSong;
                }
                catch
                {
                    return defaultSong;
                }
            }
        }
        //-------------------Public interfaces------------
        
        
        
        //----------------------Functions------------------
        public void GenerateQueue(IEnumerable<Song> source, int ind = 0)
        {
            _queue = source.ToList();
            Index = ind;
            if (IsShuffled)
            {
                Shuffle();
            }
        }

        public void AppendToQueue(IEnumerable<Song> addition)
        {
            _queue.AddRange(addition);
            if (IsShuffled)
            {
                _originalQueue.AddRange(addition);
            }
        }
        
        public void PrependToQueue(List<Song> addition)
        {
            ReadOnlyCollection<Song> readOnlyCollection = addition.AsReadOnly();
            if (IsShuffled)
            {
                List<Song> tmp = new List<Song>(readOnlyCollection);
                tmp.AddRange(_originalQueue);
                _originalQueue = tmp;
            }
            if(!HasNext)
            {
                AppendToQueue(addition);
            }
            else
            {
                List<Song> tmp = _queue.GetRange(0, Index+1);
                tmp.AddRange(addition);
                tmp.AddRange(_queue.Skip(Index + 1));
                _queue = tmp;
            }
        }
        
        private void Shuffle()
        {
            shuffling.WaitOne();
            if(QueueCount == 0) { return; }

            _originalQueue = _queue.ToList();
            Song tmp = _queue.Pop(Index);
            Index = 0;
            _queue.Shuffle();
            _queue = _queue.Prepend(tmp).ToList();
            
            shuffling.Set();
        }

        private void UnShuffle()
        {
            shuffling.WaitOne();
            if (_originalQueue.Count <= 0) return;
            
            Index = _originalQueue.IndexOf(Current);
            _queue = _originalQueue.ToList();
            _originalQueue = new List<Song>();
            
            shuffling.Set();
        }
        
        public bool IncrementIndex()
        {
            if (Index + 1 >= QueueCount && LoopState != Enums.LoopState.All)
            {
                return false;
            }

            Index++;
            return true;
        }

        public void DecrementIndex()
        {
            Index--;
        }

        public void ToggleLoop(int state)
        {
            state %= 3;
            LoopState = (Enums.LoopState)state;
            MainActivity.stateHandler.loopState = LoopState;
        }
        //------------------------Functions------------------------
    }
}