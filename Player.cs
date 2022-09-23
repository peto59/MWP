using Android.App;
using Android.Media;
using AndroidX.AppCompat.App;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ass_Pain
{
    public class Slovenska_prostituka
    {
        protected MediaPlayer player = new MediaPlayer();
        protected List<string> queue = new List<string>();
        int index = 0;
        bool used = false;
        AppCompatActivity view;

        public Slovenska_prostituka()
        {
            player.Completion += NextSong;
            player.Prepared += (sender, ea) =>
            {
                used = true;
            };
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

        public void Play(string source)
        {
            GenerateQueue(source);
        }

        public void NextSong(object sender = null, EventArgs e = null)
        {
            if (queue.Count > index)
            {
                if (used)
                {
                    player.Reset();
                }
                player.SetDataSource(queue[index]);
                side_player.populate_side_bar(view);
                index++;
                player.Prepare();
                player.Start();
            }
        }

        public void Resume(object sender, EventArgs e)
        {
            Console.WriteLine("Resumed");
            if (used)
            {
                player.Start();
            }
        }
        public void Stop(object sender, EventArgs e)
        {
            Console.WriteLine("Stopped");
            player.Pause();
        }

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
            index = 0;
            if (FileManager.IsDirectory(source))
            {
                GenerateQueue(FileManager.GetSongs(source));
            }
            else
            {
                queue = new List<string>{source};
                NextSong();

            }
        }

        ///<summary>
        ///Generates clear queue from list and resets index to 0
        ///</summary>
        public void GenerateQueue(List<string> source)
        {
            index = 0;
            queue = source;
            NextSong();
        }

        public void GenerateQueue(List<string> source, int i)
        {
            index = i;
            queue = source;
            NextSong();
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
    }
}