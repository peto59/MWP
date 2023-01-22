﻿using Android.App;
using Android.Content;
using Android.Media;
using AndroidX.AppCompat.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Java.Util.Jar.Attributes;

namespace Ass_Pain
{
    [BroadcastReceiver(Enabled = false, Exported = false)]
    [IntentFilter(new[] { AudioManager.ActionAudioBecomingNoisy })]
    public class Slovenska_prostituka : BroadcastReceiver
    {
        private static MediaPlayer player = new MediaPlayer();
        private List<string> queue = new List<string>();
        private List<string> orignalQueue = new List<string>();
        private int index = 0;
        private bool used = false;
        AppCompatActivity view;
        Int16 loopState = 0;
        private bool loopSingle = false;
        private bool loopAll = false;
        private bool shuffle = false;
        /*public bool IsLoopingAll
        {
            get { return loopAll; }
        }*/

        /*public bool IsLoopingSingle
        {
            get { return loopSingle; }
        }*/
        public Int16 LoopState
        {
            get { return loopState; }
        }
        public bool IsShuffling
        {
            get { return shuffle; }
        }
        public bool IsPlaying
        {
            get { return player.IsPlaying; }
        }
        public int Duration
        {
            get { return player.Duration; }
        }
        public int CurrentPosition
        {
            get { return player.CurrentPosition; }
        }

        public Slovenska_prostituka()
        {
            player.Completion += NextSong;
            player.Prepared += (sender, ea) =>
            {
                used = true;
            };
        }

        public override void OnReceive(Context context, Intent intent)
        {
            Console.WriteLine("noisy");
            player.Pause();
        }

        public void SetView(AppCompatActivity new_view)
        {
            view = new_view;
        }

        public string NowPlaying()
        {
            try
            {
                return queue[index];
            }
            catch
            {
                return "prostitutka ja nai";
            }
        }

        public void Play()
        {
            if (used)
            {
                player.Reset();
            }
            player.SetDataSource(queue[index]);
            side_player.populate_side_bar(view);
            player.Prepare();
            player.Start();
            side_player.populate_side_bar(view);
        }

        public void NextSong(object sender = null, EventArgs e = null)
        {
            if (loopSingle && sender == null) 
            {
                Play();
            } else if (queue.Count > index)
            {
                index++;
                Play();
            } else if (loopAll)
            {
                index = 0;
                Play();
            }
        }

        public void PreviousSong(object sender = null, EventArgs e = null)
        {
            try
            {
                if(player.CurrentPosition > 10000)//if current song is playing longer than 10 seconds
                {
                    player.SeekTo(0);
                    return;
                }
                if (used)
                {
                    player.Reset();
                }
                index -= 2;
                player.SetDataSource(queue[index]);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                index = 0;
                player.SetDataSource(queue[index]);
            }
                side_player.populate_side_bar(view);
                index++;
                player.Prepare();
                player.Start();
        }

        public void Resume()
        {
            Console.WriteLine("Resumed");
            if (used)
            {
                player.Start();
            }
        }
        public void Stop()
        {
            Console.WriteLine("Stopped");
            player.Pause();
        }

        public bool TogglePlayButton(AppCompatActivity context)
        {
            if (player.IsPlaying)
            {
                side_player.SetPlayButton(context);
                player.Pause();
                return false;
            }
            else
            {
                if (used)
                {
                    player.Start();
                    side_player.SetStopButton(context);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        ///<summary>
        ///Clears queue and resets index to 0
        ///</summary>
        public void ClearQueue(object sender = null, EventArgs e = null)
        {
            queue = new List<string>();
            index = 0;
        }

        ///<summary>
        ///Generates clear queue from single track or entire album/author and resets index to 0
        ///</summary>
        public void GenerateQueue(string source)
        {
            side_player.SetStopButton(view);
            index = 0;
            if (FileManager.IsDirectory(source))
            {
                GenerateQueue(FileManager.GetSongs(source));
            }
            else
            {
                queue = new List<string>{source};
                Play();
            }
        }

        ///<summary>
        ///Generates clear queue from list and resets index to 0
        ///</summary>
        public void GenerateQueue(List<string> source, int i = 0)
        {
            side_player.SetStopButton(view);
            queue = source;
            index = i;
            Shuffle(shuffle);
            Play();
        }

        ///<summary>
        ///Adds single track or entire album/author to queue
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
            }
        }

        ///<summary>
        ///Adds list of songs to queue
        ///</summary>
        public void AddToQueue(List<string> addition)
        {
            queue.AddRange(addition);
        }

        ///<summary>
        ///Adds song or entire album/author as first to queue
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
            }
            
        }
        
        ///<summary>
        ///Adds song list as first to queue
        ///</summary>
        public void PlayNext(List<string> addition)
        {
            addition.AddRange(queue);
            queue = addition;
        }

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
                if(orignalQueue.Count > 0)
                {
                    index = orignalQueue.IndexOf(queue[index]);
                    queue = orignalQueue;
                }
                orignalQueue = new List<string>();
            }
            shuffle = newShuffleState;
        }

        public void ToggleLoop(Int16 state)
        {
            loopState = state;
            switch (state) { 
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
                default: break;
            }
        }
    }
}