using Android.App;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ass_Pain
{
    internal static class FileManager
    {
        private static readonly string Root = (string)Android.OS.Environment.ExternalStorageDirectory;
        private static readonly string Path = Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
        public static readonly string MusicFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic)?.AbsolutePath;
        //private static readonly string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string( System.IO.Path.GetInvalidFileNameChars() ) + new string( System.IO.Path.GetInvalidPathChars() )+"'`/|\\:*\"#?<>");
        //private static readonly string invalidRegStr = string.Format( @"([{0}]*\.+$)|([{0}]+)", invalidChars );
        private static readonly string invalidRegStr = string.Format( @"([{0}]*\.+$)|([{0}]+)", System.Text.RegularExpressions.Regex.Escape(new string( System.IO.Path.GetInvalidFileNameChars() ) + new string( System.IO.Path.GetInvalidPathChars() )+"'`/|\\:*\"#?<>") );
        public static void DiscoverFiles(string path = null)
        {
            //return;
            //Directory.CreateDirectory(music_folder);
            /*if (!File.Exists($"{music_folder}/.nomedia"))
            {
                File.Create($"{music_folder}/.nomedia").Close();
            }*/
            path ??= Root;

            string nameFromPath = GetNameFromPath(path);
            if(nameFromPath == "Android")
            {
                return;
            }
            if (nameFromPath.StartsWith("."))
            {
                return;
            }
            if (File.Exists($"{path}/.nomedia"))
            {
                return;
            }
            switch (nameFromPath)
            {
                case "sound_recorder":
                case "Notifications":
                case "Recordings":
                case "Ringtones":
                case "MIUI":
                case "Alarms":
                    return;
            }

            if (path == MusicFolder)
            {
                return;
            }
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                DiscoverFiles(dir);
            }
            
            foreach (string file in Directory.EnumerateFiles(path, "*.mp3"))
            {
                try
                {
                    Console.WriteLine($"Processing: {file}");
                    string title = GetSongTitle(file);
                    if (title == null)
                    {
                        SetSongTitle(file);
                        title = GetNameFromPath(file).Replace(".mp3", "");
                    }
                    if (title.Contains(".mp3"))
                    {
                        using TagLib.File tfile = TagLib.File.Create(file);
                        tfile.Tag.Title = tfile.Tag.Title.Replace(".mp3", "");
                        title = tfile.Tag.Title;
                        tfile.Save();
                    }
                    title = Sanitize(title);
                    string[] unsanitizedArtists = GetSongArtist(file);
                    string artist = unsanitizedArtists.Length > 0 ? Sanitize(GetAlias(unsanitizedArtists[0])) : "No Artist";
                    string uAlbum = GetSongAlbum(file);
                    Console.WriteLine("Moving " + file);
                    if (uAlbum != null)
                    {
                        string album = Sanitize(uAlbum);
                        Directory.CreateDirectory($"{MusicFolder}/{artist}/{album}");
                        File.Move(file, $"{MusicFolder}/{artist}/{album}/{title}.mp3");
                    }
                    else
                    {
                        Directory.CreateDirectory($"{MusicFolder}/{artist}");
                        File.Move(file, $"{MusicFolder}/{artist}/{title}.mp3");
                    }
                    
                    
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"error: {file}");
                    Console.WriteLine(ex);
                }
            }
            //GetSongTitle(GetSongs());
        }

        ///<summary>
        ///Creates virtual song topology in MainActivity.StateHandler
        ///</summary>
        public static void GenerateList(string path)
        {

            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                GenerateList(dir);
            }

            foreach (string file in Directory.EnumerateFiles(path, "*.mp3"))
            {
                //Console.WriteLine(file);
                using TagLib.File tfile = TagLib.File.Create(file);
                string[] artists = tfile.Tag.Performers.Length == 0 ? tfile.Tag.AlbumArtists : tfile.Tag.Performers;

                List<Artist> artistsList = new List<Artist>();
                if (artists.Length > 0)
                {
                    foreach (string artist in artists)
                    {
                        List<Artist> inList = MainActivity.stateHandler.Artists.Select(artist);
                        if (inList.Count > 0)
                        {
                            artistsList.Add(inList[0]);
                            continue;
                        }
                        string part = Sanitize(GetAlias(artist));
                        if(File.Exists($"{MusicFolder}/{part}/cover.jpg"))
                            artistsList.Add(new Artist(GetAlias(artist), $"{MusicFolder}/{part}/cover.jpg"));
                        else if(File.Exists($"{MusicFolder}/{part}/cover.png"))
                            artistsList.Add(new Artist(GetAlias(artist), $"{MusicFolder}/{part}/cover.png"));
                        else
                            artistsList.Add(new Artist(GetAlias(artist), "Default"));
                    }
                }
                else
                {
                    artistsList.Add(MainActivity.stateHandler.Artists.Select("No Artist")[0]);
                }
                
                Song song;

                if (!string.IsNullOrEmpty(tfile.Tag.Album))
                {
                    string artistPart = Sanitize(GetAlias(artistsList[0].Title));
                    string albumPart = Sanitize(tfile.Tag.Album);
                    Album album;

                    List<Album> inListAlbum = MainActivity.stateHandler.Albums.Select(tfile.Tag.Album);
                    if (inListAlbum.Count > 0)
                    {
                        album = inListAlbum[0];
                    }
                    else
                    {
                        if(File.Exists($"{MusicFolder}/{artistPart}/{albumPart}/cover.jpg"))
                            album = new Album(tfile.Tag.Album, $"{MusicFolder}/{artistPart}/{albumPart}/cover.jpg");
                        else if(File.Exists($"{MusicFolder}/{artistPart}/{albumPart}/cover.png"))
                            album = new Album(tfile.Tag.Album, $"{MusicFolder}/{artistPart}/{albumPart}/cover.png");
                        else
                            album = new Album(tfile.Tag.Album, "Default");
                    }
                    
                    song = new Song(artistsList, tfile.Tag.Title, File.GetCreationTime(file), file, album);
                    album.AddSong(ref song);
                    album.AddArtist(ref artistsList);
                    foreach (Artist artist in artistsList)
                    {
                        artist.AddAlbum(ref album);
                        artist.AddSong(ref song);
                    }
                    song.AddAlbum(ref album);
                    
                    if (inListAlbum.Count == 0)
                    {
                        MainActivity.stateHandler.Albums.Add(album);
                    }
                    
                }
                else
                {
                    List<Album> albums = new List<Album>();
                    artistsList.ForEach(artist =>
                    {
                        albums.Add(artist.Albums.Select("Uncategorized")[0]);
                    });
                    song = new Song(artistsList, tfile.Tag.Title, File.GetCreationTime(file), file, albums);
                    albums.ForEach(album => album.AddSong(ref song));
                    artistsList.ForEach(artist => artist.AddSong(ref song));
                    foreach (Artist artist in artistsList)
                    {
                        artist.AddSong(ref song);
                    }
                }
                song.AddArtist(ref artistsList);

                MainActivity.stateHandler.Songs.Add(song);
                foreach (Artist artist in artistsList.Where(artist => MainActivity.stateHandler.Artists.Select(artist.Title).Count == 0))
                {
                    MainActivity.stateHandler.Artists.Add(artist);
                }

                //MainActivity.stateHandler.Artists.AddRange(artistsList);
                tfile.Dispose();
            }
        }
        
        public static List<string> GetAuthors()
        {
            return Directory.EnumerateDirectories(MusicFolder).Where(author => !GetNameFromPath(author).StartsWith(".")).ToList();
            /*List<string> authors = new List<string>();
            foreach (string author in Directory.EnumerateDirectories(music_folder))
            {
                Console.WriteLine(author);
                authors.Add(author);
            }
            return authors;*/
        }

        ///<summary>
        ///Gets all albums of all authors
        ///</summary>
        public static List<string> GetAlbums()
        {
            List<string> albums = new List<string>();

            foreach (string author in Directory.EnumerateDirectories(MusicFolder))
            {
                albums.AddRange(Directory.EnumerateDirectories(author));
                /*foreach (string album in Directory.EnumerateDirectories(author))
                {
                    Console.WriteLine(album);
                    albums.Add(album);
                }*/
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
                foreach (string playlistName in GetPlaylist())
                {
                    foreach(string file in GetSongs(path))
                    {
                        DeletePlaylist(playlistName, file);
                    }
                }
                Directory.Delete(path, true);
            }
            else
            {
                File.Delete(path);
                foreach (string playlistName in GetPlaylist())
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
            /*List<string> albums = new List<string>();
            foreach (string album in Directory.EnumerateDirectories(author))
            {
                Console.WriteLine(album);
                albums.Add(album);
            }
            return albums;*/
            return Directory.EnumerateDirectories(author).ToList();
        }

        ///<summary>
        ///Gets all songs in device
        ///</summary>
        public static List<string> GetSongs()
        {
            /*foreach (var song in MainActivity.stateHandler.Songs)
            {
                Console.WriteLine(song);
            }*/
            return Directory.EnumerateFiles(MusicFolder, "*.mp3", SearchOption.AllDirectories).ToList();
        }

        ///<summary>
        ///Gets all songs in album or all album-less songs for author
        ///</summary>
        public static List<string> GetSongs(string path)
        {
            /*var mp3Files = Directory.EnumerateFiles(path, "*.mp3");
            List<string> songs = new List<string>();
            foreach (string currentFile in mp3Files)
            {
                Console.WriteLine(currentFile);
                songs.Add(currentFile);
            }
            return songs;*/
            return Directory.EnumerateFiles(path, "*.mp3").ToList();
        }

        public static void SetSongTitle(string file)
        {
            if (!File.Exists(file)) return;
            using TagLib.File tfile = TagLib.File.Create(file);
            tfile.Tag.Title = GetNameFromPath(file).Replace(".mp3", "");
            tfile.Save();

        }

        public static string GetSongTitle(string path)
        {
            if (!File.Exists(path)) return "cant get title";
            using TagLib.File tfile = TagLib.File.Create(path);
            return tfile.Tag.Title;
        }

        public static List<string> GetSongTitle(List<string> files)
        {
            List<string> titles = new List<string>();
            foreach (string currentFile in files)
            {
                using TagLib.File tfile = TagLib.File.Create(currentFile);
                titles.Add(tfile.Tag.Title);
            }
            return titles;
        }

        public static string GetSongAlbum(string path)
        {
            if (!File.Exists(path)) return "cant get album";
            using TagLib.File tfile = TagLib.File.Create(path);
            return tfile.Tag.Album;

        }
        public static string[] GetSongArtist(string path)
        {
            if (!File.Exists(path)) return new[] { "cant get artist" };
            using TagLib.File tfile = TagLib.File.Create(path);
            return tfile.Tag.Performers.Length == 0 ? tfile.Tag.AlbumArtists : tfile.Tag.Performers;
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
            if (path == string.Empty)
                return (string.Empty, string.Empty);
            if (IsDirectory(path))
            {
                return (GetNameFromPath(path), GetNameFromPath(System.IO.Path.GetDirectoryName(path)));
            }

            try
            {
                return (GetNameFromPath(System.IO.Path.GetDirectoryName(path)), GetNameFromPath(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(path))));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ("", "");
            }
        }

        public static bool IsDirectory(string path)
        {
            return string.IsNullOrEmpty(System.IO.Path.GetFileName(path)) || Directory.Exists(path);
        }

        public static string GetAlias(string name)
        {
            while (true)
            {
                string json = File.ReadAllText($"{MusicFolder}/aliases.json");
                Dictionary<string, string> aliases = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (aliases.TryGetValue(name, out string alias))
                {
                    name = alias;
                    continue;
                }

                if (!aliases.TryGetValue(Sanitize(name), out alias)) return name;
                name = alias;
            }
        }

        // TODO : recursive alias 
        public static void AddAlias(string name, string target)
        {
            if(name == target)
            {
                return;
            }
            string author = Sanitize(target);

            string nameFile = Sanitize(name);

            string json = File.ReadAllText($"{MusicFolder}/aliases.json");
            Dictionary<string, string> aliases = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            aliases.Add(nameFile, author);
            File.WriteAllTextAsync($"{MusicFolder}/aliases.json", JsonConvert.SerializeObject(aliases));
            if (Directory.Exists($"{MusicFolder}/{author}"))
            {
                foreach (string song in GetSongs($"{MusicFolder}/{nameFile}"))
                {
                    FileInfo fi = new FileInfo(song);
                    File.Move(song, $"{MusicFolder}/{author}/{fi.Name}");
                }
                foreach(string album in GetAlbums($"{MusicFolder}/{nameFile}"))
                {
                    string albumName = GetNameFromPath(album);
                    if (Directory.Exists($"{MusicFolder}/{author}/{albumName}"))
                    {
                        foreach (string song in GetSongs(album))
                        {
                            FileInfo fi = new FileInfo(song);
                            File.Move(song, $"{MusicFolder}/{author}/{albumName}/{fi.Name}");
                        }
                    }
                    else
                    {
                        Directory.Move(album, $"{MusicFolder}/{author}/{albumName}");
                    }
                }
                Directory.Delete($"{MusicFolder}/{nameFile}", true);
            }
            else
            {
                Directory.Move($"{MusicFolder}/{nameFile}", $"{MusicFolder}/{author}");
            }
            foreach (string song in GetSongs($"{MusicFolder}/{author}"))
            {
                using TagLib.File tfile = TagLib.File.Create($"{song}");
                string[] authors = tfile.Tag.Performers;
                for (int i = 0; i < authors.Length; i++)
                {
                    if (authors[i] == name)
                    {
                        authors[i] = target;
                    }
                    else
                    {
                        //TODO: add symlink move
                        if (i == 0)
                        {
                        }
                        //Android.Systems.Os.Symlink();
                    }
                }
                tfile.Tag.Performers = authors;
                tfile.Save();
            }
        }

        public static void CreatePlaylist(string name)
        {
            string json = File.ReadAllText($"{MusicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            playlists.Add(name, new List<string>());
            File.WriteAllTextAsync($"{MusicFolder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        public static void AddToPlaylist(string name, string song)
        {
            string json = File.ReadAllText($"{MusicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            playlists[name].Add(song);
            File.WriteAllTextAsync($"{MusicFolder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        public static void AddToPlaylist(string name, List<string> songs)
        {
            string json = File.ReadAllText($"{MusicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            playlists[name].AddRange(songs);
            File.WriteAllTextAsync($"{MusicFolder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }


        ///<summary>
        ///Deletes <paramref name="song"/> from <paramref name="playlist"/>
        ///</summary>
        public static void DeletePlaylist(string playlist, string song)
        {
            string json = File.ReadAllText($"{MusicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            if (!playlists.TryGetValue(playlist, out List<string> playlist1)) return;
            playlist1.Remove(song);
            File.WriteAllTextAsync($"{MusicFolder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        ///<summary>
        ///Deletes <paramref name="playlist"/>
        ///</summary>
        public static void DeletePlaylist(string playlist)
        {
            string json = File.ReadAllText($"{MusicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            playlists.Remove(playlist);
            File.WriteAllTextAsync($"{MusicFolder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        public static string Sanitize(string value)
        {
            value = System.Text.RegularExpressions.Regex.Replace( value, invalidRegStr, "" );
            return System.Text.RegularExpressions.Regex.Replace(value, @"\s+|_{2,}", "_").Trim().Replace("_-_", "_").Replace(",_", ",");
            //return value.Replace("/", "").Replace("|", "").Replace("\\", "").Replace(":", "").Replace("*", "").Replace("\"", "").Replace("#", "").Replace("?", "").Replace("<", "").Replace(">", "").Trim().Replace(" ", "_");
        }

        public static int GetAvailableFile(string name = "video")
        {
            int i = 0;
            while (File.Exists($"{Path}/tmp/{name}{i}.mp3"))
            {
                i++;
            }
            string dest = $"{Path}/tmp/{name}{i}.mp3";
            File.Create(dest).Close();
            return i;
        }

        public static void GetPlaceholderFile(string writePath, string name, string extension)
        {
            File.Create($"{writePath}/{name}.{extension}").Close();
        }

        ///<summary>
        ///Gets all playlist names
        ///</summary>
        public static List<string> GetPlaylist()
        {
            string json = File.ReadAllText($"{MusicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            return playlists.Keys.ToList();
        }

        ///<summary>
        ///Gets all songs in <paramref name="playlist"/>
        ///<br>Returns <returns>empty List of strings</returns> if <paramref name="playlist"/> doesn't exist</br>
        ///</summary>
        public static List<Song> GetPlaylist(string playlist)
        {
            string json = File.ReadAllText($"{MusicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            if (playlists.TryGetValue(playlist, out List<string> playlist1))
            {
                List<Song> x = new List<Song>();
                foreach (var song in playlist1)
                {
                    var y = MainActivity.stateHandler.Songs.Where(a => a.Path == song);
                    if (y.Any())
                    {
                        x.AddRange(y);
                    }
                    else
                    {
                        DeletePlaylist(playlist, song);
                    }
                }
                return x;
            }
            return new List<Song>();
        }
        public static void AddSyncTarget(string host)
        {
            
            string json = File.ReadAllText($"{Path}/sync_targets.json");
            Dictionary<string, List<string>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            targets.Add(host, GetSongs());
            File.WriteAllTextAsync($"{Path}/sync_targets.json", JsonConvert.SerializeObject(targets));
        }

        public static void AddTrustedHost(string host)
        {

            string json = File.ReadAllText($"{Path}/trusted_hosts.json");
            List<string> hosts = JsonConvert.DeserializeObject<List<string>>(json);
            hosts.Add(host);
            File.WriteAllTextAsync($"{Path}/trusted_hosts.json", JsonConvert.SerializeObject(hosts));
        }

        public static bool GetTrustedHost(string host)
        {

            string json = File.ReadAllText($"{Path}/trusted_hosts.json");
            List<string> hosts = JsonConvert.DeserializeObject<List<string>>(json);
            return hosts.Contains(host);
        }

        public static (bool, List<string>) GetSyncSongs(string host)
        {
            string json = File.ReadAllText($"{Path}/sync_targets.json");
            Dictionary<string, List<string>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            return targets.TryGetValue(host, out List<string> target) ? (true, target) : (false, null);
        }
    }
}