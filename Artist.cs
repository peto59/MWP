using System;
using System.Collections.Generic;
using Android.Graphics;
using System.IO;
using Android.App;

namespace Ass_Pain
{
    public class Artist
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

        public List<Album> Albums { get; } = new List<Album>();
        public Album Album
        {
            get
            {
                return Albums.Count > 0 ? Albums[0] : new Album("No Album", "Default", false);
            }
        }
        public string ImgPath { get; }
        public bool Initialized { get; } = true;
        
        public void AddAlbum(ref List<Album> albums)
        {
            Albums.AddRange(albums);
        }
        public void AddAlbum(ref Album album)
        {
            Albums.Add(album);
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
                        foreach (Album album in Albums)
                        {
                            if (!album.Initialized)
                            {
                                continue;
                            }
                            image = album.GetImage(false);
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

        public Artist(string title, Song song, Album album, string imgPath)
        {
            Title = title;
            Songs = new List<Song> {song};
            Albums = new List<Album> {album};
            ImgPath = imgPath;
        }
        
        public Artist(string title, List<Song> song, Album album, string imgPath)
        {
            Title = title;
            Songs = song;
            Albums = new List<Album> {album};
            ImgPath = imgPath;
        }
        
        public Artist(string title, Song song, List<Album> album, string imgPath)
        {
            Title = title;
            Songs = new List<Song> {song};
            Albums = album;
            ImgPath = imgPath;
        }
        
        public Artist(string title, List<Song> song, List<Album> album, string imgPath)
        {
            Title = title;
            Songs = song;
            Albums = album;
            ImgPath = imgPath;
        }
        
        public Artist(string title, string imgPath, bool initialized = true)
        {
            Title = title;
            ImgPath = imgPath;
            Initialized = initialized;
        }

        public override bool Equals(object obj)
        {
            var item = obj as Artist;

            if (item == null)
            {
                return false;
            }
            
            return Equals(item);
        }

        protected bool Equals(Artist other)
        {
            return Title == other.Title && Equals(Songs, other.Songs) && Equals(Albums, other.Albums) && Equals(ImgPath, other.ImgPath);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Title, Songs, Albums, ImgPath);
        }

        public override string ToString()
        {
            return $"Artist: title> {Title} song> {Song.Title} album> {Album.Title}";
        }
    }
}