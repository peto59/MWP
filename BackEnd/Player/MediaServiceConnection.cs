using Android.Content;
using Android.OS;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Player
{
    /// <summary>
    /// Connector to <see cref="MediaService"/>
    /// </summary>
    public class MediaServiceConnection : Java.Lang.Object, IServiceConnection
    {
        /// <summary>
        /// Binder to <see cref="MediaService"/>
        /// </summary>
        public MediaServiceBinder? Binder { get; private set; }
        /// <summary>
        /// Whether service is currently connected to <see cref="Binder"/>
        /// </summary>
        public bool Connected { get; private set; }

        /// <inheritdoc />
        public void OnServiceConnected(ComponentName? name, IBinder? binder)
        {
            // Cast the IBinder to your binder class and obtain a reference to the service instance
            MediaServiceBinder? tempBinder = (MediaServiceBinder?)binder;
            if (tempBinder == null) return;
            Binder = tempBinder;
#if DEBUG
            MyConsole.WriteLine("OnServiceConnected");
#endif
            
            if (Binder?.Service == null) return;
            Connected = true;
#if DEBUG
            MyConsole.WriteLine("Service connected");
#endif
        }

        /// <inheritdoc />
        public void OnServiceDisconnected(ComponentName? name)
        {
            Connected = false;
            Binder = null;
        }

        /// <inheritdoc />
        public new void Dispose()
        {
            Dispose(true);
            base.Dispose();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            Connected = false;
            Binder?.Dispose(this);
            //Binder = null;
            base.Dispose(disposing);
        }
    }
}