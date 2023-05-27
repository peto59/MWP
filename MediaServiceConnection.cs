using Android.Content;
using Android.OS;

namespace Ass_Pain
{
    public class MediaServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public MediaServiceBinder Binder { get; private set; }

        public void OnServiceConnected(ComponentName name, IBinder binder)
        {
            // Cast the IBinder to your binder class and obtain a reference to the service instance
            Binder = binder as MediaServiceBinder;
            var serviceInstance = Binder?.Service;

            // You can now use the service instance to interact with your foreground service
            if (serviceInstance != null)
            {
                // Call methods or access properties of the service instance
                
                //serviceInstance.DoSomething();
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            Binder = null;
        }
    }
}