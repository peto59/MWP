using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MWP
{
    internal class Interval
    {
        public static async Task SetIntervalAsync(Action action, int delay, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(delay, token);
                    action();
                }
            }
            catch (TaskCanceledException) { }
        }

    }
}