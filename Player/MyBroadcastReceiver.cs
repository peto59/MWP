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

namespace Ass_Pain
{
    [BroadcastReceiver(Enabled = false, Exported = false)]
    [IntentFilter(new[] { AudioManager.ActionAudioBecomingNoisy })]
    public class MyBroadcastReceiver :  BroadcastReceiver
    {
        AppCompatActivity view;
        public MyBroadcastReceiver()
        {
            throw new NotImplementedException();
        }
        public MyBroadcastReceiver(AppCompatActivity new_view) { 
            view = new_view;
        }
        public override void OnReceive(Context context, Intent intent)
        {
            Console.WriteLine("noisy");
            view.StartService(new Intent(MediaService.ActionPause, null, view, typeof(MediaService)));
            //player.Pause();
        }
    }
}