using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ass_Pain
{
    public  class APIThrottler : DelegatingHandler
    {
        long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        bool running = false;

        public async Task<(string, string, string, byte[])> Throttle(List<string> arguments) {
            while(running)
            {
                await Task.Delay(1000);
            }
            running = true;
            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if(now < milliseconds+1500) {
                await Task.Delay(1000);
            }
            milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            (string tmp1, string tmp2, string tmp3, byte[] tmp4) =  await Downloader.SearchAPI(arguments[0], arguments[1], arguments[2]);
            running = false;
            return (tmp1, tmp2, tmp3, tmp4);
        }
    }
}