using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Net;
using Android.Text;
using AndroidX.AppCompat.Widget;
using Ass_Pain.BackEnd;
using Ass_Pain.BackEnd.Network;
using TagLib.Tiff.Arw;
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
        private float scale;
        private View? view;
        private AssetManager assets;

        private enum ShareActionType
        {
            TrustedNetworkAdd,
            TrustedNetworkDelete,
            AvailableHostAdd,
            AvailableHostDelete,
        };
        
        /// <inheritdoc />
        public override View? OnCreateView(LayoutInflater? inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            view = inflater?.Inflate(Resource.Layout.share_fragment, container, false);
            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.share_fragment_main);
            
            RenderUi();
            
            Console.WriteLine(NetworkManager.GetAllHosts());
            
            return view;
        }


        /// <inheritdoc />
        public ShareFragment(Context ctx, AssetManager assets)
        {
            context = ctx;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
            this.assets = assets;
        }


        private void RenderUi()
        {
            TextView? remoteListeningLabel = view?.FindViewById<TextView>(Resource.Id.remote_listening_label);
            TextView? remoteConnectionsPortLabel = view?.FindViewById<TextView>(Resource.Id.remote_connections_port_label);
            TextView? remoteConnectionsPort = view?.FindViewById<TextView>(Resource.Id.remote_connections_port);
            TextView? trustedNetworkLabel = view?.FindViewById<TextView>(Resource.Id.trusted_network_label);
            TextView? addTrustedNetworkButton = view?.FindViewById<TextView>(Resource.Id.add_trusted_network_button);
            TextView? availableHostsLabel = view?.FindViewById<TextView>(Resource.Id.available_hosts_label);

            if (availableHostsLabel != null) availableHostsLabel.Typeface = font;
            if (remoteListeningLabel != null) remoteListeningLabel.Typeface = font;
            if (remoteConnectionsPortLabel != null) remoteConnectionsPortLabel.Typeface = font;
            if (remoteConnectionsPort != null) remoteConnectionsPort.Typeface = font;
            if (trustedNetworkLabel != null) trustedNetworkLabel.Typeface = font;
            if (addTrustedNetworkButton != null) addTrustedNetworkButton.Typeface = font;

            if (addTrustedNetworkButton != null)
                addTrustedNetworkButton.Click += delegate { AreYouSure(ShareActionType.TrustedNetworkAdd); };

            List<string> trustedSsids = FileManager.GetTrustedSsids();
            LinearLayout? trustedNetworkList = view?.FindViewById<LinearLayout>(Resource.Id.trusted_network_list);
            for (int i = 0; i < trustedSsids.Count; i++)
                trustedNetworkList?.AddView(TrustedNetworkTile(trustedSsids[i]));

            
            List<(string hostname, DateTime? lastSeen, bool state)> allHosts = NetworkManager.GetAllHosts();
            LinearLayout? availableHostsList = view?.FindViewById<LinearLayout>(Resource.Id.available_hosts_list);
            foreach (var (hostname, lastSeen, state) in allHosts)
                availableHostsList?.AddView(AvailableHostsTile(hostname, lastSeen, state));
            
            
            /*
             * Remote listening switch
             */
            SwitchCompat? remoteListeningSwitch = view?.FindViewById<SwitchCompat>(Resource.Id.remote_listening_switch);
            if (remoteListeningSwitch != null)
                remoteListeningSwitch.CheckedChange +=
                    delegate(object _, CompoundButton.CheckedChangeEventArgs args)
                    {
                        SettingsManager.CanUseWan = args.IsChecked;
                    };
        }

        
        
        
        private LinearLayout AvailableHostsTile(string host, DateTime? lastSeen, bool state)
        {
            LinearLayout tile = new LinearLayout(context);
            LinearLayout.LayoutParams tileParams =  new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            tileParams.SetMargins((int)(5 * scale + 0.5f), (int)(5 * scale + 0.5f), (int)(5 * scale + 0.5f), (int)(5 * scale + 0.5f));
            tile.LayoutParameters = tileParams;
            tile.SetPadding(0, (int)(15 * scale + 0.5f), 0,(int)(15 * scale + 0.5f));
            tile.Orientation = Orientation.Horizontal;
            tile.SetBackgroundResource(Resource.Drawable.rounded_dark);
            tile.SetGravity(GravityFlags.CenterVertical);

            /*
             * Label for host
             */
            
            TextView hostName = new TextView(context);
            LinearLayout.LayoutParams hostNameParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            hostNameParams.SetMargins((int)(10 * scale + 0.5f), 0, 0, 0);
            hostName.LayoutParameters = hostNameParams;
            hostName.Text = host;
            hostName.SetTextColor(Color.White);
            hostName.TextSize = 15;
            hostName.Typeface = font;
            
            tile.AddView(hostName);
            
            
              
            /*
             * View for filling space
             */
            View fillingView = new View(context);
            LinearLayout.LayoutParams fillingViewParams = new LinearLayout.LayoutParams(
                0, 0);
            fillingViewParams.Weight = 1;
            fillingView.LayoutParameters = fillingViewParams;

            tile.AddView(fillingView);
            
            
            /*
             * time away label
             */
            string timeAwayString = "";
            if (lastSeen != null)
            {
                var today = DateTime.Now;
                var diffOfDates = today - lastSeen;

                if (diffOfDates?.Days != 0) timeAwayString += diffOfDates?.Days + "d ";
                if (diffOfDates?.Hours != 0) timeAwayString += diffOfDates?.Hours + "h ";
                if (diffOfDates?.Minutes != 0) timeAwayString += diffOfDates?.Minutes + "m ";
                if (diffOfDates?.Seconds != 0) timeAwayString += diffOfDates?.Seconds + "s ago";
            }
            else if (lastSeen == null)
            {
                timeAwayString = "offline";
            }
            
            TextView timeAway = new TextView(context);
            LinearLayout.LayoutParams timeAwayParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            timeAwayParams.SetMargins((int)(10 * scale + 0.5f), 0, 0, 0);
            timeAway.LayoutParameters = timeAwayParams;
            timeAway.Text = timeAwayString;
            timeAway.SetTextColor(Color.White);
            timeAway.TextSize = 15;
            timeAway.Typeface = font;
            
            tile.AddView(timeAway);
            
            
            
            /*
             * View for filling space
             */
            View fillingView2 = new View(context);
            LinearLayout.LayoutParams fillingViewParams2 = new LinearLayout.LayoutParams(
                0, 0);
            fillingViewParams2.Weight = 1;
            fillingView2.LayoutParameters = fillingViewParams2;

            tile.AddView(fillingView2);
            
            
            /*
             * Button with cross
             */
            int padding = 2;
            TextView crossButton = new TextView(context);
            LinearLayout.LayoutParams crossButtonParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            crossButtonParams.SetMargins(
                (int)(10 * scale + 0.5f), 
                0, 
                (int)(10 * scale + 0.5f), 
                0);
            crossButton.LayoutParameters = crossButtonParams;
            crossButton.Gravity = GravityFlags.Right;
            crossButton.SetTextColor(Color.White);
            crossButton.TextSize = 15;
            crossButton.Typeface = font;

            if (state)
            {
                Drawable? icon = context.GetDrawable(Resource.Drawable.plus);
                crossButton.SetBackgroundResource(Resource.Drawable.rounded_button_green);
                crossButton.SetCompoundDrawablesWithIntrinsicBounds(icon, null, null, null);
                crossButton.Click += delegate { AreYouSure(ShareActionType.AvailableHostAdd, host); };
            }
            else
            {
                crossButton.SetPadding(
                    (int)(padding * scale + 0.5f), 
                    (int)(padding * scale + 0.5f),
                    (int)(padding * scale + 0.5f),
                    (int)(padding * scale + 0.5f));
                Drawable? icon = context.GetDrawable(Resource.Drawable.cross);
                crossButton.SetBackgroundResource(Resource.Drawable.rounded_button);
                crossButton.SetCompoundDrawablesWithIntrinsicBounds(icon, null, null, null);
                crossButton.Click += delegate { AreYouSure(ShareActionType.AvailableHostDelete, host); };
            }
            

            tile.AddView(crossButton);
            
            return tile;
        }
        

        private LinearLayout TrustedNetworkTile(string ssid)
        {
            LinearLayout tile = new LinearLayout(context);
            LinearLayout.LayoutParams tileParams =  new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            tileParams.SetMargins((int)(5 * scale + 0.5f), (int)(5 * scale + 0.5f), (int)(5 * scale + 0.5f), (int)(5 * scale + 0.5f));
            tile.LayoutParameters = tileParams;
            tile.SetPadding(0, (int)(15 * scale + 0.5f), 0,(int)(15 * scale + 0.5f));
            tile.Orientation = Orientation.Horizontal;
            tile.SetBackgroundResource(Resource.Drawable.rounded_dark);
            tile.SetGravity(GravityFlags.CenterVertical);
            
            /*
             * Label for network
             */
            
            TextView networkName = new TextView(context);
            LinearLayout.LayoutParams networkNameParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            networkNameParams.SetMargins((int)(10 * scale + 0.5f), 0, 0, 0);
            networkName.LayoutParameters = networkNameParams;
            networkName.Text = ssid;
            networkName.SetTextColor(Color.White);
            networkName.TextSize = 15;
            networkName.Typeface = font;
            
            tile.AddView(networkName);
            
            
            /*
             * View for filling space
             */
            View fillingView = new View(context);
            LinearLayout.LayoutParams fillingViewParams = new LinearLayout.LayoutParams(
                0, 0);
            fillingViewParams.Weight = 1;
            fillingView.LayoutParameters = fillingViewParams;

            tile.AddView(fillingView);
            
            
            /*
             * Button with cross
             */
            TextView crossButton = new TextView(context);
            LinearLayout.LayoutParams crossButtonParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            crossButtonParams.SetMargins(
                (int)(10 * scale + 0.5f), 
                0, 
                (int)(10 * scale + 0.5f),
                0);
            crossButton.LayoutParameters = crossButtonParams;
            crossButton.SetPadding((int)(2 * scale + 0.5f), (int)(2 * scale + 0.5f), (int)(2 * scale + 0.5f), (int)(2 * scale + 0.5f));
            crossButton.Gravity = GravityFlags.Right;
            crossButton.SetTextColor(Color.White);
            crossButton.TextSize = 15;
            crossButton.Typeface = font;
            crossButton.SetBackgroundResource(Resource.Drawable.rounded_button);
            Drawable? icon = context.GetDrawable(Resource.Drawable.cross);
            crossButton.SetCompoundDrawablesWithIntrinsicBounds(icon, null, null, null);

            crossButton.Click += (_, _) => AreYouSure(ShareActionType.TrustedNetworkDelete, ssid);
            

            tile.AddView(crossButton);
            
            return tile;
        }

        
        private void AreYouSure(ShareActionType actionType, string ssid = null)
        {
            LayoutInflater? ifl = LayoutInflater.From(context);
            View? popupView = ifl?.Inflate(Resource.Layout.share_are_you_sure, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(popupView);

            TextView? title = popupView?.FindViewById<TextView>(Resource.Id.share_are_you_sure_title);
            TextView? yes = popupView?.FindViewById<TextView>(Resource.Id.share_are_you_sure_yes);
            TextView? no = popupView?.FindViewById<TextView>(Resource.Id.share_are_you_sure_no);

            if (title != null) title.Typeface = font;
            if (yes != null) yes.Typeface = font;
            if (no != null) no.Typeface = font;

            AlertDialog? dialog = alert.Create();
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
            switch (actionType)
            {
                case ShareActionType.TrustedNetworkAdd:
                    title?.SetText( Html.FromHtml(
                        $"By performing this action, you will add <font color='#fa6648'>{NetworkManager.Common.CurrentSsid}" +
                        $"</font> to trusted networks list, proceed ?"
                    ), TextView.BufferType.Spannable);

                    if (yes != null)
                    {
                        yes.Click += (_, _) =>
                        {
                            FileManager.AddTrustedSsid(NetworkManager.Common.CurrentSsid);
                            RefreshFragment();
                            dialog?.Cancel();
                        };
                    }
                    break;
                case ShareActionType.TrustedNetworkDelete:
                    title?.SetText( Html.FromHtml(
                        $"By performing this action, you will blocklist <font color='#fa6648'>{ssid}" +
                        $"</font> from trusted networks list, proceed ?"
                    ), TextView.BufferType.Spannable);

                    if (yes != null) yes.Click += (_, _) =>
                    {
                        FileManager.DeleteTrustedSsid(ssid);
                        RefreshFragment();
                        dialog?.Cancel();
                    };
                    break;
                case ShareActionType.AvailableHostAdd:
                    title?.SetText( Html.FromHtml(
                        $"By performing this action, you will add <font color='#fa6648'>{ssid}" +
                        $"</font> to trusted hosts list, proceed ?"
                    ), TextView.BufferType.Spannable);

                    if (yes != null) yes.Click += (_, _) =>
                    {
                        FileManager.AddTrustedSyncTarget(ssid);
                        RefreshFragment();
                        dialog?.Cancel();
                    };
                    break;
                case ShareActionType.AvailableHostDelete:
                    title?.SetText( Html.FromHtml(
                        $"By performing this action, you will blocklist <font color='#fa6648'>{ssid}" +
                        $"</font> from trusted hosts list, proceed ?"
                    ), TextView.BufferType.Spannable);

                    if (yes != null) yes.Click += (_, _) =>
                    {
                        FileManager.DeleteTrustedSyncTarget(ssid);
                        RefreshFragment();
                        dialog?.Cancel();
                    };
                    break;
            }
            


            if (no != null) no.Click += (_, _) => dialog?.Cancel();
            dialog?.Show();
            

        }


        private void RefreshFragment()
        {
            Fragment frg = ParentFragmentManager.FindFragmentByTag("shareFragTag");
            var ft = ParentFragmentManager.BeginTransaction();
            ft.Detach(frg);
            ft.Attach(frg);
            ft.Commit();
        }
        
    }
}