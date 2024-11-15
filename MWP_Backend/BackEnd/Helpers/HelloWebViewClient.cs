#if ANDROID
using Android.Webkit;
#endif
namespace MWP
{
#if ANDROID
    internal class HelloWebViewClient : WebViewClient
    {
        // For API level 24 and later
        /// <inheritdoc />
        public override bool ShouldOverrideUrlLoading(WebView? view, IWebResourceRequest? request)
        {
            if (request is { Url: not null }) view?.LoadUrl(request.Url.ToString() ?? string.Empty);
            return false;
        }


    }
#endif
}