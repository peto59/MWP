using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Android.Graphics;

namespace Ass_Pain
{
    public abstract class MusicBaseClass
    {
        public abstract string Title { get; }
        public abstract Bitmap Image { get; }
        public abstract Bitmap GetImage(bool shouldFallBack = true);
    }

    public abstract class MusicBaseContainer : MusicBaseClass
    {
        public abstract List<Song> Songs { get; }
    }
}