using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ass_Pain
{
    internal static class FileManager
    {
        public static List<string> GetAuthors()
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

        public static string GetAlias(string name)
        {
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string json = File.ReadAllText($"{path}/aliases.json");
            Dictionary<string, string> aliases = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (aliases.ContainsKey(name))
            {
                return aliases[name];
            }
            return name;
        }

        public static void AddAlias(string name, string target)
        {
            if(name == target)
            {
                return;
            }
            string author = Sanitize(target);

            string nameFile = Sanitize(name);

            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string json = File.ReadAllText($"{path}/aliases.json");
            Dictionary<string, string> aliases = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            aliases.Add(nameFile, nameFile);
            File.WriteAllTextAsync($"{path}/alises.json", JsonConvert.SerializeObject(aliases));
            Directory.Move($"{path}/music/{nameFile}", $"{path}/music/{author}");
            foreach (string song in FileManager.GetSongs($"{path}/music/{author}"))
            {
                var tfile = TagLib.File.Create($"{song}");
                string[] autors = tfile.Tag.Performers;
                for (int i = 0; i < autors.Length; i++)
                {
                    if (autors[i] == name)
                    {
                        autors[i] = target;
                    }
                    else
                    {
                        //TODO: add symlink move
                        if (i == 0)
                        {
                            continue;
                        }
                        //Android.Systems.Os.Symlink();
                    }
                }
                tfile.Tag.Performers = autors;
                tfile.Save();
            }
        }

        public static void CreatePlaylist(string name)
        {
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string json = File.ReadAllText($"{path}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            playlists.Add(name, new List<string>());
            File.WriteAllTextAsync($"{path}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        public static void AddToPlaylist(string name, string song)
        {
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string json = File.ReadAllText($"{path}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            playlists[name].Add(song);
            File.WriteAllTextAsync($"{path}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        public static void AddToPlaylist(string name, List<string> songs)
        {
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string json = File.ReadAllText($"{path}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            playlists[name].AddRange(songs);
            File.WriteAllTextAsync($"{path}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        public static string Sanitize(string value)
        {
            return value.Replace("/", "").Replace("#", "").Replace("?", "");
        }

        public static int GetAvailableFile()
        {
            int i = 0;
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            while (File.Exists($"{path}/tmp/video{i}.mp3"))
            {
                i++;
            }
            string dest = $"{path}/tmp/video{i}.mp3";
            File.Create(dest).Close();
            return i;
        }

        ///<summary>
        ///Gets all playlist names
        ///</summary>
        public static List<string> GetPlaylist()
        {
            List<string> list = new List<string>();
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string json = File.ReadAllText($"{path}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            foreach (KeyValuePair<string, List<string>> pr in playlists)
            {
                list.Add(pr.Key);
            }
            return list;
        }

        ///<summary>
        ///Gets all songs in <paramref name="playlist"/>
        ///<br>Returns <returns>null</returns> if <paramref name="playlist"/> doesn't exist</br>
        ///</summary>
        public static List<string> GetPlaylist(string playlist)
        {
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string json = File.ReadAllText($"{path}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            if (playlists.ContainsKey(playlist))
            {
                return playlists[playlist];
            }
            return null;
            
        }
    }
}