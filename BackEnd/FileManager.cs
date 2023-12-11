using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.Views;
using Google.Android.Material.Snackbar;
using Newtonsoft.Json;
using TagLib;
using TagLib.Id3v2;
using File = System.IO.File;
using Tag = TagLib.Id3v2.Tag;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd
{
    internal static class FileManager
    {
        private static readonly string? Root = (string?)Android.OS.Environment.ExternalStorageDirectory;
        // ReSharper disable once InconsistentNaming
        private static readonly string? _privatePath = Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
        public static string PrivatePath => _privatePath ?? string.Empty;
        // ReSharper disable once InconsistentNaming
        private static readonly string? _musicFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic)?.AbsolutePath;
        public static string MusicFolder => _musicFolder ?? string.Empty;
        //private static readonly string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string( System.IO.Path.GetInvalidFileNameChars() ) + new string( System.IO.Path.GetInvalidPathChars() )+"'`/|\\:*\"#?<>");
        //private static readonly string invalidRegStr = string.Format( @"([{0}]*\.+$)|([{0}]+)", invalidChars );
        private static readonly string InvalidRegStr = string.Format( @"([{0}]*\.+$)|([{0}]+)", System.Text.RegularExpressions.Regex.Escape(new string( Path.GetInvalidFileNameChars() ) + new string( Path.GetInvalidPathChars() )+"'`/|\\:*\"#?<>") );

        public static void Innit()
        {
            if (!Directory.Exists(_musicFolder))
            {
#if DEBUG
                MyConsole.WriteLine("Creating " + $"{_musicFolder}");
#endif
                if (_musicFolder != null) Directory.CreateDirectory(_musicFolder);
            }

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

            if (!File.Exists($"{_musicFolder}/aliases.json"))
            {
                File.WriteAllTextAsync($"{_musicFolder}/aliases.json", JsonConvert.SerializeObject(new Dictionary<string, string>()));

            }

            if (!File.Exists($"{_musicFolder}/playlists.json"))
            {
                File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(new Dictionary<string, List<string>>()));
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
        /// Creates virtual song topology in MainActivity.StateHandler and allocates all new files
        /// </summary>
        public static void DiscoverFiles(bool generateStateHandlerEntry = false)
        {
            MainActivity.StateHandler.FileListGenerationEvent.WaitOne();
            if (Root != null) DiscoverFiles(Root, generateStateHandlerEntry);
            MainActivity.StateHandler.FileListGenerationEvent.Set();
        }

        private static void DiscoverFiles(string path, bool generateStateHandlerEntry)
        {
            string nameFromPath = GetNameFromPath(path);
            if(nameFromPath.StartsWith(".") || File.Exists($"{path}/.nomedia"))
            {
                return;
            }
            if (generateStateHandlerEntry && path == _musicFolder)
            {
                return;
            }
            switch (nameFromPath)
            {
                case "Android":
                case "sound_recorder":
                case "Notifications":
                case "Recordings":
                case "Ringtones":
                case "MIUI":
                case "Alarms":
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
#if DEBUG
                    MyConsole.WriteLine($"Processing: {file}");
#endif
                    AddSong(file, _musicFolder != null && !file.Contains(_musicFolder), generateStateHandlerEntry || (_musicFolder != null && !file.Contains(_musicFolder)));
                    
                }
                catch(Exception ex)
                {
#if DEBUG
                    MyConsole.WriteLine($"error: {file}");
                    MyConsole.WriteLine(ex);
#endif
                }
            }
        }

        ///<summary>
        ///Creates virtual song topology in MainActivity.StateHandler
        ///</summary>
        public static void GenerateList(string path, bool first = true)
        {

            if (first)
            {
                MainActivity.StateHandler.FileListGenerationEvent.WaitOne();
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
                MainActivity.StateHandler.FileListGenerationEvent.Set();
            }
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
        private static List<string> GetAlbums(string author)
        {
            return Directory.EnumerateDirectories(author).ToList();
        }

        ///<summary>
        ///Gets all songs in device
        ///</summary>
        public static int GetSongsCount()
        {
            return Directory.EnumerateFiles(_musicFolder ?? string.Empty, "*.mp3", SearchOption.AllDirectories).Count();
        }

        ///<summary>
        ///Gets all songs in album or all album-less songs for author
        ///</summary>
        private static List<string> GetSongs(string path)
        {
            return Directory.EnumerateFiles(path, "*.mp3").ToList();
        }
        

        ///<summary>
        ///Gets last name/folder from <paramref name="path"/>
        ///</summary>
        public static string GetNameFromPath(string path)
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
            while (true)
            {
                string json = File.ReadAllText($"{_musicFolder}/aliases.json");
                Dictionary<string, string> aliases = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
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
            string json = File.ReadAllText($"{_musicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
            playlists.Add(name, new List<string>());
            File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        public static void AddToPlaylist(string name, string song)
        {
            string json = File.ReadAllText($"{_musicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
            playlists[name].Add(song);
            File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        public static void AddToPlaylist(string name, List<string> songs)
        {
            string json = File.ReadAllText($"{_musicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
            playlists[name].AddRange(songs);
            File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }


        ///<summary>
        ///Deletes <paramref name="song"/> from <paramref name="playlist"/>
        ///</summary>
        public static void DeletePlaylist(string playlist, string song)
        {
            string json = File.ReadAllText($"{_musicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
            if (!playlists.TryGetValue(playlist, out List<string> playlist1)) return;
            playlist1.Remove(song);
            File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(playlists));
        }

        ///<summary>
        ///Deletes <paramref name="playlist"/>
        ///</summary>
        public static void DeletePlaylist(string playlist)
        {
            string json = File.ReadAllText($"{_musicFolder}/playlists.json");
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
            playlists.Remove(playlist);
            File.WriteAllTextAsync($"{_musicFolder}/playlists.json", JsonConvert.SerializeObject(playlists));
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

        public static string GetAvailableFile(string path, string name, string extension)
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

        ///<summary>
        ///Gets all playlist names
        ///</summary>
        public static List<string> GetPlaylist()
        {
            try
            {
                string json = File.ReadAllText($"{_musicFolder}/playlists.json");
                Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
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
            Dictionary<string, List<string>> playlists = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
            if (!playlists.TryGetValue(playlist, out List<string> playlist1)) return new List<Song>();
            List<Song> x = new List<Song>();
            foreach (string song in playlist1)
            {
                List<Song> y = MainActivity.StateHandler.Songs.Where(a => a.Path == song).ToList();
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

        public static void AddTrustedSyncTarget(string host)
        {

            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> hosts =
                JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            hosts.Add(host, MainActivity.StateHandler.Songs);
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
            //TODO: add to this list on download and receiving song from network
            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            return targets.TryGetValue(host, out List<Song> target) ? target : new List<Song>();
        }
        
        public static List<string> GetTrustedSyncTargets()
        {
            string json = File.ReadAllText($"{_privatePath}/trusted_sync_targets.json");
            SongJsonConverter customConverter = new SongJsonConverter(true);
            Dictionary<string, List<Song>> targets = JsonConvert.DeserializeObject<Dictionary<string, List<Song>>>(json, customConverter) ?? new Dictionary<string, List<Song>>();
            return targets.Keys.ToList();
        }

        private static void AddSong(string path, string title, IReadOnlyList<string> artists, string? album = null)
        {
            List<Artist> artistList = new List<Artist>();
            foreach (string artist in artists)
            {
                Artist artistObj;
                List<Artist> inArtistList = MainActivity.StateHandler.Artists.Select(GetAlias(artist));
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
                    MainActivity.StateHandler.Artists.Add(artistObj);
                }
                artistList.Add(artistObj);
            }
            
            Song song = new Song(artistList, title, File.GetCreationTime(path), path);
            artistList.ForEach(art => art.AddSong(ref song));
            //TODO: prepend
            MainActivity.StateHandler.Songs.Add(song);

            Album albumObj = new Album(string.Empty, string.Empty, false, false);
            if (!string.IsNullOrEmpty(album))
            {
                if (album != null)
                {
                    List<Album> inAlbumList = MainActivity.StateHandler.Albums.Select(album);
                    if (inAlbumList.Any())
                    {
                        albumObj = inAlbumList[0];
                        albumObj.AddSong(ref song);
                        albumObj.AddArtist(ref artistList);
                    }
                    else
                    {
                        string albumPath = Sanitize(album);
                        string artistPath = Sanitize(GetAlias(artists[0]));
                        if(File.Exists($"{_musicFolder}/{artistPath}/{albumPath}/cover.jpg"))
                            albumObj = new Album(album, song, artistList, $"{_musicFolder}/{artistPath}/{albumPath}/cover.jpg");
                        else if(File.Exists($"{_musicFolder}/{artistPath}/{albumPath}/cover.png"))
                            albumObj = new Album(album, song, artistList, $"{_musicFolder}/{artistPath}/{albumPath}/cover.png");
                        else
                            albumObj = new Album(album, song, artistList, "Default");
                        MainActivity.StateHandler.Albums.Add(albumObj);
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
        }

        public static (List<string> missingArtists, (string album, string artistPath) missingAlbum) AddSong(string path, bool isNew = false, bool generateStateHandlerEntry = true)
        {
            using TagLib.File tfile = TagLib.File.Create(path, ReadStyle.PictureLazy);
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
            else
            {
                title = Path.GetFileName(path).Replace(".mp3", "");
                tfile.Tag.Title = title;
                tfile.Save();
            }

            string[] artists = (tfile.Tag.Performers.Any() ? tfile.Tag.Performers : tfile.Tag.AlbumArtists.Any() ? tfile.Tag.AlbumArtists : new []{ "No Artist" }).Distinct().ToArray();

            string album = tfile.Tag.Album;
            if (isNew)
            {
                string output = $"{_musicFolder}/{Sanitize(GetAlias(artists[0]))}";
                if (!string.IsNullOrEmpty(album))
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
                catch (Exception e)
                {
#if DEBUG
                    MyConsole.WriteLine(e);
#endif
                }
                path = output;
            }


            if (generateStateHandlerEntry)
            {
                AddSong(path, title, artists, album);
            }

            List<string> missingArtists = (from artist in artists let artistPath = $"{_musicFolder}/{Sanitize(GetAlias(artist))}" where !File.Exists($"{artistPath}/cover.jpg") && !File.Exists($"{artistPath}/cover.png") select artist).ToList();
            if (!string.IsNullOrEmpty(album))
            {
                string albumPath = $"{_musicFolder}/{Sanitize(GetAlias(artists[0]))}/{Sanitize(album)}";
                if (!File.Exists($"{albumPath}/cover.jpg") && !File.Exists($"{albumPath}/cover.png"))
                {
                    return (missingArtists, (album, Sanitize(GetAlias(artists[0]))));
                }
            }
            return (missingArtists, (string.Empty, string.Empty));
        }
        
        public static void AddSong(View view, string path, string title, string[] artists, string artistId,
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
            
            //https://stackoverflow.com/questions/34507982/adding-custom-tag-using-taglib-sharp-library
            if (!string.IsNullOrEmpty(acoustIdTrackId))
            {
                Tag custom = (Tag) tfile.GetTag(TagTypes.Id3v2);
                PrivateFrame p = PrivateFrame.Get(custom, "AcoustIDTrackID", true);
                p.PrivateData = System.Text.Encoding.UTF8.GetBytes(acoustIdTrackId);
            }
            
            //reading private frame
            // File f = File.Create("<YourMP3.mp3>");
            // TagLib.Id3v2.Tag t = (TagLib.Id3v2.Tag)f.GetTag(TagTypes.Id3v2);
            // PrivateFrame p = PrivateFrame.Get(t, "CustomKey", false); // This is important. Note that the third parameter is false.
            // string data = Encoding.UTF8.GetString(p.PrivateData.Data);
            
            string output = $"{_musicFolder}/{Sanitize(GetAlias(artists[0]))}";
            if (!string.IsNullOrEmpty(album))
            {
                output = $"{output}/{Sanitize(album!)}";
                tfile.Tag.Album = album;
                if (!string.IsNullOrEmpty(releaseGroupId))
                {
                    tfile.Tag.MusicBrainzReleaseGroupId = releaseGroupId;
                }
            }
            tfile.Save();
            Directory.CreateDirectory(output);
            output = $"{output}/{Sanitize(title)}.mp3";
            try
            {
                File.Move(path, output);
            }
            catch
            {
                File.Delete(path);
#if DEBUG
                MyConsole.WriteLine("Video already exists");
#endif
                Snackbar.Make(view, $"Exists: {title}", BaseTransientBottomBar.LengthLong).Show();
                return;
            }
            File.Delete(path);
            Snackbar.Make(view, $"Success: {title}", BaseTransientBottomBar.LengthLong).Show();

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
            }
        }
    }
}