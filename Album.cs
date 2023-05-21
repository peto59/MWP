using System;
using System.Collections.Generic;
using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Navigation;
using Google.Android.Material.Snackbar;
using Android.Graphics;
using Android.Widget;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Android.Service.Autofill;
using Android.Icu.Number;
using Org.Apache.Http.Conn;
using Com.Arthenica.Ffmpegkit;
using Android.Drm;
using AngleSharp.Html.Dom;
using Newtonsoft.Json;
using System.Threading;
using Org.Apache.Http.Authentication;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Ass_Pain
{
    public class Album
    {
        public string Title { get; }
        public List<Song> Songs { get; } = new List<Song>();
        public Song Song
        {
            get
            {
                return Songs.Count > 0 ? Songs[0] : new Song("No Name", new DateTime(1970, 1, 1), "Default", false);
            }
        }

        public List<Artist> Artists { get; } = new List<Artist>();
        public Artist Artist
        {
            get
            {
                return Artists.Count > 0 ? Artists[0] : new Artist("No Artist", "Default", false);
            }
        }
        
        public string ImgPath { get; }
        public bool Initialized { get; } = true;
        
        public void AddArtist(ref List<Artist> artists)
        {
            Artists.AddRange(artists);
        }
        public void AddArtist(ref Artist artist)
        {
            Artists.Add(artist);
        }
        
        public void AddSong(ref List<Song> songs)
        {
            Songs.AddRange(songs);
        }
        public void AddSong(ref Song song)
        {
            Songs.Add(song);
        }

        public Bitmap GetImage(bool shouldFallBack = true)
        {
            Bitmap image = null;

            try
            {
                if (!string.IsNullOrEmpty(ImgPath))
                {
                    using (var f = File.OpenRead(ImgPath))
                    {
                        image = BitmapFactory.DecodeStream(f);
                        f.Close();
                    }
                }
                else if (shouldFallBack)
                {
                    foreach (Song song in Songs)
                    {
                        if (!song.Initialized)
                        {
                            continue;
                        }
                        image = song.GetImage(false);
                        if (image != null)
                        {
                            break;
                        }
                    }

                    if (image == null)
                    {
                        foreach (Artist artist in Artists)
                        {
                            if (!artist.Initialized)
                            {
                                continue;
                            }
                            image = artist.GetImage(false);
                            if (image != null)
                            {
                                break;
                            }
                        }
                    }
                }
                if (image == null)
                {
                    image = BitmapFactory.DecodeStream(Application.Context.Assets.Open("music_placeholder.png")); //In case of no cover and no embedded picture show default image from assets 
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                image = BitmapFactory.DecodeStream(Application.Context.Assets.Open("music_placeholder.png")); //In case of no cover and no embedded picture show default image from assetsthrow;
            }
            return image;
        }

        public Album(string title, Song song, Artist artist, string imgPath)
        {
            Title = title;
            Songs = new List<Song> {song};
            Artists = new List<Artist> {artist};
            ImgPath = imgPath;
        }
        
        public Album(string title, List<Song> song, Artist artist, string imgPath)
        {
            Title = title;
            Songs = song;
            Artists = new List<Artist> {artist};
            ImgPath = imgPath;
        }
        
        public Album(string title, Song song, List<Artist> artist, string imgPath)
        {
            Title = title;
            Songs = new List<Song> {song};
            Artists = artist;
            ImgPath = imgPath;
        }
        
        public Album(string title, List<Song> song, List<Artist> artist, string imgPath)
        {
            Title = title;
            Songs = song;
            Artists = artist;
            ImgPath = imgPath;
        }
        
        public Album(string title, string imgPath, bool initialized = true)
        {
            Title = title;
            ImgPath = imgPath;
            Initialized = initialized;
        }
        
        public override bool Equals(object obj)
        {
            var item = obj as Album;

            if (item == null)
            {
                return false;
            }
            
            return Equals(item);
        }

        protected bool Equals(Album other)
        {
            return Title == other.Title && Equals(Songs, other.Songs) && Equals(Artists, other.Artists) && Equals(ImgPath, other.ImgPath);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Title, Songs, Artists, ImgPath);
        }
        
        public override string ToString()
        {
            return $"Album: title> {Title} song> {Song.Title} artist> {Artist.Title}";
        }
    }
}