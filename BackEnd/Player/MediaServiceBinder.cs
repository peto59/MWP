using Android.OS;

namespace MWP.BackEnd.Player
{
    /// <summary>
    /// Binder to <see cref="MediaService"/>
    /// </summary>
    public class MediaServiceBinder : Binder
    {
        /// <summary>
        /// <see cref="MediaService"/>
        /// </summary>
        public MediaService Service { get; }

        /// <inheritdoc />
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