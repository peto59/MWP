using Android.App;
using Android.Content;
using Android.Media;
using MWP.Helpers;
using AndroidApp = Android.App.Application;
#if DEBUG
#endif

namespace MWP
{
    /// <inheritdoc />
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { AudioManager.ActionAudioBecomingNoisy })]
    public class MyBroadcastReceiver :  BroadcastReceiver
    {
        /// <inheritdoc />
        public override void OnReceive(Context? context, Intent? intent)
        {
#if DEBUG
            MyConsole.WriteLine("noisy");
#endif
            context?.StartService(new Intent(MediaService.ActionPause, null, context, typeof(MediaService)));
            //player.Pause();
        }
    }
}