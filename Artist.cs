using System;
using System.Collections.Generic;
using TagLib.Riff;

namespace Ass_Pain
{
    public class Artist
    {
        public string Title { get; }

        public List<Song> Songs { get; }
        public Song Song
        {
            get
            {
                return Songs.Count > 0 ? Songs[0] : null;
            }
        }
        public List<Album> Albums { get; }
        public Album Album
        {
            get
            {
                return Albums.Count > 0 ? Albums[0] : null;
            }
        }
        
        public void AddAlbum(List<Album> albums)
        {
            Albums.AddRange(albums);
        }
        public void AddAlbum(Album album)
        {
            Albums.Add(album);
        }
        
        public void AddSong(List<Song> songs)
        {
            Songs.AddRange(songs);
        }
        public void AddSong(Song song)
        {
            Songs.Add(song);
        }
        

        public Artist(string title, Song song, Album album)
        {
            Title = title;
            Songs = new List<Song> {song};
            Albums = new List<Album> {album};
        }
        
        public Artist(string title, List<Song> song, Album album)
        {
            Title = title;
            Songs = song;
            Albums = new List<Album> {album};
        }
        
        public Artist(string title, Song song, List<Album> album)
        {
            Title = title;
            Songs = new List<Song> {song};
            Albums = album;
        }
        
        public Artist(string title, List<Song> song, List<Album> album)
        {
            Title = title;
            Songs = song;
            Albums = album;
        }
        
        public Artist(string title)
        {
            Title = title;
        }

        public override bool Equals(object obj)
        {
            var item = obj as Artist;

            if (item == null)
            {
                return false;
            }
            
            return Equals(item);
        }

        protected bool Equals(Artist other)
        {
            return Title == other.Title && Equals(Songs, other.Songs) && Equals(Albums, other.Albums);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Title, Songs, Albums);
        }

        public override string ToString()
        {
            return $"Artist: title> {Title} song> {Song} album> {Album}";
        }
    }
}