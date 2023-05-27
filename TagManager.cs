using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ass_Pain
{
    internal class TagManager : IDisposable
    {
        TagLib.File _tfile;
        private bool _changed = false;
        private Song _song;
        private SongSave _saveFlags = SongSave.None;

        public TagManager(Song song)
        {
            _tfile = TagLib.File.Create(song.Path);
            _song = song;
        }

        public string Title
        {
            get
            {
                return _tfile.Tag.Title;
            }
            set
            {
                if(value != Title && !string.IsNullOrEmpty(value))
                {
                    _tfile.Tag.Title = value;
                    _changed = true;
                    _saveFlags |= SongSave.Title;
                }
            }
        }

        public string Artist
        {
            get
            {
                return _tfile.Tag.Performers.FirstOrDefault();
            }
            set
            {
                if (value != _tfile.Tag.Performers.FirstOrDefault() && !string.IsNullOrEmpty(value))
                {
                    _tfile.Tag.Performers = new[] { value };
                    _changed = true;
                    _saveFlags |= SongSave.Artist;
                }
            }
        }

        public string[] Artists
        {
            get
            {
                return _tfile.Tag.Performers;
            }
            set
            {
                if (!value.SequenceEqual(_tfile.Tag.Performers) && value.Length > 0)
                {
                    _tfile.Tag.Performers = value;
                    _changed = true;
                    _saveFlags |= SongSave.Artist;
                }
            }
        }
        
        public string Album
        {
            get
            {
                return _tfile.Tag.Album;
            }
            set
            {
                if (value != _tfile.Tag.Album && !string.IsNullOrEmpty(value))
                {
                    _tfile.Tag.Album = value;
                    _changed = true;
                    _saveFlags |= SongSave.Album;
                    _saveFlags &= ~SongSave.NoAlbum;
                }
            }
        }

        public void NoAlbum()
        {
            //pridaj if iba ak album predtym bol nastaveny
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
            
            _tfile.Save();

            string path = FileManager.music_folder;

            if (movingFlag)
            {
                _song.Nuke();
            }

            if (_saveFlags.HasFlag(SongSave.Artist))
            {
                path = $"{path}/{FileManager.Sanitize(FileManager.GetAlias(_tfile.Tag.Performers.FirstOrDefault()))}";
            }
            else
            {
                path = $"{path}/{FileManager.Sanitize(_song.Artists[0].Title)}";
            }

            if (_saveFlags.HasFlag(SongSave.Album))
            {
                path = $"{path}/{FileManager.Sanitize(_tfile.Tag.Album)}";
                //restore albums
            }
            else if (!_saveFlags.HasFlag(SongSave.NoAlbum))
            {
                if (_song.Albums.Count > 0)
                {
                    path = $"{path}/{FileManager.Sanitize(_song.Album.Title)}";
                }
            }


            Song s;
            if (_saveFlags.HasFlag(SongSave.Title))
            {
                
                path = $"{path}/{FileManager.Sanitize(_tfile.Tag.Title)}";
                s = new Song(_song, _tfile.Tag.Title, path);

            }else
            {
                path = $"{path}/{FileManager.Sanitize(_song.Title)}";
                s = new Song(_song, _song.Title, path);
            }

            Album a = null;
            if (_saveFlags.HasFlag(SongSave.Album))
            {
                //restore albums
                var inListAlbum = MainActivity.stateHandler.Albums.Select(_tfile.Tag.Album);
                if (inListAlbum.Count > 0)
                {
                    a = inListAlbum[0];
                    a.AddSong(ref s);
                }
                else
                {
                    a = new Album(_tfile.Tag.Album, Ass_Pain.Album.GetImagePath(_tfile.Tag.Album, FileManager.Sanitize(FileManager.GetAlias(_tfile.Tag.Performers.FirstOrDefault()))));
                    a.AddSong(ref s);
                    MainActivity.stateHandler.Albums.Add(a);
                }
                s.AddAlbum(ref a);
            }
            
            if (_saveFlags.HasFlag(SongSave.Artist))
            {
                /*_song.Artists.ForEach(artist =>
                {
                    if (artist.Songs.Count == 0 && artist.Albums.Count == 0)
                    {
                        bool nuke = true;
                        artist.Albums.ForEach(album =>
                        {
                            if (album.Songs.Count == 0)
                            {
                                album.Nuke();
                            }
                            else
                            {
                                nuke = false;
                            }
                        });
                        if (nuke)
                        {
                            artist.Nuke();
                        }
                    }
                });*/
                
                //restore artists
                foreach (var performer in _tfile.Tag.Performers)
                {
                    Artist art;
                    var alias = FileManager.GetAlias(performer);
                    var inList = MainActivity.stateHandler.Artists.Select(alias);
                    if (inList.Count > 0)
                    {
                        art = inList[0];
                        art.AddSong(ref s);
                        if (_saveFlags.HasFlag(SongSave.Album))
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
                        if (_saveFlags.HasFlag(SongSave.Album))
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
                var inList = MainActivity.stateHandler.Songs.Select(s.Title);
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
                Save();
                
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

    [Flags]
    enum SongSave
    {
        None = 0,
        Title = 1,
        Artist = 2,
        Album = 4,
        NoAlbum = 8
    }
}