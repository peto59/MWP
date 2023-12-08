using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Android.App;
using Android.Graphics;
using Android.Support.V4.Media;

namespace MWP
{
    public static class MusicBaseClassStatic
    {
        public static Bitmap placeholder => BitmapFactory.DecodeStream(Application.Context.Assets?.Open("music_placeholder.png")) ?? throw new InvalidOperationException();
    }
    public abstract class MusicBaseClass
    {
        public readonly Guid Id = Guid.NewGuid();
        public abstract string Title { get; }
        public Bitmap Image => GetImage() ?? placeholder ?? throw new InvalidOperationException();
        public abstract Bitmap? GetImage(bool shouldFallBack = true);
        protected static Bitmap placeholder = MusicBaseClassStatic.placeholder;

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

        protected MediaDescriptionCompat? _description;
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
    }

    public abstract class MusicBaseContainer : MusicBaseClass
    {
        public abstract List<Song> Songs { get; }
    }
}