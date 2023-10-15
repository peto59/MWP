using System;
using Android.Content;
using Android.OS;
using MWP.Helpers;

namespace MWP
{
    public class MediaServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public MediaServiceBinder Binder { get; private set; }
        public bool Connected { get; private set; }

        public void OnServiceConnected(ComponentName? name, IBinder? binder)
        {
            // Cast the IBinder to your binder class and obtain a reference to the service instance
            MediaServiceBinder? tempBinder = (MediaServiceBinder?)binder;
            if (tempBinder != null)
            {
                Binder = tempBinder;
                MediaService? serviceInstance = Binder?.Service;
#if DEBUG
                MyConsole.WriteLine("OnServiceConnected");
#endif

                // You can now use the service instance to interact with your foreground service
                if (serviceInstance != null)
                {
                    Connected = true;
#if DEBUG
                    MyConsole.WriteLine("Service connected");
#endif
                    // Call methods or access properties of the service instance

                    //serviceInstance.Play();
                }
                
            }
        }

        public void OnServiceDisconnected(ComponentName? name)
        {
            Connected = false;
            //Binder = null;
        }

        /*public void Dispose()
        {
            Connected = false;
            Binder?.Dispose();
            Binder = null;
        }*/

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            Connected = false;
            Binder?.Dispose();
            //Binder = null;
            base.Dispose(disposing);
        }
    }
}