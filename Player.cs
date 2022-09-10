using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Media;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Graphics;
namespace Ass_Pain
{
    internal class Player
    {
        protected MediaPlayer player;
        protected List<string> queue = new List<string>();

        public void Play(object sender, EventArgs e)
        {
            //var folder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            Console.WriteLine("Clicked");
            var folder = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            Console.WriteLine(folder);
            var filesList = Directory.GetFiles(folder);
            foreach (var file in filesList)
            {
                Console.WriteLine(file);
                if (player == null)
                {
                    player = new MediaPlayer();
                }
                else
                {
                    player.Reset();
                }
                player.SetDataSource(file);
                player.Prepare();
                player.Start();
            }
            //RootDirectory();
        }

        public void stop(object sender, EventArgs e)
        {
            Console.WriteLine("Stopped");
            player.Pause();
        }

        public void AddToQueue()
        {
            queue = new List<string>();

        }
        public static void GetAutors(object sender, EventArgs e)
        {
            var root = Directory.EnumerateDirectories($"{Application.Context.GetExternalFilesDir(null).AbsolutePath}/music");
            foreach (string autor in root)
            {
                Console.WriteLine(autor);
            }
        }
        public static void GetAlbums(object sender, EventArgs e)
        {
            //var mp3Files = Directory.EnumerateFiles(path1, "*.mp3", SearchOption.AllDirectories);
            var root = Directory.EnumerateDirectories($"{Application.Context.GetExternalFilesDir(null).AbsolutePath}/music");
            foreach (string autor in root)
            {
                foreach(string album in Directory.EnumerateDirectories(autor))
                {
                    Console.WriteLine(album);

                }
            }
        }
        public static List<string> GetSongs(string path)
        {
            List<string> songs = new List<string>();
            var mp3Files = Directory.EnumerateFiles(path, "*.mp3");
            foreach (string currentFile in mp3Files)
            {
                Console.WriteLine(currentFile);
                songs.Add(currentFile);
            }
            return songs;
        }
        public static void GetAllSongs(object sender, EventArgs e)
        {
            var root = $"{Application.Context.GetExternalFilesDir(null).AbsolutePath}/music";
            var mp3Files = Directory.EnumerateFiles(root, "*.mp3", SearchOption.AllDirectories);
            foreach (string currentFile in mp3Files)
            {
                Console.WriteLine(currentFile);
            }
        }
    }
}