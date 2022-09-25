﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Service.QuickSettings;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Ass_Pain
{
    internal static class FileManager
    {
        static string root = (string)Android.OS.Environment.ExternalStorageDirectory;
        static string music_folder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
        public static void DiscoverFiles(string path = null)
        {
            if(path == null)
            {
                path = root;
            }
            if(GetNameFromPath(path) == "Android")
            {
                return;
            }
            if (GetNameFromPath(path).StartsWith("."))
            {
                return;
            }
            if (File.Exists($"{path}/.nomedia"))
            {
                return;
            }
            if (GetNameFromPath(path) == "sound_recorder")
            {
                return;
            }
            if (path == music_folder)
            {
                return;
            }
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                DiscoverFiles(dir);
            }
            
            foreach (var file in Directory.EnumerateFiles(path, "*.mp3"))
            {
                try
                {
                    string title = GetSongTitle(file);
                    if (title == null)
                    {
                        SetSongTitle(file);
                        title = GetNameFromPath(file).Replace(".mp3", "");
                    }
                    if (title.Contains(".mp3"))
                    {
                        var tfile = TagLib.File.Create(file);
                        tfile.Tag.Title = tfile.Tag.Title.Replace(".mp3", "");
                        title = tfile.Tag.Title;
                        tfile.Save();
                    }
                    title = Sanitize(title);
                    string[] uartists = GetSongArtist(file);
                    string artist;
                    if (uartists.Length > 0)
                    {
                        artist = GetAlias(Sanitize(uartists[0]));
                    }
                    else
                    {
                        artist = "No Artist";
                    }
                    string uAlbum = GetSongAlbum(file);
                    Console.WriteLine("Moving " + file);
                    if (uAlbum != null)
                    {
                        string album = Sanitize(uAlbum);
                        Directory.CreateDirectory($"{music_folder}/{artist}/{album}");
                        File.Move(file, $"{music_folder}/{artist}/{album}/{title}.mp3");
                    }
                    else
                    {
                        Directory.CreateDirectory($"{music_folder}/{artist}");
                        File.Move(file, $"{music_folder}/{artist}/{title}.mp3");
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"error: {file}");
                    Console.WriteLine(ex);
                }
            }
        }
        public static List<string> GetAuthors()
        {
            List<string> authors = new List<string>();
            foreach (string author in Directory.EnumerateDirectories(music_folder))
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
            List<string> albums = new List<string>();

            foreach (string author in Directory.EnumerateDirectories(music_folder))
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
        /// Deletes file on <paramref name="path"/>
        ///</summary>
        public static void Delete(string path)
        {
            if (IsDirectory(path))
            {
                foreach (var playlistName in GetPlaylist())
                {
                    foreach(var file in GetSongs(path))
                    {
                        DeletePlaylist(playlistName, file);
                    }
                }
                Directory.Delete(path, true);
            }
            else
            {
                File.Delete(path);
                foreach (var playlistName in GetPlaylist())
                {
                    DeletePlaylist(playlistName, path);
                }
            }
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
        ///Gets all songs in device
        ///</summary>
        public static List<string> GetSongs()
        {
            var mp3Files = Directory.EnumerateFiles(music_folder, "*.mp3", SearchOption.AllDirectories);
            List<string> songs = new List<string>();
            foreach (string currentFile in mp3Files)
            {
                Console.WriteLine(currentFile);
                songs.Add(currentFile);
            }
            return songs;
        }

        ///<summary>
        ///Gets all songs in album or all albumless songs for author
        ///</summary>
        public static List<string> GetSongs(string path)
        {
            var mp3Files = Directory.EnumerateFiles(path, "*.mp3");
            List<string> songs = new List<string>();
            foreach (string currentFile in mp3Files)
            {
                Console.WriteLine(currentFile);
                songs.Add(currentFile);
            }
            return songs;
        }

        public static void SetSongTitle(string file)
        {
            if (File.Exists(file))
            {
                var tfile = TagLib.File.Create(file);
                tfile.Tag.Title = GetNameFromPath(file).Replace(".mp3", "");
                tfile.Save();
            }

        }

        public static string GetSongTitle(string path)
        {
            if (File.Exists(path))
            {
                var tfile = TagLib.File.Create(path);
                return tfile.Tag.Title;
            }
            else
            {
                return "cant get title";
            }
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

        public static string GetSongAlbum(string path)
        {
            if (File.Exists(path))
            {
                var tfile = TagLib.File.Create(path);
                return tfile.Tag.Album;
            }
            else
            {
                return "cant get album";
            }
        }
        public static string[] GetSongArtist(string path)
        {
            if (File.Exists(path))
            {
                var tfile = TagLib.File.Create(path);
                if (tfile.Tag.Performers.Length == 0)
                {
                    return tfile.Tag.AlbumArtists;
                }
                return tfile.Tag.Performers;
            }
            else {
                return new string[] { "cant get artist" };
            }
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
        public static (string album, string autor) GetAlbumAuthorFromPath(string path)
        {
            return (GetNameFromPath(path), GetNameFromPath(Path.GetDirectoryName(path)));
        }

        public static bool IsDirectory(string path)
        {
            return string.IsNullOrEmpty(Path.GetFileName(path)) || Directory.Exists(path);
        }

        public static string GetAlias(string name)
        {
            string json = File.ReadAllText($"{music_folder}/aliases.json");
            Dictionary<string, string> aliases = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (aliases.ContainsKey(name))
            {
                return GetAlias(aliases[name]);
            }
            else if (aliases.ContainsKey(Sanitize(name)))
            {
                return GetAlias(aliases[Sanitize(name)]);
            }
            return name;
        }

        // TODO : rercursive alias 
        public static void AddAlias(string name, string target)
        {
            if(name == target)
            {
                return;
            }
            string author = Sanitize(target);

            string nameFile = Sanitize(name);

            string json = File.ReadAllText($"{music_folder}/aliases.json");
            Dictionary<string, string> aliases = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            aliases.Add(nameFile, author);
            File.WriteAllTextAsync($"{music_folder}/aliases.json", JsonConvert.SerializeObject(aliases));
            if (Directory.Exists($"{music_folder}/{author}"))
            {
                foreach (string song in GetSongs($"{music_folder}/{nameFile}"))
                {
                    FileInfo fi = new FileInfo(song);
                    File.Move(song, $"{music_folder}/{author}/{fi.Name}");
                }
                foreach(string album in GetAlbums($"{music_folder}/{nameFile}"))
                {
                    string albumName = GetNameFromPath(album);
                    if (Directory.Exists($"{music_folder}/{author}/{albumName}"))
                    {
                        foreach (string song in GetSongs(album))
                        {
                            FileInfo fi = new FileInfo(song);
                            File.Move(song, $"{music_folder}/{author}/{albumName}/{fi.Name}");
                        }
                    }
                    else
                    {
                        Directory.Move(album, $"{music_folder}/{author}/{albumName}");
                    }
                }
                Directory.Delete($"{music_folder}/{nameFile}", true);
            }
            else
            {
                Directory.Move($"{music_folder}/{nameFile}", $"{music_folder}/{author}");
            }
            foreach (string song in GetSongs($"{music_folder}/{author}"))
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
            string json = File.ReadAllText($"{music_folder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            playlists.Add(name, new List<string>());
            File.WriteAllTextAsync($"{music_folder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        public static void AddToPlaylist(string name, string song)
        {
            string json = File.ReadAllText($"{music_folder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            playlists[name].Add(song);
            File.WriteAllTextAsync($"{music_folder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        public static void AddToPlaylist(string name, List<string> songs)
        {
            string json = File.ReadAllText($"{music_folder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            playlists[name].AddRange(songs);
            File.WriteAllTextAsync($"{music_folder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }


        ///<summary>
        ///Deletetes <paramref name="playlist"/> from <paramref name="song"/>
        ///</summary>
        public static void DeletePlaylist(string playlist, string song)
        {
            string json = File.ReadAllText($"{music_folder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            if (playlists.ContainsKey(playlist))
            {
                playlists[playlist].Remove(song);
                File.WriteAllTextAsync($"{music_folder}/playlists.json", JsonConvert.SerializeObject(playlists));
            }
        }

        ///<summary>
        ///Deletetes <paramref name="playlist"/>
        ///</summary>
        public static void DeletePlaylist(string playlist)
        {
            string json = File.ReadAllText($"{music_folder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            playlists.Remove(playlist);
            File.WriteAllTextAsync($"{music_folder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        public static string Sanitize(string value)
        {
            return value.Replace("/", "").Replace("|", "").Replace("\\", "").Replace(":", "").Replace("*", "").Replace("\"", "").Replace("#", "").Replace("?", "").Replace("<", "").Replace(">", "").Trim(' ');
        }

        public static int GetAvailableFile(string name = "video")
        {
            int i = 0;
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            while (File.Exists($"{path}/tmp/{name}{i}.mp3"))
            {
                i++;
            }
            string dest = $"{path}/tmp/{name}{i}.mp3";
            File.Create(dest).Close();
            return i;
        }

        ///<summary>
        ///Gets all playlist names
        ///</summary>
        public static List<string> GetPlaylist()
        {
            string json = File.ReadAllText($"{music_folder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            return playlists.Keys.ToList();
        }

        ///<summary>
        ///Gets all songs in <paramref name="playlist"/>
        ///<br>Returns <returns>empty List<string></returns> if <paramref name="playlist"/> doesn't exist</br>
        ///</summary>
        public static List<string> GetPlaylist(string playlist)
        {
            string json = File.ReadAllText($"{music_folder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            if (playlists.ContainsKey(playlist))
            {
                return playlists[playlist];
            }
            return new List<string>();
        }
        public static void AddSyncTarget(IPAddress host)
        {
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string json = File.ReadAllText($"{path}/sync_targets.json");
            Dictionary<IPAddress, List<string>> targets = JsonConvert.DeserializeObject<Dictionary<IPAddress, List<string>>>(json);
            targets.Add(host, GetSongs());
        }
        public static List<string> GetSyncSongs(IPAddress host)
        {
            string path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string json = File.ReadAllText($"{path}/sync_targets.json");
            Dictionary<IPAddress, List<string>> targets = JsonConvert.DeserializeObject<Dictionary<IPAddress, List<string>>>(json);
            if (targets.ContainsKey(host))
            {
                return targets[host];
            }
            return new List<string>();
        }
    }
}