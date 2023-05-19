using System;
using System.Collections.Generic;

namespace Ass_Pain
{
    public class Album
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
        public List<Artist> Artists { get; }
        public Artist Artist
        {
            get
            {
                return Artists.Count > 0 ? Artists[0] : new Artist("No Artist");
            }
        }
        
        public void AddArtist(List<Artist> artists)
        {
            Artists.AddRange(artists);
        }
        public void AddArtist(Artist artist)
        {
            Artists.Add(artist);
        }
        
        public void AddSong(List<Song> songs)
        {
            Songs.AddRange(songs);
        }
        public void AddSong(Song song)
        {
            Songs.Add(song);
        }

        public Album(string title, Song song, Artist artist)
        {
            Title = title;
            Songs = new List<Song> {song};
            Artists = new List<Artist> {artist};
        }
        
        public Album(string title, List<Song> song, Artist artist)
        {
            Title = title;
            Songs = song;
            Artists = new List<Artist> {artist};
        }
        
        public Album(string title, Song song, List<Artist> artist)
        {
            Title = title;
            Songs = new List<Song> {song};
            Artists = artist;
        }
        
        public Album(string title, List<Song> song, List<Artist> artist)
        {
            Title = title;
            Songs = song;
            Artists = artist;
        }
        
        public Album(string title)
        {
            Title = title;
        }
        
        public override bool Equals(object obj)
        {
            var item = obj as Album;

            if (item == null)
            {
                return false;
            }
            
            return Equals(item);
        }

        protected bool Equals(Album other)
        {
            return Title == other.Title && Equals(Songs, other.Songs) && Equals(Artists, other.Artists);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Title, Songs, Artists);
        }
        
        public override string ToString()
        {
            return $"Album: title> {Title} song> {Song} artist> {Artist}";
        }
    }
}