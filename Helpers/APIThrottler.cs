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

        public async Task<(string title, string recordingId, string trackId, List<(string title, string id)> artist, List<(string title, string id)> releaseGroup, string thumbnailUrl)> Throttle(string path, string originalAuthor, string originalTitle) {
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
            (string title, string recordingId, string trackId, List<(string title, string id)> artists, List<(string title, string id)> releaseGroup, string thumbnailUrl) =  await Downloader.GetMusicBrainzIdFromFingerprint(path, originalAuthor, originalTitle);
            running = false;
            return (title, recordingId, trackId, artists, releaseGroup, thumbnailUrl);
        }
    }
}