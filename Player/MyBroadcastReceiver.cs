using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AndroidApp = Android.App.Application;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { AudioManager.ActionAudioBecomingNoisy })]
    public class MyBroadcastReceiver :  BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Console.WriteLine("noisy");
            context.StartService(new Intent(MediaService.ActionPause, null, context, typeof(MediaService)));
            //player.Pause();
        }
    }
}