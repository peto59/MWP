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
    /// <summary>
    /// Class to throttle function calls
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public  class APIThrottler : DelegatingHandler
    {

        private Dictionary<string, (long milliseconds, bool running, int wait, int preWait)> signatures = new Dictionary<string, (long, bool, int, int)>();
        
        /// <summary>
        /// Throttles given function <paramref name="func"/> to call every <paramref name="wait"/> milliseconds
        /// </summary>
        /// <param name="func">Function to be executed</param>
        /// <param name="id">Differentiator between mutltiple call queues</param>
        /// <param name="wait">time to wait between calls in milliseconds, subsequent calls with same <paramref name="id"/> ignore this value</param>
        /// <param name="preWait">time to wait before first call in milliseconds, subsequent calls with same <paramref name="id"/> ignore this value</param>
        /// <typeparam name="T">any type</typeparam>
        /// <returns>Awaitable Task T</returns>
        public async Task<T> Throttle<T>(Func<Task<T>> func, string id = "Default", int wait = 1500, int preWait = 0) {
            signatures.TryAdd(id, (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond, false, wait, preWait));

            (long milliseconds, bool running, int wait, int preWait) instance = signatures[id];
            await Task.Delay(instance.preWait);
            instance.preWait = 0;
            
            while(instance.running)
            {
                await Task.Delay(1000);
            }
            instance.running = true;
            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if(now < instance.milliseconds+instance.wait) {
                await Task.Delay(1000);
            }
            instance.milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            T output = await func();
            instance.running = false;
            return output;
        }
        
        /// <summary>
        /// Throttles given function <paramref name="func"/> to call every <paramref name="wait"/> milliseconds
        /// </summary>
        /// <param name="func">Function to be executed</param>
        /// <param name="id">Differentiator between mutltiple call queues</param>
        /// <param name="wait">time to wait between calls, subsequent calls with same <paramref name="id"/> ignore this value</param>
        /// <param name="preWait">time to wait before first call, subsequent calls with same <paramref name="id"/> ignore this value</param>
        /// <typeparam name="T">any type</typeparam>
        /// <returns>Awaitable Task T</returns>
        public async Task<T> Throttle<T>(Func<T> func, string id = "Default", int wait = 1500, int preWait = 0) {
            signatures.TryAdd(id, (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond, false, wait, preWait));

            (long milliseconds, bool running, int wait, int preWait) instance = signatures[id];
            await Task.Delay(instance.preWait);
            instance.preWait = 0;
            
            while(instance.running)
            {
                await Task.Delay(1000);
            }
            instance.running = true;
            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if(now < instance.milliseconds+instance.wait) {
                await Task.Delay(1000);
            }
            instance.milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            T output = func();
            instance.running = false;
            return output;
        }
    }
}