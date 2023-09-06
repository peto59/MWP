using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text.Style;
using Android.Util;
using Google.Android.Material.FloatingActionButton;
using Fragment = AndroidX.Fragment.App.Fragment;



namespace Ass_Pain
{
    public class AllSongsFragment : Fragment
    {
        private readonly Context context;
        private RelativeLayout mainLayout;
        private Typeface font;


        private static AllSongsFragment instance;
        
        private SongsFragment songsFragment;
        private SongsFragment PlaylistsFragment;
        private AlbumAuthorFragment AlbumsFragment;
        private AlbumFragment albumFragment;
        private AuthorFragment authorFragment;
        private PlaylistsFragment playlistsFragment;
        private PlaylistFragment playlistFragment;
        
        public enum FragmentType
        {
            AlbumFrag,
            AuthorFrag,
            PlaylistFrag
        }

   
        public AllSongsFragment(Context ctx)
        {
            context = ctx;
            songsFragment = new SongsFragment(ctx);
            AlbumsFragment = new AlbumAuthorFragment(ctx);
            albumFragment = new AlbumFragment(ctx);
            authorFragment = new AuthorFragment(ctx);
            playlistsFragment = new PlaylistsFragment(ctx);
            playlistFragment = new PlaylistFragment(ctx);
            
            font = Typeface.CreateFromAsset(context.Assets, "sulphur.ttf");

        }
        
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            instance = this;
            View view = inflater.Inflate(Resource.Layout.all_songs_fragment, container, false);
            mainLayout = view?.FindViewById<RelativeLayout>(Resource.Id.AllSongsMainLayout);
            
            
            TextView author = mainLayout?.FindViewById<TextView>(Resource.Id.author);
            TextView allSongs = mainLayout?.FindViewById<TextView>(Resource.Id.all_songs);
            TextView playlists = mainLayout?.FindViewById<TextView>(Resource.Id.playlists);
            if (author != null && allSongs != null && playlists != null)
            {
                author.Typeface = font;
                allSongs.Typeface = font;
                playlists.Typeface = font;

                FloatingActionButton createPlaylist = mainLayout?.FindViewById<FloatingActionButton>(Resource.Id.fab);
                if (BlendMode.Multiply != null)
                    createPlaylist?.Background?.SetColorFilter(
                        new BlendModeColorFilter(Color.Rgb(255, 76, 41), BlendMode.Multiply)
                    );
                if (createPlaylist != null) createPlaylist.Click += CreatePlaylistPopup;


                int activeFragment = 0;
                author.Click += (sender, e) =>
                {
                    var fragmentTransaction = ChildFragmentManager.BeginTransaction();
                    if (activeFragment == 0)
                    {
                        fragmentTransaction.Add(Resource.Id.AllSongsFragmentLayoutDynamic, AlbumsFragment);
                        activeFragment = 1;
                    }
                    else
                        fragmentTransaction.Replace(Resource.Id.AllSongsFragmentLayoutDynamic, AlbumsFragment);

                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                };
                allSongs.Click += (sender, e) =>
                {
                    var fragmentTransaction = ChildFragmentManager.BeginTransaction();
                    if (activeFragment == 0)
                    {
                        fragmentTransaction.Add(Resource.Id.AllSongsFragmentLayoutDynamic, songsFragment);
                        activeFragment = 1;
                    }
                    else
                        fragmentTransaction.Replace(Resource.Id.AllSongsFragmentLayoutDynamic, songsFragment);

                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                };
                playlists.Click += (sender, e) =>
                {
                    var fragmentTransaction = ChildFragmentManager.BeginTransaction();
                    if (activeFragment == 0)
                    {
                        fragmentTransaction.Add(Resource.Id.AllSongsFragmentLayoutDynamic, playlistsFragment);
                        activeFragment = 1;
                    }
                    else
                        fragmentTransaction.Replace(Resource.Id.AllSongsFragmentLayoutDynamic, playlistsFragment);

                    fragmentTransaction.AddToBackStack(null);
                    fragmentTransaction.Commit();
                };
            }

