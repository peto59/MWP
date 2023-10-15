using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MWP
{
    public static class SongExtensions
    {
        ///<summary>
        ///Returns alphabetically ordered songs list
        ///</summary>
        public static List<Song> OrderAlphabetically( [NotNull] this List<Song> songs, bool reverse = false)
        {
            return reverse ? songs.OrderByDescending(song => song.Title).ToList() : songs.OrderBy(song => song.Title).ToList();
        }
        
        ///<summary>
        ///Returns song list ordered by date created, defaults to newest first
        ///</summary>
        public static List<Song> OrderByDate( [NotNull] this List<Song> songs, bool reverse = false)
        {
            return reverse ? songs.OrderBy(song => song.DateCreated).ToList() : songs.OrderByDescending(song => song.DateCreated).ToList();
        }
        
        public static List<Song> Order( [NotNull] this List<Song> songs, SongOrderType type)
        {
            return type switch
            {
                SongOrderType.Alphabetically => songs.OrderAlphabetically(),
                SongOrderType.AlphabeticallyReverse => songs.OrderAlphabetically(true),
                SongOrderType.ByDate => songs.OrderByDate(),
                SongOrderType.ByDateReverse => songs.OrderByDate(true),
                _ => songs
            };
        }
        
        public static IEnumerable<Song> Search( [NotNull] this List<Song> songs, string query)
        {
            return string.IsNullOrEmpty(query) ? songs : songs.Where(song => song.Title.Contains(query, StringComparison.InvariantCultureIgnoreCase));
        }
        
        public static List<Artist> Search( [NotNull] this List<Artist> artists, string query)
        {
            return string.IsNullOrEmpty(query) ? artists : artists.Where(artist => artist.Title.Contains(query, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }
        
        public static List<Album> Search( [NotNull] this List<Album> albums, string query)
        {
            return string.IsNullOrEmpty(query) ? albums : albums.Where(album  => album.Title.Contains(query, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }
        
        public static List<Song> Select( [NotNull] this List<Song> songs, string query)
        {
            return string.IsNullOrEmpty(query) ? songs : songs.Where(album  => album.Title == query).ToList();
        }
        public static List<Artist> Select( [NotNull] this List<Artist> artists, string query)
        {
            return !string.IsNullOrEmpty(query) ? artists.Where(album => album.Title == query).ToList() : artists;
        }
        
        public static List<Album> Select( [NotNull] this List<Album> albums, string query)
        {
            return string.IsNullOrEmpty(query) ? albums : albums.Where(album  => album.Title == query).ToList();
        }
    }
}