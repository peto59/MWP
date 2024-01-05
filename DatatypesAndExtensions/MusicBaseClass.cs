using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using AndroidX.VectorDrawable.Graphics.Drawable;

namespace MWP
{
    /// <summary>
    /// All static object related to <see cref="MusicBaseClass"/>
    /// </summary>
    public static class MusicBaseClassStatic
    {
        private static Bitmap? _placeholderImage;
        /// <summary>
        /// Default image placeholder
        /// </summary>
        /// <exception cref="InvalidOperationException">Assests should not be null</exception>
        public static Bitmap Placeholder => 
            _placeholderImage ??= BitmapFactory.DecodeStream(Application.Context.Assets?.Open("music_placeholder.png")) ??
                                            throw new InvalidOperationException("this is null, fix me please, senpai");

        //TODO: sickoooo null
        private static Bitmap? _albumsImage;

        /// <summary>
        /// Image for albums
        /// </summary>
        public static Bitmap AlbumsImage =>
            _albumsImage ??=
                BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.albums) ??
                Placeholder;
        
        private static Bitmap? _musicImage;

        /// <summary>
        /// Music image
        /// </summary>
        public static Bitmap MusicImage =>
            _musicImage ??=
                BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.music) ??
                Placeholder;

        private static Bitmap? _playlistsImage;
        /// <summary>
        /// Image for playlists
        /// </summary>
        public static Bitmap PlaylistsImage =>
            _playlistsImage ??= BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.playlists) ??
                                Placeholder;


        private static Bitmap? _artistsImage;
        /// <summary>
        /// Image for artists
        /// </summary>
        public static Bitmap ArtistsImage =>
            _artistsImage ??= BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.artists) ??
                              Placeholder;

        private static Bitmap? _playImage;
        /// <summary>
        /// play symbol
        /// </summary>
        public static Bitmap PlayImage =>
            _playImage ??= BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.play) ??
                           Placeholder;

        private static Bitmap? _shuffleImage;
        /// <summary>
        /// shuffle symbol
        /// </summary>
        public static Bitmap ShuffleImage =>
            _shuffleImage ??= BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.shuffle2) ??
                              Placeholder;
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
        public abstract string Title { get; }
        /// <summary>
        /// Image for this object
        /// </summary>
        public Bitmap Image => GetImage() ?? MusicBaseClassStatic.Placeholder;
        /// <summary>
        /// Function to load image for this object
        /// </summary>
        /// <param name="shouldFallBack">Whether to look for images in related objects</param>
        /// <returns><see cref="Bitmap"/> for this obejct</returns>
        public abstract Bitmap? GetImage(bool shouldFallBack = true);

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
        
        /// <summary>
        /// Creates new instance
        /// </summary>
        protected MusicBaseClass()
        {
            IdString = Id.ToString();
        }
    }

    /// <summary>
    /// Basic datatype for music objects capable of holding <see cref="MWP.Song"/>s
    /// </summary>
    public abstract class MusicBaseContainer : MusicBaseClass
    {
        /// <summary>
        /// <see cref="MWP.Song"/>s belonging to this object
        /// </summary>
        public abstract List<Song> Songs { get; }
    }
}