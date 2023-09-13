using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Content.Res;
using Android.Graphics;
using Fragment = AndroidX.Fragment.App.Fragment;
using Orientation = Android.Widget.Orientation;

namespace Ass_Pain
{
    /// <inheritdoc />
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class ShareFragment : Fragment
    {
        private readonly Context context;
        private RelativeLayout? mainLayout;
        private Typeface? font;
        private View? view;
        
        
        /// <inheritdoc />
        public override View? OnCreateView(LayoutInflater? inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            view = inflater?.Inflate(Resource.Layout.share_fragment, container, false);
            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.share_fragment_main);
            
            RenderUi();
            
            return view;
        }


        /// <inheritdoc />
        public ShareFragment(Context ctx, AssetManager assets)
        {
            context = ctx;
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
        }


        private void RenderUi()
        {
            TextView? remoteListeningLabel = view?.FindViewById<TextView>(Resource.Id.remote_listening_label);
            TextView? remoteConnectionsPortLabel = view?.FindViewById<TextView>(Resource.Id.remote_connections_port_label);
            TextView? remoteConnectionsPort = view?.FindViewById<TextView>(Resource.Id.remote_connections_port);
            TextView? trustedNetworkLabel = view?.FindViewById<TextView>(Resource.Id.trusted_network_label);
            TextView? addTrustedNetworkButton = view?.FindViewById<TextView>(Resource.Id.add_trusted_network_button);

            
            if (remoteListeningLabel != null) remoteListeningLabel.Typeface = font;
            if (remoteConnectionsPortLabel != null) remoteConnectionsPortLabel.Typeface = font;
            if (remoteConnectionsPort != null) remoteConnectionsPort.Typeface = font;
            if (trustedNetworkLabel != null) trustedNetworkLabel.Typeface = font;
            if (addTrustedNetworkButton != null) addTrustedNetworkButton.Typeface = font;



            LinearLayout? trustedNetworkList = view?.FindViewById<LinearLayout>(Resource.Id.trusted_network_list);
         
            trustedNetworkList?.AddView(TrustedNetworkTile("Synology"));
        }

        private LinearLayout TrustedNetworkTile(string text)
        {
            LinearLayout tile = new LinearLayout(context);
            LinearLayout.LayoutParams tileParams =  new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            tileParams.SetMargins(5, 5, 5, 5);
            tile.LayoutParameters = tileParams;
            tile.SetPadding(0, 15, 0,15);
            tile.Orientation = Orientation.Horizontal;
            tile.SetBackgroundResource(Resource.Drawable.rounded_dark);

            TextView networkName = new TextView(context);
            LinearLayout.LayoutParams networkNameParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            networkNameParams.SetMargins(10, 0, 0, 0);
            networkName.LayoutParameters = networkNameParams;
            networkName.Text = text;
            networkName.SetTextColor(Color.White);
            networkName.TextSize = 15;
            
            tile.AddView(networkName);
            
            return tile;
        }
        
        
    }
}