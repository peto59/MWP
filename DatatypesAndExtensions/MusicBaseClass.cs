using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Android.App;
using Android.Graphics;

namespace Ass_Pain
{
    public static class MusicBaseClassStatic
    {
        public static Bitmap placeholder => BitmapFactory.DecodeStream(Application.Context.Assets?.Open("music_placeholder.png")) ?? throw new InvalidOperationException();
    }
    public abstract class MusicBaseClass
    {
        public abstract string Title { get; }
        public Bitmap Image => GetImage() ?? placeholder ?? throw new InvalidOperationException();
        public abstract Bitmap? GetImage(bool shouldFallBack = true);
        protected static Bitmap placeholder = MusicBaseClassStatic.placeholder;
    }

    public abstract class MusicBaseContainer : MusicBaseClass
    {
        public abstract List<Song> Songs { get; }
    }
}