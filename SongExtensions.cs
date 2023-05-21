using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ass_Pain
{
    public static class SongExtensions
    {
        ///<summary>
        ///Returns alphabetically ordered songs list
        ///</summary>
        public static List<Song> OrderAlphabetically( [NotNull] this List<Song> songs, bool reverse = false)
        {
            if (reverse)
            {
                return songs.OrderByDescending(song => song.Name).ToList();
            }
            return songs.OrderBy(song => song.Name).ToList();
        }
        
        ///<summary>
        ///Returns song list ordered by date created, defaults to newest first
        ///</summary>
        public static List<Song> OrderByDate( [NotNull] this List<Song> songs, bool reverse = false)
        {
            if (!reverse)
            {
                return songs.OrderByDescending(song => song.DateCreated).ToList();
            }
            return songs.OrderBy(song => song.DateCreated).ToList();
        }
        
        public static List<Song> Order( [NotNull] this List<Song> songs, SongOrderType type)
        {
            switch (type)
            {
                case SongOrderType.Alphabetically:
                    return songs.OrderAlphabetically();
                case SongOrderType.AlphabeticallyReverse:
                    return songs.OrderAlphabetically(true);
                case SongOrderType.ByDate:
                    return songs.OrderByDate();
                case SongOrderType.ByDateReverse:
                    return songs.OrderByDate(true);
                default: return songs;
            }
        }
        
        public static IEnumerable<Song> Search( [NotNull] this List<Song> songs, string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return songs;
            }
            return songs.Where(song => song.Title.Contains(query, StringComparison.InvariantCultureIgnoreCase));  
        }
        
        public static List<Artist> Search( [NotNull] this List<Artist> artists, string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return artists;
            }
            return artists.Where(artist => artist.Title.Contains(query, StringComparison.InvariantCultureIgnoreCase)).ToList();  
        }
        
        public static List<Album> Search( [NotNull] this List<Album> albums, string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return albums;
            }
            return albums.Where(album  => album.Title.Contains(query, StringComparison.InvariantCultureIgnoreCase)).ToList();  
        }
    }

    public enum SongOrderType: byte
    {
        Alphabetically = 1,
        AlphabeticallyReverse = 2,
        ByDate = 3,
        ByDateReverse = 4
    }
}