using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Ass_Pain
{
    internal class TagManager : IDisposable
    {
        private TagLib.File tfile;
        private bool changed;
        private Song song;
        private SongSave saveFlags = SongSave.None;

        public TagManager(Song song)
        {
            tfile = TagLib.File.Create(song.Path);
            this.song = song;
        }

        public string Title
        {
            get
            {
                return tfile.Tag.Title;
            }
            set
            {
                if (value == Title || string.IsNullOrEmpty(value)) return;
                tfile.Tag.Title = value;
                changed = true;
                saveFlags |= SongSave.Title;
            }
        }

        public string Artist
        {
            get
            {
                return tfile.Tag.Performers.FirstOrDefault();
            }
            set
            {
                if (value == tfile.Tag.Performers.FirstOrDefault() || string.IsNullOrEmpty(value)) return;
                tfile.Tag.Performers = new[] { value };
                changed = true;
                saveFlags |= SongSave.Artist;
            }
        }

        public string[] Artists
        {
            get
            {
                return tfile.Tag.Performers;
            }
            set
            {
                if (value.SequenceEqual(tfile.Tag.Performers) || value.Length <= 0) return;
                tfile.Tag.Performers = value;
                changed = true;
                saveFlags |= SongSave.Artist;
            }
        }
        
        public string Album
        {
            get
            {
                return tfile.Tag.Album;
            }
            set
            {
                if (value == tfile.Tag.Album || string.IsNullOrEmpty(value)) return;
                tfile.Tag.Album = value;
                changed = true;
                saveFlags |= SongSave.Album;
                saveFlags &= ~SongSave.NoAlbum;
            }
        }

        public void NoAlbum()
        {
            //TODO: pridaj if iba ak album predtym bol nastaveny
            tfile.Tag.Album = null;
            changed = true;
            saveFlags |= SongSave.NoAlbum;
            saveFlags &= ~SongSave.Album;
        }

        public void Save()
        {
            if (!changed)
            {   
                return;
            }

            changed = false;
            bool  movingFlag = saveFlags.HasFlag(SongSave.Title) || saveFlags.HasFlag(SongSave.Artist) ||
                                    saveFlags.HasFlag(SongSave.Album) || saveFlags.HasFlag(SongSave.NoAlbum);
            
            tfile.Save();

            string path = FileManager.MusicFolder;

            if (movingFlag)
            {
                song.Nuke();
            }

            if (saveFlags.HasFlag(SongSave.Artist))
            {
                path = $"{path}/{FileManager.Sanitize(FileManager.GetAlias(tfile.Tag.Performers.FirstOrDefault()))}";
            }
            else
            {
                path = $"{path}/{FileManager.Sanitize(song.Artists[0].Title)}";
            }

            if (saveFlags.HasFlag(SongSave.Album))
            {
                path = $"{path}/{FileManager.Sanitize(tfile.Tag.Album)}";
            }
            else if (!saveFlags.HasFlag(SongSave.NoAlbum))
            {
                if (song.Albums.Count > 0)
                {
                    path = $"{path}/{FileManager.Sanitize(song.Album.Title)}";
                }
            }


            Song s;
            if (saveFlags.HasFlag(SongSave.Title))
            {
                
                path = $"{path}/{FileManager.Sanitize(tfile.Tag.Title)}";
                s = new Song(song, tfile.Tag.Title, path);

            }else
            {
                path = $"{path}/{FileManager.Sanitize(song.Title)}";
                s = new Song(song, song.Title, path);
            }

            Album a = null;
            if (saveFlags.HasFlag(SongSave.Album))
            {
                //restore albums
                List<Album> inListAlbum = MainActivity.stateHandler.Albums.Select(tfile.Tag.Album);
                if (inListAlbum.Count > 0)
                {
                    a = inListAlbum[0];
                    a.AddSong(ref s);
                }
                else
                {
                    a = new Album(tfile.Tag.Album, Ass_Pain.Album.GetImagePath(tfile.Tag.Album, FileManager.Sanitize(FileManager.GetAlias(tfile.Tag.Performers.FirstOrDefault()))));
                    a.AddSong(ref s);
                    MainActivity.stateHandler.Albums.Add(a);
                }
                s.AddAlbum(ref a);
            }
            
            if (saveFlags.HasFlag(SongSave.Artist))
            {
                //restore artists
                foreach (string performer in tfile.Tag.Performers)
                {
                    Artist art;
                    string alias = FileManager.GetAlias(performer);
                    List<Artist> inList = MainActivity.stateHandler.Artists.Select(alias);
                    if (inList.Count > 0)
                    {
                        art = inList[0];
                        art.AddSong(ref s);
                        if (saveFlags.HasFlag(SongSave.Album))
                        {
                            a?.AddArtist(ref art);
                            if (art.Albums.Select(a?.Title).Count == 0)
                            {
                                art.AddAlbum(ref a);
                            }
                        }
                    }
                    else
                    {
                        art = new Artist(alias, Ass_Pain.Artist.GetImagePath(FileManager.Sanitize(alias)));
                        art.AddSong(ref s);
                        if (saveFlags.HasFlag(SongSave.Album))
                        {
                            a?.AddArtist(ref art);
                            art.AddAlbum(ref a);
                        }
                        MainActivity.stateHandler.Artists.Add(art);
                    }
                    s.AddArtist(ref art);
                }
            }
            
            if (movingFlag)
            {
                List<Song> inList = MainActivity.stateHandler.Songs.Select(s.Title);
                if (inList.Count == 0)
                {
                    MainActivity.stateHandler.Songs.Add(s);
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
                Save();
                
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

    [Flags]
    internal enum SongSave
    {
        None = 0,
        Title = 1,
        Artist = 2,
        Album = 4,
        NoAlbum = 8
    }
}