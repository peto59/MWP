using System;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using AndroidX.Core.Text;
using Google.Android.Material.FloatingActionButton;
using MWP.BackEnd;
using MWP.BackEnd.Network;
using Color = Android.Graphics.Color;
using Fragment = AndroidX.Fragment.App.Fragment;
using Orientation = Android.Widget.Orientation;


namespace MWP
{
    /// <summary>
    /// 
    /// </summary>
    public class PlaylistsFragment : Fragment
    {
        private readonly Context context;
        private RelativeLayout? mainLayout;
        private Typeface? font;

        private PlaylistFragment playlistFragment; 
            
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inflater"></param>
        /// <param name="container"></param>
        /// <param name="savedInstanceState"></param>
        /// <returns></returns>
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.playlists_fragment, container, false);
            
            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.playlists_fragment_main);

            RenderPlaylists();
            
            FloatingActionButton? createPlaylist = mainLayout?.FindViewById<FloatingActionButton>(Resource.Id.playlists_fab);
            if (BlendMode.Multiply != null)
                createPlaylist?.Background?.SetColorFilter(
                    new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                );
            if (createPlaylist != null) createPlaylist.Click += CreatePlaylistPopup;
            
            return view;
        }

        /// <summary>
        /// Constructor for SongsFragment.cs
        /// </summary>
        /// <param name="ctx">Main Activity context (e.g. "this")</param>
        /// <param name="assets"></param>
        public PlaylistsFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            playlistFragment = new PlaylistFragment(ctx, assets);
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
        }


        private void RenderPlaylists()
        {
            ScrollView playlistsScroll = new ScrollView(context);
            RelativeLayout.LayoutParams playlistScrollParams = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            );
            playlistScrollParams.SetMargins(0, 20, 0, 0);
            playlistsScroll.LayoutParameters = playlistScrollParams;


            LinearLayout playlistLnMain = new LinearLayout(context);
            playlistLnMain.Orientation = Orientation.Vertical;
            RelativeLayout.LayoutParams playlistLnMainParams = new RelativeLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent
            );
            playlistLnMainParams.SetMargins(20, 20, 20, 20);
            playlistLnMain.LayoutParameters = playlistLnMainParams;
            
            int[] playlistCardMargins = { 0, 50, 0, 0 };


            List<string> playlists = FileManager.GetPlaylist();
            foreach (var playlist in playlists)
            {
                LinearLayout lnIn = new LinearLayout(context);
                lnIn.Orientation = Orientation.Vertical;
                lnIn.SetBackgroundResource(Resource.Drawable.rounded_primaryColor);

                LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                );
                lnInParams.SetMargins(
                    playlistCardMargins[0], playlistCardMargins[1], 
                    playlistCardMargins[2], playlistCardMargins[3]
                );
                lnIn.LayoutParameters = lnInParams;


                TextView plaName = new TextView(context);
                LinearLayout.LayoutParams nameParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                );
                nameParams.SetMargins(0, 20, 0, 20);
                plaName.LayoutParameters = nameParams; 
                plaName.Text = playlist;
                plaName.Typeface = font;
                plaName.SetTextColor(Color.White);
                plaName.TextSize = 30;
                plaName.TextAlignment = TextAlignment.Center;

                TextView songsCount = new TextView(context);
                LinearLayout.LayoutParams countParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                );
                countParams.SetMargins(0, 20, 0, 20);
                songsCount.LayoutParameters = countParams;
                songsCount.Text = $"number of songs: {FileManager.GetPlaylist(playlist).Count}";
                songsCount.Typeface = font;
                songsCount.SetTextColor(Color.White);
                songsCount.TextSize = 15;
                songsCount.TextAlignment = TextAlignment.Center;


                lnIn.Click += (_, _) =>
                {
                    var fragmentTransaction = ParentFragmentManager.BeginTransaction();
                    Bundle bundle = new Bundle();
                    bundle.PutString("title", playlist);
                    
                    playlistFragment.Arguments = bundle;
                    fragmentTransaction.Replace(Resource.Id.MainFragmentLayoutDynamic, playlistFragment);
                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                };

                lnIn.LongClick += (_, _) =>
                {
                    DeletePlaylist(playlist);
                };

                lnIn.AddView(plaName);
                lnIn.AddView(songsCount);

                playlistLnMain.AddView(lnIn);
            }

            playlistsScroll.AddView(playlistLnMain);
            mainLayout?.AddView(playlistsScroll);
        }
        
        
        private void CreatePlaylistPopup(object sender, EventArgs e)
        {
            LayoutInflater? ifl = LayoutInflater.From(context);
            View? view = ifl?.Inflate(Resource.Layout.new_playlist_popup, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);

            TextView? dialogTitle = view?.FindViewById<TextView>(Resource.Id.AddPlaylist_title);
            if (dialogTitle != null) dialogTitle.Typeface = font;

            AlertDialog? dialog = alert.Create();
            EditText? userData = view?.FindViewById<EditText>(Resource.Id.editText);
            if (userData != null)
            {
                userData.Typeface = font;
                alert.SetCancelable(false);


                TextView? pButton = view?.FindViewById<TextView>(Resource.Id.AddPlaylist_submit);
                if (pButton != null) pButton.Typeface = font;
                if (pButton != null)
                    pButton.Click += (_, _) =>
                    {
                        if (userData.Text != null)
                        {
                            FileManager.CreatePlaylist(userData.Text);
                            Toast.MakeText(
                                    context, userData.Text + " Created successfully",
                                    ToastLength.Short
                                )
                                ?.Show();
                        }
                        
                        alert.Dispose();
                        dialog?.Cancel();
                        RenderPlaylists();
                    };
            }

            TextView? nButton = view?.FindViewById<TextView>(Resource.Id.AddPlaylist_cancel);
            if (nButton != null) nButton.Typeface = font;
            
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
            if (nButton != null) nButton.Click += (_, _) => dialog?.Cancel();
            
            dialog?.Show();
        }
        
         private void DeletePlaylist(string playlistName)
        {
            LayoutInflater? ifl = LayoutInflater.From(context);
            View? view = ifl?.Inflate(Resource.Layout.delete_playlist_popup, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);

            TextView? dialogTitle = view?.FindViewById<TextView>(Resource.Id.delete_playlist_title);
            if (dialogTitle != null) dialogTitle.Typeface = font;
            
            dialogTitle?.SetText( Html.FromHtml(
                $"By performing this action, you will delete playlist: <font color='#fa6648'>{playlistName}" +
                $"</font>, proceed ?",
                HtmlCompat.FromHtmlModeLegacy
            ), TextView.BufferType.Spannable);
            
            AlertDialog? dialog = alert.Create();
            
            TextView? pButton = view?.FindViewById<TextView>(Resource.Id.delete_playlist_submit);
            if (pButton != null) pButton.Typeface = font;
            if (pButton != null)
                pButton.Click += (_, _) =>
                {
                    FileManager.DeletePlaylist(playlistName);
                    Toast.MakeText(
                            context, playlistName + " Deleted successfully",
                            ToastLength.Short
                        )
                        ?.Show();
                    
                    alert.Dispose();
                    dialog?.Cancel();
                    RenderPlaylists();
                };

            TextView? nButton = view?.FindViewById<TextView>(Resource.Id.delete_playlist_cancel);
            if (nButton != null) nButton.Typeface = font;
            
            
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
            if (nButton != null) nButton.Click += (_, _) => dialog?.Cancel();
            
            dialog?.Show();
        }
        
        
    }
}