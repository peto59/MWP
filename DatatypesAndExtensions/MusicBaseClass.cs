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
    public static class MusicBaseClassStatic
    {
        private static Bitmap? _placeholderImage;
        public static Bitmap Placeholder => 
            _placeholderImage ??= BitmapFactory.DecodeStream(Application.Context.Assets?.Open("music_placeholder.png")) ??
                                            throw new InvalidOperationException("this is null, fix me please, senpai");

        //TODO: sickoooo null
        private static Bitmap? _albumsImage;

        public static Bitmap AlbumsImage =>
            _albumsImage ??=
                BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.albums) ??
                Placeholder;
        
        private static Bitmap? _musicImage;

        public static Bitmap MusicImage =>
            _musicImage ??=
                BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.music) ??
                Placeholder;

        private static Bitmap? _playlistsImage;
        public static Bitmap PlaylistsImage =>
            _playlistsImage ??= BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.playlists) ??
                                Placeholder;


        private static Bitmap? _artistsImage;
        public static Bitmap ArtistsImage =>
            _artistsImage ??= BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.artists) ??
                              Placeholder;

        private static Bitmap? _playImage;
        public static Bitmap PlayImage =>
            _playImage ??= BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.play) ??
                           Placeholder;

        private static Bitmap? _shuffleImage;
        public static Bitmap ShuffleImage =>
            _shuffleImage ??= BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.shuffle2) ??
                              Placeholder;
    }
    public abstract class MusicBaseClass
    {
        public readonly Guid Id = Guid.NewGuid();
        public readonly string IdString;
        public abstract string Title { get; }
        public Bitmap Image => GetImage() ?? placeholder ?? throw new InvalidOperationException();
        public abstract Bitmap? GetImage(bool shouldFallBack = true);
        protected static Bitmap? placeholder = MusicBaseClassStatic.Placeholder;

        protected MediaDescriptionCompat.Builder? _builder;
        protected MediaDescriptionCompat.Builder? Builder
        {
            get
            {
                _builder ??= GetBuilder();
                return _builder;
            }
        }

        protected abstract MediaDescriptionCompat.Builder? GetBuilder();

        private MediaDescriptionCompat? _description;

        protected MediaDescriptionCompat? Description
        {
            get
            {
                _description ??= GetDescription();
                return _description;
            }
        }

        protected abstract MediaDescriptionCompat? GetDescription();
        public abstract MediaBrowserCompat.MediaItem? ToMediaItem();
        //protected abstract string ToMediaId();
        
        protected MusicBaseClass()
        {
            IdString = Id.ToString();
        }
    }

    public abstract class MusicBaseContainer : MusicBaseClass
    {
        public abstract List<Song> Songs { get; }
    }
}