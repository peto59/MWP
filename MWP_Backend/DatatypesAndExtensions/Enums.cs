using System;
using MWP_Backend.DatatypesAndExtensions;

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
    
    public enum DownloadActions
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
        None = 0,
        Title = 1,
        Artist = 2,
        Album = 4,
        NoAlbum = 8,
        Image = 16
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
        /// Play all <see cref="MWP_Backend.DatatypesAndExtensions.Song"/>s in this object
        /// </summary>
        ThisPlayAll = 3,
        /// <summary>
        /// Play all <see cref="MWP_Backend.DatatypesAndExtensions.Song"/>s in this object on shuffle
        /// </summary>
        ThisShufflePlay = 4
    }

    internal enum UseChromaprint
    {
        None,
        No,
        Manual,
        Automatic
    } 
    
    internal enum MoveFilesEnum
    {
        None,
        No,
        Yes
    } 
    
}