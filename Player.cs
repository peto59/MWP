using Android.App;
using Android.Media;
using System;
using System.Collections.Generic;
using System.IO;

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
        public static List<string> GetAuthors(object sender, EventArgs e)
        {
            var root = Directory.EnumerateDirectories($"{Application.Context.GetExternalFilesDir(null).AbsolutePath}/music");
            List<string> authors = new List<string>();
            foreach (string author in root)
            {
                Console.WriteLine(author);
                authors.Add(author);
            }
            return authors;
        }
        ///<summary>
        ///Gets all albums of all authors
        ///</summary>
        public static List<string> GetAlbums()
        {
            var root = Directory.EnumerateDirectories($"{Application.Context.GetExternalFilesDir(null).AbsolutePath}/music");
            List<string> albums = new List<string>();

            foreach (string author in root)
            {
                foreach (string album in Directory.EnumerateDirectories(author))
                {
                    Console.WriteLine(album);
                    albums.Add(album);
                }
            }
            return albums;
        }
        ///<summary>
        ///Gets all albums from author
        ///</summary>
        public static List<string> GetAlbums(string author)
        {
            List<string> albums = new List<string>();
            foreach (string album in Directory.EnumerateDirectories(author))
            {
                Console.WriteLine(album);
                albums.Add(album);
            }
            return albums;
        }
        ///<summary>
        ///Gets all songs in album or all albumless songs for author
        ///</summary>
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
        ///<summary>
        ///Gets all songs in device
        ///</summary>
        public static List<string> GetSongs()
        {
            var root = $"{Application.Context.GetExternalFilesDir(null).AbsolutePath}/music";
            var mp3Files = Directory.EnumerateFiles(root, "*.mp3", SearchOption.AllDirectories);
            List<string> songs = new List<string>();
            foreach (string currentFile in mp3Files)
            {
                Console.WriteLine(currentFile);
                songs.Add(currentFile);
            }
            return songs;
        }
        public static string GetSongTitle(string path)
        {
            var tfile = TagLib.File.Create(path);
            return tfile.Tag.Title;
        }
        public static List<string> GetSongTitle(List<string> Files)
        {
            List<string> titles = new List<string>();
            foreach (string currentFile in Files)
            {
                var tfile = TagLib.File.Create(currentFile);
                titles.Add(tfile.Tag.Title);
            }
            return titles;
        }
        public static string GetNameFromPath(string path)
        {
            return path.Substring(path.LastIndexOf('/'), path.Length);
        }
        public static List<string> GetNameAuthorFromPath(string path)
        {
            List<string> names = new List<string>();
            names.Add(GetNameFromPath(path));
            names.Add(Path.GetDirectoryName(path));
            return names;
        }
    }
}