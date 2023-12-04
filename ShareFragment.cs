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
using Java.Lang;
using TagLib.Tiff.Arw;
using Exception = System.Exception;
using Fragment = AndroidX.Fragment.App.Fragment;
using Orientation = Android.Widget.Orientation;
#if DEBUG
using Ass_Pain.Helpers;
#endif
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

        private List<string> trustedSsids;
        private LinearLayout? trustedNetworkList;
        private LinearLayout? availableHostsList;
        
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

#if DEBUG
            MyConsole.WriteLine(NetworkManager.GetAllHosts().ToString());
#endif
            
            return view;
        }


        /// <inheritdoc />
        public ShareFragment(Context ctx, AssetManager assets)
        {
            context = ctx;
            if (ctx.Resources is { DisplayMetrics: not null }) scale = ctx.Resources.DisplayMetrics.Density;
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
            this.assets = assets;
            StateHandler.OnShareFragmentRefresh += RefreshFragment;
        }


        private void RenderUi()
        {
            /*
             * ziskavanie elementov z XAML suboru za pomoci ID ktore maju pridelene v XAML subore
             */
            TextView? remoteListeningLabel = view?.FindViewById<TextView>(Resource.Id.remote_listening_label);
            TextView? remoteConnectionsPortLabel = view?.FindViewById<TextView>(Resource.Id.remote_connections_port_label);
            TextView? remoteConnectionsPort = view?.FindViewById<TextView>(Resource.Id.remote_connections_port);
            TextView? trustedNetworkLabel = view?.FindViewById<TextView>(Resource.Id.trusted_network_label);
            TextView? addTrustedNetworkButton = view?.FindViewById<TextView>(Resource.Id.add_trusted_network_button);
            TextView? availableHostsLabel = view?.FindViewById<TextView>(Resource.Id.available_hosts_label);

            /*
             * nastavovanie fontu individualne pre kazdy element obsahujuci text a zaroven kontrolovanie ci maju
             * nenulovu hodnotu.
             */
            if (availableHostsLabel != null) availableHostsLabel.Typeface = font;
            if (remoteListeningLabel != null) remoteListeningLabel.Typeface = font;
            if (remoteConnectionsPortLabel != null) remoteConnectionsPortLabel.Typeface = font;
            if (remoteConnectionsPort != null) remoteConnectionsPort.Typeface = font;
            if (trustedNetworkLabel != null) trustedNetworkLabel.Typeface = font;
            if (addTrustedNetworkButton != null) addTrustedNetworkButton.Typeface = font;


            /*
             * Nastavovanie obsahu textoveho pola zobrazujuceho aktualny WAN port a naviazovanie click eventu
             * po kliknuti na textove pole, po kliknuti sa zobrazi popup v ktorom sa nachadza vstup pre zmenenie
             * WAN portu.
             */
            if (remoteConnectionsPort != null) remoteConnectionsPort.Click += delegate { ChangeWanPort(); };
            if (remoteConnectionsPort != null) remoteConnectionsPort.Text = SettingsManager.WanPort.ToString();

            /*
             * zamknutie moznosti pridania novej doveryhodnej siete v pripade ak aktualne siet nie je doveryhodna
             * ak je doveryhodna, pridanie noveho click eventu pre tlacidlo a odomknutie moznosti pridania soveryhodnej siete
             */
#if DEBUG
            MyConsole.WriteLine(NetworkManager.Common.CurrentSsid);
#endif
            
            if (NetworkManager.Common.CurrentSsid == string.Empty     || 
                NetworkManager.Common.CurrentSsid == "<unknown ssid>" || 
                FileManager.IsTrustedSsid(NetworkManager.Common.CurrentSsid)
                )
            {
                /*
                 * zmenenie tlacidlu farbu pozdaia, textu, ikonky na vyjadrenie zamknutia tlacidla
                 */
                addTrustedNetworkButton?.SetTextColor(Color.Gray);
                addTrustedNetworkButton?.SetBackgroundResource(Resource.Drawable.rounded_button_disabled);
                Drawable? icon = context.GetDrawable(Resource.Drawable.plus_disabled);
                addTrustedNetworkButton?.SetCompoundDrawablesWithIntrinsicBounds(icon, null, null, null);
            }
            else
            {
                /*
                 * zmenenie tlacidlu farbu pozdaia, textu, ikonky na vyjadrenie odomknutia tlacidla
                 * a naviazanie click eventu pre tlacidlo ktore po kliknuti spusti popup ktory si poziada od
                 * pouzivatela potvrdenie o pridanie novej doveryhodnej siete
                 */
                Drawable? icon = context.GetDrawable(Resource.Drawable.plus);
                addTrustedNetworkButton?.SetCompoundDrawablesWithIntrinsicBounds(icon, null, null, null);
                addTrustedNetworkButton?.SetTextColor(Color.White);
                addTrustedNetworkButton?.SetBackgroundResource(Resource.Drawable.rounded_button);
                if (addTrustedNetworkButton != null)
                    addTrustedNetworkButton.Click += delegate { AreYouSure(ShareActionType.TrustedNetworkAdd); };
            }
            
            /*
             * Vykreslenie policok pre doveryhodne siete vhodne na pripojenie
             */
            trustedSsids = FileManager.GetTrustedSsids();
            trustedNetworkList = view?.FindViewById<LinearLayout>(Resource.Id.trusted_network_list);
            for (int i = 0; i < trustedSsids.Count; i++)
                trustedNetworkList?.AddView(TrustedNetworkTile(trustedSsids[i]));

            
            /*
             * Vykreslenie policok pre moznych hostitelov na spojenie
             */ 
            List<(string hostname, DateTime? lastSeen, bool state)> allHosts = NetworkManager.GetAllHosts();
            availableHostsList = view?.FindViewById<LinearLayout>(Resource.Id.available_hosts_list);
            foreach (var (hostname, lastSeen, state) in allHosts)
                availableHostsList?.AddView(AvailableHostsTile(hostname, lastSeen, state));
            
            
            /*
             * Vytvorenie click eventu pre prepinac sluziaci na prepinanie medzi stavom "moze pouzicat WAN" a "nemoze pouzivat WAN"
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
                        if (!trustedSsids.Contains(NetworkManager.Common.CurrentSsid))
                        {
                            dialog?.Cancel();
                        }

                        yes.Click += (_, _) =>
                        {
                            NetworkManagerCommon.TestNetwork();
                            FileManager.AddTrustedSsid(NetworkManager.Common.CurrentSsid);
                            
                            dialog?.Dismiss();
                            RefreshFragment();
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
                        NetworkManagerCommon.TestNetwork();
                        FileManager.DeleteTrustedSsid(ssid);
                        dialog?.Cancel();
                        RefreshFragment();
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
                        dialog?.Cancel();
                        RefreshFragment();
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
                        dialog?.Cancel();
                        RefreshFragment();
                    };
                    break;
            }
            


            if (no != null) no.Click += (_, _) => dialog?.Cancel();
            dialog?.Show();
            

        }
        
        private void RefreshFragment()
        {
            try
            { 
                /*
                    Fragment? frg = ChildFragmentManager.FindFragmentByTag("shareFragTag");
                    var ft = ParentFragmentManager.BeginTransaction();
                    if (frg != null)
                    {
                        ft.Detach(frg);
                        ft.Attach(frg);
                    }

                    ft.Commit();
                */
                if (IsAdded)
                {
                    Activity?.RunOnUiThread(() =>
                    {
                        view?.FindViewById<RelativeLayout>(Resource.Id.share_fragment_main)?.Invalidate();
                        availableHostsList?.RemoveAllViews();
                        trustedNetworkList?.RemoveAllViews();
                        trustedSsids.Clear();
                        RenderUi();
                    });
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
            }
          
        }



        
        private void ChangeWanPort()
        {
            LayoutInflater? ifl = LayoutInflater.From(context);
            View? popupView = ifl?.Inflate(Resource.Layout.share_new_port, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(popupView);
          
            AlertDialog? dialog = alert.Create();
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));

            TextView? title = popupView?.FindViewById<TextView>(Resource.Id.share_new_port_title);
            TextView? confirm = popupView?.FindViewById<TextView>(Resource.Id.share_new_port_confirm);
            TextView? cancel = popupView?.FindViewById<TextView>(Resource.Id.share_new_port_cancel);
            EditText? input = popupView?.FindViewById<EditText>(Resource.Id.share_new_port_input);

            if (title != null) title.Typeface = font;
            if (confirm != null) confirm.Typeface = font;
            if (cancel != null) cancel.Typeface = font;
            if (input != null) input.Typeface = font;

            if (title != null) title.Text = "Set new WAN port, input must obey 1024 <= 65535";

            if (confirm != null)
                confirm.Click += delegate
                {
                    if (Int32.TryParse(input?.Text, out var res))
                        if (res >= 1024 && res <= 65535)
                            SettingsManager.WanPort = res;
                        else
                            Toast.MakeText(context,
                                "The WAN port number must be bigger than 1024 and lower than 65535", ToastLength.Long)
                                ?.Show();
                    RefreshFragment();
                    dialog?.Cancel();
                    
                };
            
            if (cancel != null) cancel.Click += (_, _) => dialog?.Cancel();
            dialog?.Show();
        }
        
        
    }
}