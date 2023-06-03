using System;
using System.Collections.Generic;
using Android.Graphics;
using System.IO;
using System.Linq;

namespace Ass_Pain
{
    public class Song
    {
        public List<Artist> Artists { get; } = new List<Artist>();
        public Artist Artist
        {
            get
            {
                return Artists.Count > 0 ? Artists[0] : new Artist("No Artist", "Default", false);
            }
        }

        public Album Album
        {
            get
            {
                return Albums.Count > 0 ? Albums[0] : new Album("No Album", "Default", false);
            }
        }

        public List<Album> Albums { get; } = new List<Album>();
        public string Name { get; }
        public string Title
        {
            get { return Name; }
        }
        
        public DateTime DateCreated { get; }
        
        public string Path { get; }
        public bool Initialized { get; private set; } = true;
        
        public Bitmap Image
        {
            get { return GetImage(); }
        }

        public void AddArtist(ref List<Artist> artists)
        {
            foreach (Artist artist in artists.Where(artist => !Artists.Contains(artist)))
            {
                Artists.Add(artist);
            }
        }
        public void AddArtist(ref Artist artist)
        {
            if(!Artists.Contains(artist))
                Artists.Add(artist);
        }
        
        public void AddAlbum(ref List<Album> albums)
        {
            foreach (Album album in albums.Where(album => !Albums.Contains(album)))
            {
                Albums.Add(album);
            }
        }
        public void AddAlbum(ref Album album)
        {
            if (!Albums.Contains(album))
                Albums.Add(album);
        }
        
        public void RemoveAlbum(Album album)
        {
            Albums.Remove(album);
        }
        
        public void RemoveAlbum(List<Album> albums)
        {
            albums.ForEach(RemoveAlbum);
        }
        
        public void RemoveArtist(Artist artist)
        {
            Artists.Remove(artist);
        }
        
        public void RemoveArtist(List<Artist> artists)
        {
            artists.ForEach(RemoveArtist);
        }

        ///<summary>
        ///Nukes this object out of existence
        ///</summary>
        public void Nuke()
        {
            Albums.ForEach(album =>
            {
                album.RemoveSong(this);
                if (album.Songs.Count == 0)
                {
                    album.Nuke();
                }

            });
            
            Artists.ForEach(artist =>
            {
                artist.RemoveSong(this);
                if (artist.Songs.Count == 0 && artist.Albums.Sum(album => album.Songs.Count) == 0)
                {
                    artist.Nuke();
                }
            });
            
            MainActivity.stateHandler.Songs.Remove(this);
            Initialized = false;
        }

        public Bitmap GetImage(bool shouldFallBack = true)
        {
            Bitmap image = null;
            try
            {
                using TagLib.File tagFile = TagLib.File.Create(Path);
                using MemoryStream ms = new MemoryStream(tagFile.Tag.Pictures[0].Data.Data);
                image = BitmapFactory.DecodeStream(ms);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine($"Doesnt contain image: {Path}");
            }

            if (image != null || !shouldFallBack) return image;
            foreach (Album album in Albums.Where(album => album.Initialized))
            {
                image = album.GetImage(false);
                if (image != null)
                {
                    break;
                }
            }

            if (image != null) return image;
            foreach (Artist artist in Artists.Where(artist => artist.Initialized))
            {
                image = artist.GetImage(false);
                if (image != null)
                {
                    break;
                }
            }

            return image;
        }

        public Song(List<Artist> artists, string name, DateTime dateCreated, string path, Album album = null)
        {
            Artists = artists;
            if (album != null)
            {
                Albums = new List<Album> {album};
            }
            Name = name;
            DateCreated = dateCreated;
            Path = path;
        }
        public Song(Artist artist, string name, DateTime dateCreated, string path, Album album = null)
        {
            Artists = new List<Artist> {artist};
            if (album != null)
            {
                Albums = new List<Album> {album};
            }
            Name = name;
            DateCreated = dateCreated;
            Path = path;
        }
        
        public Song(List<Artist> artists, string name, DateTime dateCreated, string path, List<Album> albums)
        {
            Artists = artists;
            Albums = albums;
            Name = name;
            DateCreated = dateCreated;
            Path = path;
        }
        public Song(Artist artist, string name, DateTime dateCreated, string path, List<Album> albums)
        {
            Artists = new List<Artist> {artist};
            Albums = albums;
            Name = name;
            DateCreated = dateCreated;
            Path = path;
        }
        
        public Song(string name, DateTime dateCreated, string path, bool initialized = true)
        {
            Name = name;
            DateCreated = dateCreated;
            Path = path;
            Initialized = initialized;
        }

        public Song(Song song, string name)
        {
            Artists = song.Artists;
            Albums = song.Albums;
            Name = name;
            DateCreated = song.DateCreated;
            Path = song.Path;
        }
        
        public Song(Song song, string name, string path)
        {
            Artists = song.Artists;
            Albums = song.Albums;
            Name = name;
            DateCreated = song.DateCreated;
            Path = path;
        }
        
        public override bool Equals(object obj)
        {
            if (!(obj is Song item))
            {
                return false;
            }
            
            return Equals(item);
        }
        
        protected bool Equals(Song other)
        {
            return Equals(Artists, other.Artists) && Equals(Albums, other.Albums) && Name == other.Name && DateCreated.Equals(other.DateCreated) && Path == other.Path;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Artists, Albums, Name, DateCreated, Path);
        }
        
        public override string ToString()
        {
            //$"Song: title> {Name} author> {Artist.Title} album> {Album.Title} dateCreated> {DateCreated} path> {Path}"
            /*string x = $"Song: title> {Name}";
            x = $"{x} author> {Artist.Title}";
            x = $"{x} album> {Album.Title}";
            x = $"{x} dateCreated> {DateCreated}";
            x = $"{x} path> {Path}";
            return x;*/
            return $"Song: title> {Name} author> {Artist.Title} album> {Album.Title} dateCreated> {DateCreated} path> {Path}";
        }
    }
}