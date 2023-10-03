using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ass_Pain.BackEnd.Player
{
    public class MyMediaQueue
    {
        //-----------------------Private helpers--------------------
        private List<Song> _queue = new List<Song>();
        private List<Song> _originalQueue = new List<Song>();
        private static Song defaultSong = new Song("No Name", new DateTime(), "Default", false);
        private readonly AutoResetEvent shuffling = new AutoResetEvent(true);
        private bool loopAll = false;
        private bool loopSingle = false;
        //-----------------------Private helpers--------------------
        
        
        //-------------------Public interfaces------------
        public bool IsShuffled { get; private set; } = false;
        public Enums.LoopState LoopState { get; private set; } = Enums.LoopState.None;
        public IReadOnlyList<Song> Queue => _queue.AsReadOnly();
        public int QueueCount => _queue.Count;
        public bool HasNext => Index < QueueCount - 1;
        public bool HasPrevious => Index > 0;
        
        public int Index { get; private set; } = 0; //todo: checks on int.

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
            QueueObject = source.ToList();
            Index = ind;
            if (IsShuffled)
            {
                Shuffle(true, ind);
            }

        }
        
        public void Shuffle(bool newShuffleState, int? indx = null)
        {
            if(QueueCount == 0) { return; }

            int ind = indx ?? Index;

            shuffling.WaitOne();
            if (newShuffleState)
            {
                _originalQueue = _queue.ToList();
                Song tmp = _queue.Pop(ind);
                Index = 0;
                _queue.Shuffle();
                _queue = _queue.Prepend(tmp).ToList();
            }
            else
            {
                if (_queue.Count > 0)
                {
                    Index = originalQueue.IndexOf(QueueObject[Index]);
                    QueueObject = originalQueue;
                    originalQueue = new List<Song>();
                }
            }
            IsShuffled = newShuffleState;
            MainActivity.stateHandler.shuffle = newShuffleState;
            UpdatePlaybackState();
            if (Assets != null) side_player.populate_side_bar(MainActivity.stateHandler.view, Assets);
            shuffling.Set();
        }
        //------------------------Functions------------------------
    }
}