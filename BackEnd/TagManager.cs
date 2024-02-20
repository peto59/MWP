using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.Graphics;
using MWP.DatatypesAndExtensions;
using MWP.Helpers;
using TagLib;
using File = System.IO.File;

namespace MWP.BackEnd
{
    internal sealed class TagManager : IDisposable
    {
        private TagLib.File? tfile;

        public bool Changed { get; private set; }

        private Song? song;
        private SongSave saveFlags = SongSave.None;

        public string OriginalTitle
        {
            get;
        }

        public string[] OriginalArtists
        {
            get;
        }

        public string OriginalArtist => string.Join(';', OriginalArtists);

        public string OriginalAlbum
        {
            get;
            private set;
        }

        public TagManager(Song song)
        {
            tfile = TagLib.File.Create(song.Path);
            this.song = song;
            OriginalTitle = tfile?.Tag.Title ?? this.song?.Title ?? "No Title";
            OriginalArtists = tfile?.Tag.Performers ??
                              this.song?.Artists.Select(a => a.Title).ToArray() ?? new[] { "No Artist" };
            OriginalAlbum = tfile?.Tag.Album ?? this.song?.Album.Title ?? "No Album";
        }

        public string Title
        {
            get => tfile?.Tag.Title ?? song?.Title ?? "No Title";
            set
            {
                if (tfile == null) return;
                if (value == OriginalTitle || string.IsNullOrEmpty(value)) return;
                tfile.Tag.Title = value;
                Changed = true;
                saveFlags |= SongSave.Title;
            }
        }

        public string Artist
        {
            get => tfile?.Tag.Performers.FirstOrDefault() ?? tfile?.Tag.Performers[0] ?? song?.Artist.Title ?? "No Artist";
            set
            {
                if (tfile == null) return;
                string[] artists = value.Split(';');
                Artists = artists;
            }
        }

        public string[] Artists
        {
            get => tfile?.Tag.Performers ?? song?.Artists.Select(a => a.Title).ToArray() ?? new []{"No Artist"};
            set
            {
                if (tfile == null) return;
                if (value.SequenceEqual(OriginalArtists) || value.Length <= 0) return;
                tfile.Tag.Performers = value;
                Changed = true;
                saveFlags |= SongSave.Artist;
            }
        }

        public Bitmap Image
        {
            get
            {
                byte[]? dataData = tfile?.Tag.Pictures[0].Data.Data;
                if (dataData != null)
                {
                    using MemoryStream ms = new MemoryStream(dataData);
                    return BitmapFactory.DecodeStream(ms) ?? MusicBaseClassStatic.Placeholder;
                }

                return MusicBaseClassStatic.Placeholder;
            }

            set
            {
                if (tfile == null)
                {
#if DEBUG
                    MyConsole.WriteLine($"Tfile is null");
#endif
                    return;
                }
                if (value.SameAs(Image))
                {
#if DEBUG
                    MyConsole.WriteLine($"Image is same returning");
#endif
                    return;
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    value.Compress(Bitmap.CompressFormat.Png, 0, stream);
                    IPicture[] pics = new IPicture[1];
                    pics[0] = new TagLib.Picture(stream.ToArray());
                    tfile.Tag.Pictures = pics;
                }
                Changed = true;
                saveFlags |= SongSave.Image;
            }
        }
        
        public string Album
        {
            get => tfile?.Tag.Album ?? song?.Album.Title ?? "No Album";
            set
            {
                if (tfile == null) return;
                if (value == OriginalAlbum) return;
                if (value == "No Album" || string.IsNullOrEmpty(value))
                {
                    NoAlbum();
                }
                else
                {
                    tfile.Tag.Album = value;
                }
                Changed = true;
                saveFlags |= SongSave.Album;
                saveFlags &= ~SongSave.NoAlbum;
            }
        }

        public void NoAlbum()
        {
            if (tfile == null) return;
            if (string.IsNullOrEmpty(tfile.Tag.Album))
                return;
            tfile.Tag.Album = null;
            Changed = true;
            saveFlags |= SongSave.NoAlbum;
            saveFlags &= ~SongSave.Album;
        }

