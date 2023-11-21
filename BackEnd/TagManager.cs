using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ass_Pain.BackEnd
{
    internal class TagManager : IDisposable
    {
        private TagLib.File? _tfile;
        private bool _changed;

        public bool Changed => _changed;
        private Song? _song;
        private SongSave _saveFlags = SongSave.None;

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
            _tfile = TagLib.File.Create(song.Path);
            _song = song;
            OriginalTitle = _tfile?.Tag.Title ?? _song?.Title ?? "No Title";
            OriginalArtists = _tfile?.Tag.Performers ??
                              _song?.Artists.Select(a => a.Title).ToArray() ?? new[] { "No Artist" };
            OriginalAlbum = _tfile?.Tag.Album ?? _song?.Album.Title ?? "No Album";
        }

        public string Title
        {
            get => _tfile?.Tag.Title ?? _song?.Title ?? "No Title";
            set
            {
                if (_tfile == null) return;
                if (value == OriginalTitle || string.IsNullOrEmpty(value)) return;
                _tfile.Tag.Title = value;
                _changed = true;
                _saveFlags |= SongSave.Title;
            }
        }

        public string Artist
        {
            get => _tfile?.Tag.Performers.FirstOrDefault() ?? _tfile?.Tag.Performers[0] ?? _song?.Artist.Title ?? "No Artist";
            set
            {
                if (_tfile == null) return;
                string[] artists = value.Split(';');
                Artists = artists;
            }
        }

        public string[] Artists
        {
            get => _tfile?.Tag.Performers ?? _song?.Artists.Select(a => a.Title).ToArray() ?? new []{"No Artist"};
            set
            {
                if (_tfile == null) return;
                if (value.SequenceEqual(OriginalArtists) || value.Length <= 0) return;
                _tfile.Tag.Performers = value;
                _changed = true;
                _saveFlags |= SongSave.Artist;
            }
        }
        
        public string Album
        {
            get => _tfile?.Tag.Album ?? _song?.Album.Title ?? "No Album";
            set
            {
                if (_tfile == null) return;
                if (value == OriginalAlbum) return;
                if (value == "No Album" || string.IsNullOrEmpty(value))
                {
                    NoAlbum();
                }
                else
                {
                    _tfile.Tag.Album = value;
                }
                _changed = true;
                _saveFlags |= SongSave.Album;
                _saveFlags &= ~SongSave.NoAlbum;
            }
        }

        public void NoAlbum()
        {
            if (_tfile == null) return;
            if (string.IsNullOrEmpty(_tfile.Tag.Album))
                return;
            _tfile.Tag.Album = null;
            _changed = true;
            _saveFlags |= SongSave.NoAlbum;
            _saveFlags &= ~SongSave.Album;
        }

        public void Save()
        {
            if (!_changed)
            {   
                return;
            }

            _changed = false;
            bool  movingFlag = _saveFlags.HasFlag(SongSave.Title) || _saveFlags.HasFlag(SongSave.Artist) ||
                                    _saveFlags.HasFlag(SongSave.Album) || _saveFlags.HasFlag(SongSave.NoAlbum);
            
            _tfile?.Save();

            string path = FileManager.MusicFolder;

            if (movingFlag)
            {
                _song?.Nuke();
            }

            if (_saveFlags.HasFlag(SongSave.Artist) && _tfile != null)
            {
                
                path = $"{path}/{FileManager.Sanitize(FileManager.GetAlias(_tfile.Tag.Performers.FirstOrDefault() ?? _tfile.Tag.Performers[0]))}";
            }
            else if(_song != null)
            {
                path = $"{path}/{FileManager.Sanitize(_song.Artists[0].Title)}";
            }

            if (_saveFlags.HasFlag(SongSave.Album) && _tfile != null)
            {
                if (string.IsNullOrEmpty(_tfile.Tag.Album))
                {
                    path = $"{path}/{FileManager.Sanitize(_tfile.Tag.Album)}";
                }
            }
            /*else if (!_saveFlags.HasFlag(SongSave.NoAlbum) && _song is { Albums: { Count: > 0 } })
            {
                path = $"{path}/{FileManager.Sanitize(_song.Album.Title)}";
            }*/


            Song? s = null;
            if (_tfile != null)
            {
                path = $"{path}/{FileManager.Sanitize(_tfile.Tag.Title)}";
                if (_saveFlags.HasFlag(SongSave.Title))
                {
                
                    if(_song != null)
                        s = new Song(_song, _tfile.Tag.Title, path);

                }else if (_song != null)
                {
                    s = new Song(_song, _song.Title, path);
                }
            }

            Album? a = null;
            if (_saveFlags.HasFlag(SongSave.Album) && _tfile != null && s != null)
            {
                //restore albums
                List<Album> inListAlbum = MainActivity.stateHandler.Albums.Select(_tfile.Tag.Album);
                if (inListAlbum.Count > 0)
                {
                    a = inListAlbum[0];
                    a.AddSong(ref s);
                }
                else
                {
                    a = new Album(_tfile.Tag.Album, Ass_Pain.Album.GetImagePath(_tfile.Tag.Album, FileManager.Sanitize(FileManager.GetAlias(_tfile.Tag.Performers.FirstOrDefault() ?? _tfile.Tag.Performers[0]))));
                    a.AddSong(ref s);
                    MainActivity.stateHandler.Albums.Add(a);
                }
                s.AddAlbum(ref a);
            }
            
            if (_saveFlags.HasFlag(SongSave.Artist) && _tfile != null && a != null && s != null)
            {
                //restore artists
                foreach (string performer in _tfile.Tag.Performers)
                {
                    Artist art;
                    string alias = FileManager.GetAlias(performer);
                    List<Artist> inList = MainActivity.stateHandler.Artists.Select(alias);
                    if (inList.Count > 0)
                    {
                        art = inList[0];
                        art.AddSong(ref s);
                        if (_saveFlags.HasFlag(SongSave.Album))
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
                        art = new Artist(alias, Ass_Pain.Artist.GetImagePath(FileManager.Sanitize(alias)));
                        art.AddSong(ref s);
                        if (_saveFlags.HasFlag(SongSave.Album))
                        {
                            a.AddArtist(ref art);
                            art.AddAlbum(ref a);
                        }
                        MainActivity.stateHandler.Artists.Add(art);
                    }
                    s.AddArtist(ref art);
                }
            }
            
            if (movingFlag && _song != null && s != null)
            {
                List<Song> inList = MainActivity.stateHandler.Songs.Select(s.Title);
                if (inList.Count == 0)
                {
                    MainActivity.stateHandler.Songs.Add(s);
                }
                File.Move(_song.Path, path);
                _song.Artists.ForEach(o => o.AddSong(ref s));
                _song.Albums.ForEach(o => o.AddSong(ref s));
                _song = s;
            }

            _saveFlags = SongSave.None;
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
                if (_tfile != null)
                {
                    _tfile.Dispose();
                    _tfile = null;
                }
            }

            // Dispose of unmanaged resources (if any)

            // Set fields to null (optional)
            _song = null;
        }

        
        ~TagManager()
        {
            Dispose();
        }
    }
}