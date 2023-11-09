using System;

namespace MWP
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

    public enum MediaType : byte
    {
        Song = 0,
        Album = 1,
        Artist = 2
    }
    
}