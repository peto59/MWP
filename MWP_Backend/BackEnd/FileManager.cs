using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

using MWP_Backend.BackEnd;
using MWP.BackEnd.Helpers;
using MWP_Backend.DatatypesAndExtensions;
using MWP.DatatypesAndExtensions;
using MWP.UIBinding;
using Newtonsoft.Json;
using TagLib;

#if ANDROID
using Android.App;
using Android.Views;
using Google.Android.Material.Snackbar;
using Xamarin.Essentials;
#elif LINUX

#endif
using File = System.IO.File;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd
{
    internal static class FileManager
    {
#if ANDROID
            private static readonly string? _root = (string?)Android.OS.Environment.ExternalStorageDirectory;
#elif LINUX
        private static readonly string? _root = "/";
#endif
        
        public static string Root => _root ?? string.Empty;
        // ReSharper disable once InconsistentNaming
#if ANDROID
        private static readonly string? _privatePath = MediaTypeNames.Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
#elif LINUX
        private static readonly string? _privatePath = "~/.config/MWP/";
#endif
        
        public static string PrivatePath => _privatePath ?? string.Empty;
        // ReSharper disable once InconsistentNaming
#if ANDROID
        private static readonly string? _musicFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic)?.AbsolutePath;
#elif LINUX
        private static readonly string? _musicFolder = "~/Music";
#endif
        public static string MusicFolder => _musicFolder ?? string.Empty;
        //private static readonly string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string( System.IO.Path.GetInvalidFileNameChars() ) + new string( System.IO.Path.GetInvalidPathChars() )+"'`/|\\:*\"#?<>");
        //private static readonly string invalidRegStr = string.Format( @"([{0}]*\.+$)|([{0}]+)", invalidChars );
        private static readonly string InvalidRegStr = string.Format( @"([{0}]*\.+$)|([{0}]+)", System.Text.RegularExpressions.Regex.Escape(new string( Path.GetInvalidFileNameChars() ) + new string( Path.GetInvalidPathChars() )+"'`/|\\:*\"#?<>") );
        private static HashSet<string>? _chromaprintUsedSongs;
        private static readonly List<Task> Discoveries = new List<Task>();

        public static void EarlyInnit()
        {
            if (!Directory.Exists($"{_privatePath}/tmp"))
            {
#if DEBUG
                MyConsole.WriteLine("Creating " + $"{_privatePath}/tmp");
#endif
                Directory.CreateDirectory($"{_privatePath}/tmp");
            }
            
            //File.Delete($"{FileManager.PrivatePath}/trusted_sync_targets.json");
            if (!File.Exists($"{_privatePath}/trusted_sync_targets.json"))
            {
                File.WriteAllText($"{_privatePath}/trusted_sync_targets.json", JsonConvert.SerializeObject(new Dictionary<string, List<Song>>()));
            }
            
            if (!File.Exists($"{_privatePath}/trusted_SSIDs.json"))
            {
                File.WriteAllText($"{_privatePath}/trusted_SSIDs.json", JsonConvert.SerializeObject(new List<string>()));
            }
        }

        public static void LateInnit()
        {
            if (!Directory.Exists(_musicFolder))
            {
#if DEBUG
                MyConsole.WriteLine("Creating " + $"{_musicFolder}");
#endif
                if (_musicFolder != null) Directory.CreateDirectory(_musicFolder);
            }
            
            if (!File.Exists($"{_musicFolder}/usedChromaprintSongs.json"))
            {
                File.WriteAllText($"{_musicFolder}/usedChromaprintSongs.json", JsonConvert.SerializeObject(new HashSet<string>()));
                _chromaprintUsedSongs = new HashSet<string>();
            }
            else
            {
                _chromaprintUsedSongs = GetHashSet();
            }
#if DEBUG
            MyConsole.WriteLine($"Hashset length {_chromaprintUsedSongs.Count}");
#endif

            if (!File.Exists($"{_musicFolder}/aliases.json"))
            {
                File.WriteAllTextAsync($"{_musicFolder}/aliases.json", JsonConvert.SerializeObject(new Dictionary<string, string>()));

            }

            if (!File.Exists($"{_musicFolder}/playlists.json"))
            {
                SongJsonConverter customConverter = new SongJsonConverter(true);
                File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(new Dictionary<string, List<string>>(), customConverter));
            }
            
            DirectoryInfo di = new DirectoryInfo($"{_privatePath}/tmp/");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
#if DEBUG
                MyConsole.WriteLine($"Deleting {file}");
#endif
            }
        }

        /// <summary>
        /// Recreates virtual song topology in MainActivity.StateHandler
        /// </summary>
        public static void ReDiscoverFiles()
        {
            StateHandler.Songs.Clear();
            StateHandler.Artists.Clear();
            StateHandler.Albums.Clear();
                    
            StateHandler.Artists.Add(new Artist("No Artist", "Default"));
            
            DiscoverFiles(true);
        }
        
        /// <summary>
        /// Creates virtual song topology in MainActivity.StateHandler and allocates all new files
        /// </summary>
        public static void DiscoverFiles(bool generateStateHandlerEntry = false)
        {
            StateHandler.FileListGenerationEvent.WaitOne();
            if (_root != null) DiscoverFiles(_root, generateStateHandlerEntry);
            while (Discoveries.Any())
            {
                Task finishedTask = Task.WhenAny(Discoveries).GetAwaiter().GetResult();
                Discoveries.Remove(finishedTask);
            }
            StateHandler.FileListGenerationEvent.Set();
            StateHandler.FileListGenerated.Set();
        }

        private static void DiscoverFiles(string path, bool generateStateHandlerEntry)
        {
#if DEBUG
            MyConsole.WriteLine($"Path {path}");
#endif
            string nameFromPath = GetNameFromPath(path);
            if(nameFromPath.StartsWith(".") || File.Exists($"{path}/.nomedia"))
            {
                return;
            }
            if (generateStateHandlerEntry && path == _musicFolder)
            {
                return;
            }

            if (SettingsManager.ExcludedPaths.Contains(path))
            {
                return;
            }
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                DiscoverFiles(dir, generateStateHandlerEntry);
            }
            
            foreach (string file in Directory.EnumerateFiles(path, "*.mp3"))
            {
                try
                {
                    Task x = new Task(() =>
                    {
                        _ = AddSong(file, _musicFolder != null && !file.Contains(_musicFolder),
                            generateStateHandlerEntry || (_musicFolder != null && !file.Contains(_musicFolder)));
                    });
                    Discoveries.Add(x);
                    
                }
                catch (Exception ex)
                {
#if DEBUG
                    MyConsole.WriteLine($"error: {file}");
                    MyConsole.WriteLine(ex);
#endif
                    // ignored
                }
            }
        }

        ///<summary>
        ///Creates virtual song topology in MainActivity.StateHandler
        ///</summary>
        public static void GenerateList()
        {
            GenerateList(MusicFolder, true);
        }

        ///<summary>
        ///Creates virtual song topology in MainActivity.StateHandler
        ///</summary>
        private static void GenerateList(string path, bool first)
        {

            if (first)
            {
                StateHandler.FileListGenerationEvent.WaitOne();
            }
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                GenerateList(dir, false);
            }

            foreach (string file in Directory.EnumerateFiles(path, "*.mp3"))
            {
                AddSong(file);
            }

            if (first)
            {
                StateHandler.FileListGenerationEvent.Set();
            }
        }
        

        ///<summary>
        /// Deletes file on <paramref name="path"/>
        ///</summary>
        public static void Delete(string path)
        {
            if (IsDirectory(path))
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            else
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
        
        ///<summary>
        ///Gets all songs in device
        ///</summary>
        public static int GetSongsCount()
        {
            return Directory.EnumerateFiles(_musicFolder ?? string.Empty, "*.mp3", SearchOption.AllDirectories).Count();
        }

        ///<summary>
        ///Gets last name/folder from <paramref name="path"/>
        ///</summary>
        private static string GetNameFromPath(string path)
        {
            string[] subs = path.Split('/');
            return subs[^1];
        }

        private static bool IsDirectory(string path)
        {
            return string.IsNullOrEmpty(Path.GetFileName(path)) || Directory.Exists(path);
        }

        public static string GetAlias(string name)
        {
            string json = File.ReadAllText($"{_musicFolder}/aliases.json");
            Dictionary<string, string> aliases = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            while (true)
            {
                if (aliases.TryGetValue(name, out string alias))
                {
                    name = alias;
                    continue;
                }

                if (!aliases.TryGetValue(Sanitize(name), out alias)) return name;
                name = alias;
            }
        }
        
        /*[Obsolete]
        public static void AddAliasObsolete(string originalName, string newAlias)
        {
            if(originalName == newAlias)
            {
                return;
            }
            string author = Sanitize(newAlias);

            string nameFile = Sanitize(originalName);

            string json = File.ReadAllText($"{_musicFolder}/aliases.json");
            Dictionary<string, string> aliases = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            aliases.Add(nameFile, author);
            File.WriteAllTextAsync($"{_musicFolder}/aliases.json", JsonConvert.SerializeObject(aliases));
            if (Directory.Exists($"{_musicFolder}/{author}"))
            {
                foreach (string song in GetSongs($"{_musicFolder}/{nameFile}"))
                {
                    FileInfo fi = new FileInfo(song);
                    File.Move(song, $"{_musicFolder}/{author}/{fi.Name}");
                }
                foreach(string album in GetAlbums($"{_musicFolder}/{nameFile}"))
                {
                    string albumName = GetNameFromPath(album);
                    if (Directory.Exists($"{_musicFolder}/{author}/{albumName}"))
                    {
                        foreach (string song in GetSongs(album))
                        {
                            FileInfo fi = new FileInfo(song);
                            File.Move(song, $"{_musicFolder}/{author}/{albumName}/{fi.Name}");
                        }
                    }
                    else
                    {
                        Directory.Move(album, $"{_musicFolder}/{author}/{albumName}");
                    }
                }
                Directory.Delete($"{_musicFolder}/{nameFile}", true);
            }
            else
            {
                Directory.Move($"{_musicFolder}/{nameFile}", $"{_musicFolder}/{author}");
            }
            foreach (string song in GetSongs($"{_musicFolder}/{author}"))
            {
                using TagLib.File tfile = TagLib.File.Create($"{song}");
                string[] authors = tfile.Tag.Performers;
                for (int i = 0; i < authors.Length; i++)
                {
                    if (authors[i] == originalName)
                    {
                        authors[i] = newAlias;
                    }
                    else
                    {
                        if (i == 0)
                        {
                        }
                        //Android.Systems.Os.Symlink();
                    }
                }
                tfile.Tag.Performers = authors;
                tfile.Save();
            }
        }*/
        
        ///<summary>
        ///Gets all playlist names
        ///</summary>
        public static List<string> GetPlaylist()
        {
            try
            {
                string json = File.ReadAllText($"{_musicFolder}/playlists.json");
                SongJsonConverter customConverter = new SongJsonConverter(true);
                Dictionary<string, List<Song>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
                return playlists.Keys.ToList();
            }
            catch (Exception ex)
            {
#if DEBUG
                MyConsole.WriteLine(ex);
#endif
                return new List<string>();
            }
        }

        ///<summary>
        ///Gets all <see cref="Song"/>s in <paramref name="playlist"/>
        ///</summary>
        ///<param name="playlist">Name of playlist from which you want to get songs</param>
        ///<returns>
        ///<see cref="List{Song}"/> of <see cref="Song"/>s in <paramref name="playlist"/> or empty <see cref="List{Song}"/> of <see cref="Song"/>s if <paramref name="playlist"/> doesn't exist
        ///</returns>
        public static List<Song> GetPlaylist(string playlist)
        {
            string json = File.ReadAllText($"{_musicFolder}/playlists.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            if (!playlists.TryGetValue(playlist, out List<Song> playlist1))
            {
               return new List<Song>();
            }
            foreach (Song song in playlist1)
            {
                song.AddPlaylist(playlist);
            }

            return playlist1;
        }

        public static void CreatePlaylist(string name)
        {
            string json = File.ReadAllText($"{_musicFolder}/playlists.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            playlists.Add(name, new List<Song>());
            File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(playlists, customConverter));
        }

        public static void AddToPlaylist(string name, Song song)
        {
            string json = File.ReadAllText($"{_musicFolder}/playlists.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            playlists[name].Add(song);
            File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(playlists, customConverter));
        }

        public static void AddToPlaylist(string name, IEnumerable<Song> songs)
        {
            string json = File.ReadAllText($"{_musicFolder}/playlists.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            playlists[name].AddRange(songs);
            File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(playlists, customConverter));
        }


        ///<summary>
        ///Deletes <paramref name="song"/> from <paramref name="playlist"/>
        ///</summary>
        public static void DeletePlaylist(string playlist, Song song)
        {
            string json = File.ReadAllText($"{_musicFolder}/playlists.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            if (!playlists.TryGetValue(playlist, out List<Song> playlist1)) return;
            playlist1.Remove(song);
            File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(playlists, customConverter));
        }

        ///<summary>
        ///Deletes <paramref name="playlist"/>
        ///</summary>
        public static void DeletePlaylist(string playlist)
        {
            string json = File.ReadAllText($"{_musicFolder}/playlists.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            playlists.Remove(playlist);
            File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(playlists, customConverter));
        }

        public static string Sanitize(string value)
        {
            value = System.Text.RegularExpressions.Regex.Replace( value.Trim(), InvalidRegStr, string.Empty);
            value = System.Text.RegularExpressions.Regex.Replace(value, @"\s+", "_").Replace(' ', '_');
            value = System.Text.RegularExpressions.Regex.Replace(value, @"_{2,}", "_").Replace("_-_", "-").Replace(",_", ",");
            return value;
            //return value.Replace("/", "").Replace("|", "").Replace("\\", "").Replace(":", "").Replace("*", "").Replace("\"", "").Replace("#", "").Replace("?", "").Replace("<", "").Replace(">", "").Trim().Replace(" ", "_");
        }

        //public static string GetAvailableTempFile(string name = "video", string extension = "mp3")
        public static string GetAvailableTempFile(string name, string extension)
        {
            return GetAvailableFile($"{_privatePath}/tmp", name, extension);
        }

        public static (string songTempPath, string unprocessedTempPath, string thumbnailTempPath) GetAvailableDownloaderFiles()
        {
            return (GetAvailableTempFile("video", "mp3"), GetAvailableTempFile("unprocessed", "mp3"), GetAvailableTempFile("thumbnail", "jpg"));
        }

        private static string GetAvailableFile(string path, string name, string extension)
        {
            int i = 0;
            while (File.Exists($"{path}/{name}{i}.{extension.TrimStart('.')}"))
            {
                i++;
            }
            string dest = $"{path}/{name}{i}.{extension.TrimStart('.')}";
            File.Create(dest).Close();
            return dest;
        }

        public static void GetPlaceholderFile(string writePath, string name, string extension)
        {
            File.Create($"{writePath}/{name}.{extension}").Close();
        }

        public static void AddTrustedSyncTarget(string host)
        {

            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> hosts =
                JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            hosts.Add(host, StateHandler.Songs);
            File.WriteAllText($"{_privatePath}/trusted_sync_targets.json", JsonConvert.SerializeObject(hosts, customConverter));
        }
        
        public static void DeleteTrustedSyncTarget(string host)
        {
            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> hosts =
                JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            hosts.Remove(host);
            File.WriteAllText($"{_privatePath}/trusted_sync_targets.json", JsonConvert.SerializeObject(hosts, customConverter));
#if ANDROID
            SecureStorage.Remove($"{host}_privkey");
            SecureStorage.Remove($"{host}_pubkey");
#elif LINUX
#endif
        }

        public static bool IsTrustedSyncTarget(string host)
        {
            try
            {
                string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
                SongJsonConverter customConverter = new SongJsonConverter(true);
                Dictionary<string, List<Song>> hosts =
                    JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
                return hosts.ContainsKey(host);
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                return false;
            }
        }

        public static List<Song> GetTrustedSyncTargetSongs(string host)
        {
            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            return targets.TryGetValue(host, out List<Song> target) ? target : new List<Song>();
        }
        
        public static void SetTrustedSyncTargetSongs(string host, List<Song> songs)
        {
            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            if (targets.ContainsKey(host))
            {
                targets[host] = songs;
                File.WriteAllText($"{_privatePath}/trusted_sync_targets.json", JsonConvert.SerializeObject(targets, customConverter));
            }
        }
        
        /// <summary>
        /// Adds song to all hosts except <paramref name="host"/>
        /// </summary>
        /// <param name="host"></param>
        /// <param name="song"></param>
        private static void AddTrustedSyncTargetSongsExcluded(string? host, Song song)
        {
            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            
            foreach (string key in targets.Keys.Where(key => key != host))
            {
                targets[key].Add(song);
            }
            File.WriteAllText($"{_privatePath}/trusted_sync_targets.json", JsonConvert.SerializeObject(targets, customConverter));
        }
        
        public static void AddTrustedSyncTargetSongs(string host, Song song)
        {
            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            if (targets.ContainsKey(host))
            {
                targets[host].Add(song);
                File.WriteAllText($"{_privatePath}/trusted_sync_targets.json", JsonConvert.SerializeObject(targets, customConverter));
            }
        }
        
        public static void AddTrustedSyncTargetSongs(string host, IEnumerable<Song> song)
        {
            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            if (targets.ContainsKey(host))
            {
                targets[host].AddRange(song);
                File.WriteAllText($"{_privatePath}/trusted_sync_targets.json", JsonConvert.SerializeObject(targets, customConverter));
            }
        }
        
        public static void DeleteTrustedSyncTargetSongs(string host, Song song)
        {
            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            if (targets.ContainsKey(host))
            {
                targets[host].Remove(song);
                File.WriteAllText($"{_privatePath}/trusted_sync_targets.json", JsonConvert.SerializeObject(targets, customConverter));
            }
        }
        
        public static void DeleteTrustedSyncTargetSongs(string host, IEnumerable<Song> songs)
        {
            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            if (targets.ContainsKey(host))
            {
                targets[host].RemoveAll(s => songs.Any(song => song.Equals(s)));
                File.WriteAllText($"{_privatePath}/trusted_sync_targets.json", JsonConvert.SerializeObject(targets, customConverter));
            }
        }
        
        public static List<string> GetTrustedSyncTargets()
        {
            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            return targets.Keys.ToList();
        }

        private static void AddSong(string path, string title, IReadOnlyList<string> artists, string? album = null, bool generateStateHandlerEntry = true, bool isNew = false, string? remoteHostname = null)
        {
            List<Artist> artistList = new List<Artist>();
            foreach (string artist in artists)
            {
                Artist artistObj;
                List<Artist> inArtistList = StateHandler.Artists.Select(GetAlias(artist));
                if (inArtistList.Any())
                {
                    artistObj = inArtistList[0];
                }else
                {
                    string artistAlias = GetAlias(artist);
                    string artistPath = Sanitize(artist);
                    if(File.Exists($"{_musicFolder}/{artistPath}/cover.jpg"))
                        artistObj = new Artist(artistAlias, $"{_musicFolder}/{artistPath}/cover.jpg");
                    else if(File.Exists($"{_musicFolder}/{artistPath}/cover.png"))
                        artistObj = new Artist(artistAlias, $"{_musicFolder}/{artistPath}/cover.png");
                    else
                        artistObj = new Artist(artistAlias, "Default");
                    if (generateStateHandlerEntry)
                    {
                        StateHandler.Artists.Add(artistObj);
                    }
                }
                artistList.Add(artistObj);
            }
            
            Song song = new Song(artistList, title, File.GetCreationTime(path), path);
            artistList.ForEach(art => art.AddSong(ref song));
            if (generateStateHandlerEntry)
            {
                StateHandler.Songs.Insert(0, song);
            }

            Album albumObj = new Album(string.Empty, string.Empty, false, false);
            if (!string.IsNullOrEmpty(album))
            {
                if (album != null)
                {
                    List<Album> inAlbumList = StateHandler.Albums.Select(album);
                    if (inAlbumList.Any())
                    {
                        albumObj = inAlbumList[0];
                        albumObj.AddSong(ref song);
                        albumObj.AddArtist(ref artistList);
                    }
                    else
                    {
                        string albumPath = Sanitize(album);
                        string artistPath = Sanitize(artists[0]);
                        if(File.Exists($"{_musicFolder}/{artistPath}/{albumPath}/cover.jpg"))
                            albumObj = new Album(album, song, artistList, $"{_musicFolder}/{artistPath}/{albumPath}/cover.jpg");
                        else if(File.Exists($"{_musicFolder}/{artistPath}/{albumPath}/cover.png"))
                            albumObj = new Album(album, song, artistList, $"{_musicFolder}/{artistPath}/{albumPath}/cover.png");
                        else
                            albumObj = new Album(album, song, artistList, "Default");
                        if (generateStateHandlerEntry)
                        {
                            StateHandler.Albums.Add(albumObj);
                        }
                    }
                }

                song.AddAlbum(ref albumObj);
                artistList.ForEach(art => art.AddAlbum(ref albumObj));
            }
            else
            {
                List<Album> albumList = artistList.SelectMany(art => art.Albums.Where(alb => alb.Title == "Uncategorized")).ToList();
                albumList.ForEach(alb => alb.AddSong(ref song));
            }

            if (isNew)
            {
                AddTrustedSyncTargetSongsExcluded(remoteHostname, song);
            }
        }

        public static (List<string> missingArtists, (string album, string artistPath) missingAlbum) AddSong(string path, bool isNew = false, bool generateStateHandlerEntry = true, string? remoteHostname = null)
        {
            using TagLib.File tfile = TagLib.File.Create(path, ReadStyle.PictureLazy);
            bool useChromaprint = false;
            bool chromaprintAllowed =
                (SettingsManager.ShouldUseChromaprintAtDiscover == UseChromaprint.Manual ||
                 SettingsManager.ShouldUseChromaprintAtDiscover == UseChromaprint.Automatic) &&
                string.IsNullOrEmpty(remoteHostname);
            string title;
            if (!string.IsNullOrEmpty(tfile.Tag.Title))
            {
                title = tfile.Tag.Title;
                if (title.EndsWith(".mp3"))
                {
                    tfile.Tag.Title = tfile.Tag.Title.Replace(".mp3", "");
                    title = tfile.Tag.Title;
                    tfile.Save();
                }
            }
            else if (chromaprintAllowed)
            {
                useChromaprint = true;
                title = Path.GetFileName(path).Replace(".mp3", "");
            }
            else
            {
                title = Path.GetFileName(path).Replace(".mp3", "");
                tfile.Tag.Title = title;
                tfile.Save();
            }

            string[] artists;
            if (tfile.Tag.Performers.Any())
            {
                artists = tfile.Tag.Performers;
            }
            else if (tfile.Tag.AlbumArtists.Any())
            {
                artists = tfile.Tag.AlbumArtists;
            }
            else if (chromaprintAllowed)
            {
                useChromaprint = true;
                artists = new[] { "No Artist" };
            }
            else
            {
                artists = new[] { "No Artist" };
            }
            artists = artists.Distinct().ToArray();



            string? album = tfile.Tag.Album;
            if (chromaprintAllowed && string.IsNullOrEmpty(album))
            {
                useChromaprint = true;
            }
            
            if (_chromaprintUsedSongs != null && chromaprintAllowed && useChromaprint && !_chromaprintUsedSongs.Contains(path))
            {
#if ANDROID
#if DEBUG
                MyConsole.WriteLine($"Using chromaprint for {path}");
#endif
                (string title, string recordingId, string trackId, List<(string title, string id)> artist,
                    List<(string title, string id)> releaseGroup, byte[]? thumbnail) result = Chromaprint.Search(
                            path,
                            artists[0],
                            title,
                            album,
                            SettingsManager.ShouldUseChromaprintAtDiscover == UseChromaprint.Manual
                        )
                        .GetAwaiter()
                        .GetResult();
#if DEBUG
                MyConsole.WriteLine($"Finished chromaprint for {path}");
#endif
                title = result.title;
                artists = result.artist.Select(a => a.title).ToArray();
                album = null;
                if (result.releaseGroup.Count > 0 && !string.IsNullOrEmpty(result.releaseGroup[0].title))
                {
                    album = result.releaseGroup[0].title;
                    if (!string.IsNullOrEmpty(result.releaseGroup[0].id))
                    {
                        tfile.Tag.MusicBrainzReleaseGroupId = result.releaseGroup[0].id;
                    }
                }
                tfile.Tag.Title = title;
                tfile.Tag.Performers = artists;
                tfile.Tag.AlbumArtists = artists;
                tfile.Tag.Album = album;
                string tagMusicBrainzArtistId = result.artist.First().id;
                if (string.IsNullOrEmpty(tagMusicBrainzArtistId)) tfile.Tag.MusicBrainzArtistId = tagMusicBrainzArtistId;
                if (string.IsNullOrEmpty(result.recordingId)) tfile.Tag.MusicBrainzReleaseId = result.recordingId;
                if (string.IsNullOrEmpty(result.trackId)) tfile.Tag.MusicBrainzTrackId = result.trackId;
                if (result.thumbnail != null)
                {
                    IPicture[] pics = new IPicture[1];
                    pics[0] = new Picture(result.thumbnail);
                    tfile.Tag.Pictures = pics;
                }
                tfile.Save();
                if (_chromaprintUsedSongs.Add(path))
                {
                    SaveHashSet();
                }
#endif
            }
            
            if (isNew && SettingsManager.MoveFiles == MoveFilesEnum.Yes)
            {
                string output = $"{_musicFolder}/{Sanitize(artists[0])}";
                if (!string.IsNullOrEmpty(album) && album != null)
                {
                    output = $"{output}/{Sanitize(album)}";
                }
                Directory.CreateDirectory(output);
                output = $"{output}/{Sanitize(title)}.mp3";
                try
                {
#if DEBUG
                    MyConsole.WriteLine("Moving " + path);
#endif
                    File.Move(path, output);
                }
                catch (IOException ioe)
                {
                    try
                    {
                        int newBitrate = tfile.Properties.AudioBitrate;
                        using TagLib.File tfileDest = TagLib.File.Create(path, ReadStyle.PictureLazy);
                        if (newBitrate > tfileDest.Properties.AudioBitrate)
                        {
                            File.Delete(output);
                            File.Move(path, output);
                        }
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        MyConsole.WriteLine(e);
                        MyConsole.WriteLine(ioe);
#endif
                        //ignored;
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    MyConsole.WriteLine(e);
#endif
                    //ignored
                }
                path = output;
            }
            
            if (generateStateHandlerEntry || isNew)
            {
                AddSong(path, title, artists, album, generateStateHandlerEntry, isNew, remoteHostname);
            }

            List<string> missingArtist = (from artist in artists let artistPath = $"{_musicFolder}/{Sanitize(artist)}" where !File.Exists($"{artistPath}/cover.jpg") && !File.Exists($"{artistPath}/cover.png") select artist).ToList();
            if (!string.IsNullOrEmpty(album) && album != null)
            {
                string albumPath = $"{_musicFolder}/{Sanitize(artists[0])}/{Sanitize(album)}";
                if (!File.Exists($"{albumPath}/cover.jpg") && !File.Exists($"{albumPath}/cover.png"))
                {
                    return (missingArtist, (album, Sanitize(artists[0])));
                }
            }
            return (missingArtist, (string.Empty, string.Empty));
        }
        
        public delegate void SnackbarMessageDelegate(string message);
        public static void AddSong(string path, string title, string[] artists, string artistId,
            string recordingId, string acoustIdTrackId, string? album = null, string? releaseGroupId = null)
        {
            using TagLib.File tfile = TagLib.File.Create(path, ReadStyle.PictureLazy);
            tfile.Tag.Title = title;
            if (artists.Length == 0)
            {
                artists = new[] { "No Artist" };
            }
            tfile.Tag.Performers = artists;
            tfile.Tag.AlbumArtists = artists;
            if (!string.IsNullOrEmpty(artistId))
            {
                tfile.Tag.MusicBrainzArtistId = artistId;
            }
            if (!string.IsNullOrEmpty(recordingId))
            {
                tfile.Tag.MusicBrainzTrackId = recordingId;
            }
            
            if (!string.IsNullOrEmpty(acoustIdTrackId))
            {
                tfile.writePrivateFrame("AcoustIDTrackID", acoustIdTrackId);
            }
            
            
            string output = $"{_musicFolder}/{Sanitize(artists[0])}";
            if (!string.IsNullOrEmpty(album))
            {
                output = $"{output}/{Sanitize(album!)}";
                tfile.Tag.Album = album;
                if (!string.IsNullOrEmpty(releaseGroupId))
                {
                    tfile.Tag.MusicBrainzReleaseGroupId = releaseGroupId;
                }
            }

            int newBitrate = tfile.Properties.AudioBitrate;
            tfile.Save();
            Directory.CreateDirectory(output);
            output = $"{output}/{Sanitize(title)}.mp3";
            try
            {
                File.Move(path, output);
            }
            catch (IOException)
            {
                using TagLib.File tfileDest = TagLib.File.Create(path, ReadStyle.PictureLazy);
                if (newBitrate > tfileDest.Properties.AudioBitrate)
                {
                    File.Delete(output);
                    File.Move(path, output);
                }
            }
            catch (Exception)
            {
                File.Delete(path);
#if DEBUG
                MyConsole.WriteLine("Video already exists");
#endif
                Snackbar.Make($"Exists: {title}");
                return;
            }
            File.Delete(path);
            Snackbar.Make($"Success: {title}");

            AddSong(path, title, artists, album);
        }
        
        public static string GetImageFormat(byte[] image)
        {
            byte[] png = { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 };
            byte[] jpg = { 255, 216, 255 };
            if (image.Take(3).SequenceEqual(jpg))
            {
                return ".jpg";
            }

            return image.Take(16).SequenceEqual(png) ? ".png" : ".idk";
        }
        
        public static string GetImageFormat(string imagePath)
        {
            using BinaryReader reader = new BinaryReader(File.OpenRead(imagePath));
            byte[] magicBytes = reader.ReadBytes(16);

            return GetImageFormat(magicBytes);
        }

        public static bool IsTrustedSsid(string ssid)
        {
            try
            {
                string json = File.ReadAllText($"{_privatePath}/trusted_SSIDs.json");
                List<string> ssids = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                return ssids.Contains(ssid);
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                return false;
            }
        }
        
        public static List<string> GetTrustedSsids()
        {
            try
            {
                string json = File.ReadAllText($"{_privatePath}/trusted_SSIDs.json");
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                return new List<string>();
            }
        }

        public static void AddTrustedSsid(string ssid)
        {
            try
            {
#if DEBUG
                MyConsole.WriteLine(ssid);
#endif
                string json = File.ReadAllText($"{_privatePath}/trusted_SSIDs.json");
                List<string> ssids = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                ssids.Add(ssid);
                File.WriteAllText($"{_privatePath}/trusted_SSIDs.json", JsonConvert.SerializeObject(ssids));
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                //ignored
            }
        }
        
        public static void DeleteTrustedSsid(string ssid)
        {
            try
            {
                string json = File.ReadAllText($"{_privatePath}/trusted_SSIDs.json");
                List<string> ssids = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                ssids.Remove(ssid);
                File.WriteAllText($"{_privatePath}/trusted_SSIDs.json", JsonConvert.SerializeObject(ssids));
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                //ignored
            }
        }

        private static HashSet<string> GetHashSet()
        {
            string json = File.ReadAllText($"{_musicFolder}/usedChromaprintSongs.json");
            return JsonConvert.DeserializeObject<HashSet<string>>(json) ?? new HashSet<string>();
        }
        
        private static void SaveHashSet()
        {
            File.WriteAllText($"{_musicFolder}/usedChromaprintSongs.json", JsonConvert.SerializeObject(_chromaprintUsedSongs));
        }
    }
}