using Android.OS;

namespace Ass_Pain
{
    public class MediaServiceBinder : Binder
    {
        public MediaService Service { get; private set; }

        public MediaServiceBinder(MediaService service)
        {
            Service = service;
        }   
        
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            Service.Dispose();
            base.Dispose(disposing);
        }
    }
}