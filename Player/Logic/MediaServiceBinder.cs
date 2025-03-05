#if ANDROID
using Android.Content;
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

        public void Dispose(IServiceConnection conn)
        {
            Service.UnbindService(conn);
            Service.Dispose();
            Dispose(true);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
#endif