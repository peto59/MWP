using System;

namespace MWP.DatatypesAndExtensions
{
    internal enum SongSelectionDialogActions
    {
        None,
        Cancel,
        Accept,
        Next,
        Previous
    }

    internal enum LastSongSelectionNavigation
    {
        None,
        Next,
        Previous
    }
    
    internal enum DownloadActions
    {
        DownloadOnly,
        DownloadWithMbid,
        Downloadmp4
    }
    
    /// <summary>
    /// Determines in which order songs are ordered
    /// </summary>
    public enum SongOrderType
    {
        /// <summary>
        /// Orders songs alphabetically
        /// </summary>
        Alphabetically,
        /// <summary>
        /// Orders songs alphabetically in reversed order
        /// </summary>
        AlphabeticallyReverse,
        /// <summary>
        /// Orders songs by date added
        /// </summary>
        ByDate,
        /// <summary>
        /// Orders songs by date added in reversed order
        /// </summary>
        ByDateReverse
    }
    
    [Flags]
    internal enum SongSave
    {
        None,
        Title,
        Artist,
        Album,
        NoAlbum
    }

    internal enum AutoUpdate
    {
        Requested,
        Forbidden,
        NoState
    }

    internal enum CanUseNetworkState
    {
        None,
        Allowed,
        Rejected
    }

    /// <summary>
    /// Media types for <see cref="MWP.BackEnd.Player.MyMediaBrowserService"/>
    /// </summary>
    public enum MediaType : byte
    {
        /// <summary>
        /// Song
        /// </summary>
        Song = 0,
        /// <summary>
        /// Album
        /// </summary>
        Album = 1,
        /// <summary>
        /// Artist
        /// </summary>
        Artist = 2,
        /// <summary>
        /// Play all <see cref="MWP.Song"/>s in this object
        /// </summary>
        ThisPlayAll = 3,
        /// <summary>
        /// Play all <see cref="MWP.Song"/>s in this object on shuffle
        /// </summary>
        ThisShufflePlay = 4
    }
    
}