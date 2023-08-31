using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using Android.Graphics;
using Android.Util;
using AndroidX.Fragment.App;

namespace Ass_Pain
{
    public class UIRenderFunctions
    {
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="musics"></param>
        /// <param name="scale"></param>
        /// <param name="ww"></param>
        /// <param name="hh"></param>
        /// <param name="btnMargins"></param>
        /// <param name="nameMargins"></param>
        /// <param name="cardMargins"></param>
        /// <param name="nameSize"></param>
        /// <param name="index"></param>
        /// <param name="context"></param>
        /// <param name="SongButtons"></param>
        /// <param name="linForDelete"></param>
        /// <returns></returns>
        public static LinearLayout PopulateHorizontal(
            MusicBaseClass musics, float scale, int ww, int hh, int[] btnMargins, int[] nameMargins, int[] cardMargins, int nameSize, int index,
            Context context, Dictionary<LinearLayout, int> SongButtons,
            LinearLayout linForDelete = null
        )
        {
            //リネアルレーアート作る
            LinearLayout lnIn = new LinearLayout(context);
            lnIn.Orientation = Orientation.Horizontal;
            lnIn.SetBackgroundResource(Resource.Drawable.rounded);

            LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            lnInParams.SetMargins(cardMargins[0], cardMargins[1], cardMargins[2], cardMargins[3]);
            lnIn.LayoutParameters = lnInParams;


            // ボッタン作って
            int w = (int)(ww * scale + 0.5f);
            int h = (int)(hh * scale + 0.5f);



          
            lnIn.Click += (sender, e) =>
            {
                LinearLayout pressedButtoon = (LinearLayout)sender;
                foreach (KeyValuePair<LinearLayout, int> pr in SongButtons)
                {
                    if (pr.Key == pressedButtoon)
                    {
                        MainActivity.ServiceConnection?.Binder?.Service?.GenerateQueue(MainActivity.stateHandler.Songs, pr.Value);
                    }
                }
            };
            lnIn.LongClick += (sender, e) =>
            {
                // show_popup_song_edit(sender, e, song, linForDelete, lnIn);
            };

            SongButtons.Add(lnIn, index);
            

            lnIn.SetHorizontalGravity(GravityFlags.Center);
            return lnIn;
        }
        
        
        
        
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="musics"></param>
        /// <param name="ww"></param>
        /// <param name="hh"></param>
        /// <param name="btnMargins"></param>
        /// <param name="nameMargins"></param>
        /// <param name="cardMargins"></param>
        /// <param name="nameSize"></param>
        /// <param name="index"></param>
        /// <param name="context"></param>
        /// <param name="albumButtons"></param>
        /// <param name="activity"></param>
        /// <param name="linForDelete"></param>
        /// <returns></returns>
        public static LinearLayout PopulateVertical(
            MusicBaseClass musics, int ww, int hh, int[] btnMargins, int[] nameMargins, int[] cardMargins, int nameSize, int index,
            Context context, Dictionary<LinearLayout, object> albumButtons, FragmentActivity activity, 
            LinearLayout linForDelete = null
        )
        {
            //リネアルレーアート作る
            LinearLayout lnIn = new LinearLayout(context);
            lnIn.Orientation = Orientation.Vertical;
            lnIn.SetBackgroundResource(Resource.Drawable.rounded);

            LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            lnInParams.SetMargins(cardMargins[0], cardMargins[1], cardMargins[2], cardMargins[3]);
            lnIn.LayoutParameters = lnInParams;


            // ボッタン作って
            if (musics is Album album)
            {
                lnIn.Click += (sender, _) =>
                {
                    LinearLayout pressedButton = (LinearLayout)sender;
                    foreach(KeyValuePair<LinearLayout, object> pr in albumButtons)
                    {
                        if (pr.Key == pressedButton && pr.Value is Album album1)
                        {
                            ((AllSongs)activity).ReplaceFragments(AllSongs.FragmentType.AlbumFrag, album1.Title);
                            break;
                        }
                    }
                    
                };

                /*
                lnIn.LongClick += (sender, e) =>
                {
                    show_popup_song_edit(sender, e, album, linForDelete, lnIn);
                };
                */

                albumButtons.Add(lnIn, album);
            }
            else if (musics is Artist artist)
            {
                lnIn.Click += (sender, _) =>
                {
                    LinearLayout pressedButton = (LinearLayout)sender;

                    foreach (KeyValuePair<LinearLayout, object> pr in albumButtons)
                    {
                        if (pr.Key == pressedButton && pr.Value is Artist artist1)
                        {
                            ((AllSongs)activity).ReplaceFragments(AllSongs.FragmentType.Authorfrag, artist1.Title);
                            break;
                        }
                    }
                };

                /*
                lnIn.LongClick += (sender, e) =>
                {
                    show_popup_song_edit(sender, e, artist, linForDelete, lnIn);
                };
                */

                albumButtons.Add(lnIn, artist);
            }

            lnIn.SetHorizontalGravity(GravityFlags.Center);
            return lnIn;
        }
        
        
        
        
        
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="obj"></param>
        /// <param name="ww"></param>
        /// <param name="hh"></param>
        /// <param name="btnMargins"></param>
        /// <param name="nameSize"></param>
        /// <param name="nameMargins"></param>
        /// <param name="scale"></param>
        /// <param name="context"></param>
        public static void SetTilesImage(LinearLayout parent, MusicBaseClass obj, int ww, int hh, int[] btnMargins, int nameSize, int[] nameMargins, float scale, Context context)
        {
            ImageView mori = new ImageView(context);
            LinearLayout.LayoutParams ll = new LinearLayout.LayoutParams(
                (int)(ww * scale + 0.5f), (int)(hh * scale + 0.5f)
            );
            ll.SetMargins(btnMargins[0], btnMargins[1], btnMargins[2], btnMargins[3]);
            mori.LayoutParameters = ll;

            if (!(obj is Album || obj is Artist || obj is Song))
            {
                return;
            }

            mori.SetImageBitmap(
                obj.Image
            );
            

            parent.AddView(mori);

            //アルブムの名前
            TextView name = new TextView(context);
            name.Text = obj.Title;
            name.TextSize = nameSize;
            name.SetTextColor(Color.White);
            name.TextAlignment = TextAlignment.Center;
            name.SetForegroundGravity(GravityFlags.Center);

            LinearLayout.LayoutParams lnNameParams = new LinearLayout.LayoutParams(
                (int)(130 * scale + 0.5f),
                ViewGroup.LayoutParams.WrapContent
            );
            lnNameParams.SetMargins(nameMargins[0], nameMargins[1], nameMargins[2], nameMargins[3]);

            name.LayoutParameters = lnNameParams;

            parent.SetGravity(GravityFlags.Center);
            parent.SetHorizontalGravity(GravityFlags.Center);
            parent.AddView(name);
            

        }
    }
}