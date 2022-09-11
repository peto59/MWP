using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ass_Pain
{
    internal static class FileManager
    {
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
        ///Gets all albums from <paramref name="author"/>
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

        ///<summary>
        ///Gets last name/folder from <paramref name="path"/>
        ///</summary>
        public static string GetNameFromPath(string path)
        {
            string[] subs = path.Split('/');
            return subs[^1];
        }

        ///<summary>
        ///Gets album name and author from album <paramref name="path"/>
        ///</summary>
        public static Dictionary<string, string> GetAlbumAuthorFromPath(string path)
        {
            Dictionary<string, string> names = new Dictionary<string, string>
            {
                { "author", GetNameFromPath(path) },
                { "album", GetNameFromPath(Path.GetDirectoryName(path)) }
            };
            return names;
        }

        public static bool IsDirectory(string path)
        {
            return string.IsNullOrEmpty(Path.GetFileName(path)) || Directory.Exists(path);
        }
    }
}