            return view;
        }

        /// <summary>
        /// Function for replacing fragment from inside another fragment by calling ((AllSongs)this.Activity).ReplaceFragments()
        /// </summary>
        /// <param name="type">Type of fragment, by which current should be replaced</param>
        /// <param name="title">title of either an album or artist</param>
        public void ReplaceFragments(FragmentType type, string title)
        {
            var fragmentTransaction = ChildFragmentManager.BeginTransaction();
            Bundle bundle = new Bundle();
            bundle.PutString("title", title);
            
            switch (type)
            {
                case FragmentType.AlbumFrag:
                    albumFragment.Arguments = bundle;
                    fragmentTransaction.Replace(Resource.Id.AllSongsFragmentLayoutDynamic, albumFragment);
                    break;
                case FragmentType.AuthorFrag:
                    authorFragment.Arguments = bundle;
                    fragmentTransaction.Replace(Resource.Id.AllSongsFragmentLayoutDynamic, authorFragment);
                    break;
                case FragmentType.PlaylistFrag:
                    playlistFragment.Arguments = bundle;
                    fragmentTransaction.Replace(Resource.Id.AllSongsFragmentLayoutDynamic, playlistFragment);
                    break;
            }
            
            fragmentTransaction.AddToBackStack(null);
            fragmentTransaction.Commit();
            
        }


        /// <summary>
        /// Function used to retrieve this fragments instance for calling the ReplaceFragments function
        /// </summary>
        /// <returns>returns AllSongsFragment instance</returns>
        public static AllSongsFragment GetInstance()
        {
            return instance;
        }
        
        
        private void CreatePlaylistPopup(object sender, EventArgs e)
        {
            LayoutInflater ifl = LayoutInflater.From(context);
            View view = ifl?.Inflate(Resource.Layout.new_playlist_popup, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);

            TextView dialogTitle = view?.FindViewById<TextView>(Resource.Id.AddPlaylist_title);
            if (dialogTitle != null) dialogTitle.Typeface = font;

            EditText userData = view?.FindViewById<EditText>(Resource.Id.editText);
            if (userData != null)
            {
                userData.Typeface = font;
                alert.SetCancelable(false);


                TextView pButton = view.FindViewById<TextView>(Resource.Id.AddPlaylist_submit);
                if (pButton != null) pButton.Typeface = font;
                if (pButton != null)
                    pButton.Click += (_, _) =>
                    {
                        FileManager.CreatePlaylist(userData.Text);
                        Toast.MakeText(
                                context, userData.Text + " Created successfully",
                                ToastLength.Short
                            )
                            ?.Show();
                        alert.Dispose();
                    };
            }

            TextView nButton = view?.FindViewById<TextView>(Resource.Id.AddPlaylist_cancel);
            if (nButton != null) nButton.Typeface = font;
         

            AlertDialog dialog = alert.Create();
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
            if (nButton != null) nButton.Click += (_, _) => dialog?.Cancel();
            
            
            dialog?.Show();
            

        }


        private void add_alias_popup(string authorN)
        {

            LayoutInflater ifl = LayoutInflater.From(context);
            View view = ifl?.Inflate(Resource.Layout.add_alias_popup, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);

            AlertDialog dialog = alert.Create();

            if (view != null)
            {
                TextView author = view.FindViewById<TextView>(Resource.Id.author_name);
                if (author != null) author.Text = authorN;
            }

            EditText userInput = view?.FindViewById<EditText>(Resource.Id.user_author);
            Button sub = view?.FindViewById<Button>(Resource.Id.submit_alias);
            if (sub != null)
                sub.Click += delegate
                {
                    if (userInput != null) FileManager.AddAlias(authorN, userInput.Text);
                    dialog?.Hide();
                };

            Button cancel = view?.FindViewById<Button>(Resource.Id.cancel_alias);
            if (cancel != null)
                cancel.Click += delegate { dialog?.Hide(); };


            dialog?.Show();
        }


    }
}