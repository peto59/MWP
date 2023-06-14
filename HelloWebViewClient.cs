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
using Android.Webkit;

namespace Ass_Pain
{
    public class HelloWebViewClient : WebViewClient
    {
        // For API level 24 and later
        public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
        {
            view.LoadUrl(request.Url?.ToString()!);
            return false;
        }


    }
}