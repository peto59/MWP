using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MWP.DatatypesAndExtensions;

namespace MWP.BackEnd
{
    internal class TagManager : IDisposable
    {
        private TagLib.File? tfile;

        public bool Changed { get; private set; }

        private Song? song;
        private SongSave saveFlags = SongSave.None;

        public string OriginalTitle
        {
            get;
            private set;
        }

        public string[] OriginalArtists
        {
            get;
            private set;
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
                return;
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
                
                path = $"{path}/{FileManager.Sanitize(FileManager.GetAlias(tfile.Tag.Performers.FirstOrDefault() ?? tfile.Tag.Performers[0]))}";
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
                path = $"{path}/{FileManager.Sanitize(tfile.Tag.Title)}";
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
                List<Album> inListAlbum = StateHandler.Albums.Select(tfile.Tag.Album);
                if (inListAlbum.Count > 0)
                {
                    a = inListAlbum[0];
                    a.AddSong(ref s);
                }
                else
                {
                    a = new Album(tfile.Tag.Album, MWP.Album.GetImagePath(tfile.Tag.Album, FileManager.Sanitize(FileManager.GetAlias(tfile.Tag.Performers.FirstOrDefault() ?? tfile.Tag.Performers[0]))));
                    a.AddSong(ref s);
                    StateHandler.Albums.Add(a);
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
                    List<Artist> inList = StateHandler.Artists.Select(alias);
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
                        art = new Artist(alias, MWP.Artist.GetImagePath(FileManager.Sanitize(alias)));
                        art.AddSong(ref s);
                        if (saveFlags.HasFlag(SongSave.Album))
                        {
                            a.AddArtist(ref art);
                            art.AddAlbum(ref a);
                        }
                        StateHandler.Artists.Add(art);
                    }
                    s.AddArtist(ref art);
                }
            }
            
            if (movingFlag && song != null && s != null)
            {
                List<Song> inList = StateHandler.Songs.Select(s.Title);
                if (inList.Count == 0)
                {
                    StateHandler.Songs.Add(s);
                }
                File.Move(song.Path, path);
                song.Artists.ForEach(o => o.AddSong(ref s));
                song.Albums.ForEach(o => o.AddSong(ref s));
                song = s;
            }

            saveFlags = SongSave.None;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    
        protected virtual void Dispose(bool disposing)
        {
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