        public void Save()
        {
            if (!Changed)
            {
#if DEBUG
                MyConsole.WriteLine("No Changes are being made!");       
#endif
                return;
            }
            
            List<string> playlistsToUpdate = new List<string>();
            if (song != null)
            {
                string originalPath = song.Path;
                foreach (string playlist in FileManager.GetPlaylist().Where(playlist => FileManager.GetPlaylist(playlist).Any(playlistSong => playlistSong.Path == originalPath)))
                {
                    playlistsToUpdate.Add(playlist);
                    FileManager.DeletePlaylist(playlist, song);
                }
            }

            Changed = false;
            bool  movingFlag = saveFlags.HasFlag(SongSave.Title) || saveFlags.HasFlag(SongSave.Artist) ||
                                    saveFlags.HasFlag(SongSave.Album) || saveFlags.HasFlag(SongSave.NoAlbum);
            
            tfile?.Save();

            string path = FileManager.MusicFolder;

            if (movingFlag)
            {
                song?.Nuke();
            }

            if (saveFlags.HasFlag(SongSave.Artist) && tfile != null)
            {
                
                path = $"{path}/{FileManager.Sanitize(tfile.Tag.Performers.FirstOrDefault() ?? tfile.Tag.Performers[0])}";
            }
            else if(song != null)
            {
                path = $"{path}/{FileManager.Sanitize(song.Artists[0].Title)}";
            }

            if (saveFlags.HasFlag(SongSave.Album) && tfile != null)
            {
                if (string.IsNullOrEmpty(tfile.Tag.Album))
                {
                    path = $"{path}/{FileManager.Sanitize(tfile.Tag.Album)}";
                }
            }
            /*else if (!_saveFlags.HasFlag(SongSave.NoAlbum) && _song is { Albums: { Count: > 0 } })
            {
                path = $"{path}/{FileManager.Sanitize(_song.Album.Title)}";
            }*/


            Song? s = null;
            if (tfile != null)
            {
                path = $"{path}/{FileManager.Sanitize(tfile.Tag.Title)}.mp3";
                if (saveFlags.HasFlag(SongSave.Title))
                {
                
                    if(song != null)
                        s = new Song(song, tfile.Tag.Title, path);

                }else if (song != null)
                {
                    s = new Song(song, song.Title, path);
                }
            }

            Album? a = null;
            if (saveFlags.HasFlag(SongSave.Album) && tfile != null && s != null)
            {
                //restore albums
                List<Album> inListAlbum = MainActivity.StateHandler.Albums.Select(tfile.Tag.Album);
                if (inListAlbum.Count > 0)
                {
                    a = inListAlbum[0];
                    a.AddSong(ref s);
                }
                else
                {
                    a = new Album(tfile.Tag.Album, MWP.Album.GetImagePath(tfile.Tag.Album, FileManager.Sanitize(FileManager.GetAlias(tfile.Tag.Performers.FirstOrDefault() ?? tfile.Tag.Performers[0]))));
                    a.AddSong(ref s);
                    MainActivity.StateHandler.Albums.Add(a);
                }
                s.AddAlbum(ref a);
            }
            
            if (saveFlags.HasFlag(SongSave.Artist) && tfile != null && a != null && s != null)
            {
                //restore artists
                foreach (string performer in tfile.Tag.Performers)
                {
                    Artist art;
                    string alias = FileManager.GetAlias(performer);
                    List<Artist> inList = MainActivity.StateHandler.Artists.Select(alias);
                    if (inList.Count > 0)
                    {
                        art = inList[0];
                        art.AddSong(ref s);
                        if (saveFlags.HasFlag(SongSave.Album))
                        {
                            a.AddArtist(ref art);
                            if (art.Albums.Select(a.Title).Count == 0)
                            {
                                art.AddAlbum(ref a);
                            }
                        }
                    }
                    else
                    {
                        art = new Artist(alias, MWP.Artist.GetImagePath(FileManager.Sanitize(performer)));
                        art.AddSong(ref s);
                        if (saveFlags.HasFlag(SongSave.Album))
                        {
                            a.AddArtist(ref art);
                            art.AddAlbum(ref a);
                        }
                        MainActivity.StateHandler.Artists.Add(art);
                    }
                    s.AddArtist(ref art);
                }
            }
            
            if (movingFlag && song != null && s != null)
            {
                List<Song> inList = MainActivity.StateHandler.Songs.Select(s.Title);
                if (inList.Count == 0)
                {
                    MainActivity.StateHandler.Songs.Add(s);
                }
                File.Move(song.Path, path);
                song.Artists.ForEach(o => o.AddSong(ref s));
                song.Albums.ForEach(o => o.AddSong(ref s));
                song = s;
            }


            if (s != null) StateHandler.TriggerTagManagerFragmentRefresh(OriginalTitle, s);
            foreach (string playlist in playlistsToUpdate)
            {
                if (s != null) FileManager.AddToPlaylist(playlist, s);
            }


            saveFlags = SongSave.None;
            #if DEBUG
            MyConsole.WriteLine("Save is saving ver save (TagManager)!");
            #endif
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
#if DEBUG
            MyConsole.WriteLine("Dispose in TagManager log");   
#endif
            if (disposing)
            {
                //Save();
                
                // Dispose of managed resources
                if (tfile != null)
                {
                    tfile.Dispose();
                    tfile = null;
                }
            }

            // Dispose of unmanaged resources (if any)

            // Set fields to null (optional)
            song = null;
        }

        
        ~TagManager()
        {
            Dispose();
        }
    }
}