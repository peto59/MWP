using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ass_Pain
{
    public class MediaService : Service,
    AudioManager.IOnAudioFocusChangeListener
    {
        public void OnAudioFocusChange(AudioFocus focusChange)
        {
        }

        public override IBinder OnBind(Intent intent)
        {
            //throw new NotImplementedException();
            //Console.WriteLine("UN-Lucky");
            return null;
        }
    }
}