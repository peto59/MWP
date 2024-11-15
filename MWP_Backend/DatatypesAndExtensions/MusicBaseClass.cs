using System.Net.Mime;
using System.Reflection;

namespace MWP_Backend.DatatypesAndExtensions
{
    /// <summary>
    /// All static object related to <see cref="MusicBaseClass"/>
    /// </summary>
    public static class MusicBaseClassStatic
    {
        private static Assembly _myAssembly = Assembly.GetExecutingAssembly();
        private static byte[]? _placeholderImage;

        private static byte[] GetImage(string fileName)
        {
            using Stream? resourceStream =
                _myAssembly.GetManifestResourceStream(_myAssembly.GetName().Name + fileName);
            if (resourceStream == null)
            {
                throw new InvalidOperationException("this is null, fix me please, senpai");
            }

            using MemoryStream memoryStream = new MemoryStream();
            resourceStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
        
        /// <summary>
        /// Default image placeholder
        /// </summary>
        /// <exception cref="InvalidOperationException">Assests should not be null</exception>
        public static byte[] Placeholder => _placeholderImage ??= GetImage(".Images.music_placeholder.png");
            

        //TODO: sickoooo null
        private static byte[]? _albumsImage;

        /// <summary>
        /// Image for albums
        /// </summary>
        public static byte[] AlbumsImage =>
            _albumsImage ??= GetImage(".Images.music_placeholder.png");
        
        private static byte[]? _musicImage;

        /// <summary>
        /// Music image
        /// </summary>
        public static byte[] MusicImage =>
            _musicImage ??= GetImage(".Images.music_placeholder.png");

        private static byte[]? _playlistsImage;
        /// <summary>
        /// Image for playlists
        /// </summary>
        public static byte[] PlaylistsImage =>
            _playlistsImage ??= GetImage(".Images.music_placeholder.png");


        private static byte[]? _artistsImage;
        /// <summary>
        /// Image for artists
        /// </summary>
        public static byte[] ArtistsImage =>
            _artistsImage ??= GetImage(".Images.music_placeholder.png");

        private static byte[]? _playImage;
        /// <summary>
        /// play symbol
        /// </summary>
        public static byte[] PlayImage =>
            _playImage ??= GetImage(".Images.music_placeholder.png");

        private static byte[]? _shuffleImage;
        /// <summary>
        /// shuffle symbol
        /// </summary>
        public static byte[] ShuffleImage =>
            _shuffleImage ??= GetImage(".Images.music_placeholder.png");
    }
    /// <summary>
    /// Basic music object
    /// </summary>
    public abstract class MusicBaseClass
    {
        /// <summary>
        /// unique id
        /// </summary>
        public readonly Guid Id = Guid.NewGuid();
        /// <summary>
        /// <see cref="Id"/> as <see cref="string"/>
        /// </summary>
        public readonly string IdString;
        /// <summary>
        /// Name of object
        /// </summary>
        public abstract string Title { get; protected internal set; }
        /// <summary>
        /// Image for this object
        /// </summary>
        public byte[] Image => GetImage() ?? MusicBaseClassStatic.Placeholder;
        /// <summary>
        /// Function to load image for this object
        /// </summary>
        /// <param name="shouldFallBack">Whether to look for images in related objects</param>
        /// <returns><see cref="byte"/> for this object</returns>
        public abstract byte[]? GetImage(bool shouldFallBack = true);
        
        #if ANDROID
        private MediaDescriptionCompat.Builder? builder;
        /// <summary>
        /// Media Description Builder
        /// </summary>
        protected MediaDescriptionCompat.Builder? Builder
        {
            get
            {
                builder ??= GetBuilder();
                return builder;
            }
        }

        /// <summary>
        /// Gets Media Description Builder
        /// </summary>
        /// <returns>Media Description Builder</returns>
        protected abstract MediaDescriptionCompat.Builder? GetBuilder();

        private MediaDescriptionCompat? description;

        /// <summary>
        /// Media description
        /// </summary>
        protected MediaDescriptionCompat? Description
        {
            get
            {
                description ??= GetDescription();
                return description;
            }
        }

        /// <summary>
        /// Gets Media description
        /// </summary>
        /// <returns>Media description</returns>
        protected abstract MediaDescriptionCompat? GetDescription();
        /// <summary>
        /// Representation of this object as Media Item
        /// </summary>
        /// <returns>this object as Media Item</returns>
        public abstract MediaBrowserCompat.MediaItem? ToMediaItem();
        #endif
        
        /// <summary>
        /// Creates new instance
        /// </summary>
        protected MusicBaseClass()
        {
            IdString = Id.ToString();
        }
    }

    /// <summary>
    /// Basic datatype for music objects capable of holding <see cref="Song"/>s
    /// </summary>
    public abstract class MusicBaseContainer : MusicBaseClass
    {
        /// <summary>
        /// <see cref="Song"/>s belonging to this object
        /// </summary>
        public abstract List<Song> Songs { get; }
    }
}