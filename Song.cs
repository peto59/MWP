using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Android.Provider;

namespace Ass_Pain
{
    public class Song
    {
        public List<Artist> Artists { get; }
        public Artist Artist
        {
            get
            {
                return Artists.Count > 0 ? Artists[0] : new Artist("No Artist");
            }
        }

        public Album Album
        {
            get
            {
                return Albums.Count > 0 ? Albums[0] : null;
            }
        }

        public List<Album> Albums { get; }
        public string Name { get; }
        public string Title
        {
            get { return Name; }
        }
        
        public DateTime DateCreated { get; }
        
        public string Path { get;}


        public void AddArtist(List<Artist> artists)
        {
            Artists.AddRange(artists);
        }
        public void AddArtist(Artist artist)
        {
            Artists.Add(artist);
        }
        
        public void AddAlbum(List<Album> albums)
        {
            Albums.AddRange(albums);
        }
        public void AddAlbum(Album album)
        {
            Albums.Add(album);
        }

        public Song(List<Artist> artists, string name, DateTime dateCreated, string path, Album album = null)
        {
            Artists = artists;
            Albums = new List<Album> {album};
            Name = name;
            DateCreated = dateCreated;
            Path = path;
        }
        public Song(Artist artist, string name, DateTime dateCreated, string path, Album album = null)
        {
            Artists = new List<Artist> {artist};
            Albums = new List<Album> {album};
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
        
        public Song(string name, DateTime dateCreated, string path)
        {
            Name = name;
            DateCreated = dateCreated;
            Path = path;
        }
        
        public override bool Equals(object obj)
        {
            var item = obj as Song;

            if (item == null)
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
            return $"Song: title> {Name} author> {Artist} album> {Album} dateCreated> {DateCreated} path> {Path}";
        }
    }
}