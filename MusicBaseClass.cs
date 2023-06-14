using Android.Graphics;

namespace Ass_Pain
{
    public abstract class MusicBaseClass
    {
        public string Title;
        public Bitmap Image;
        public abstract Bitmap GetImage(bool shouldFallBack = true);
    }
}