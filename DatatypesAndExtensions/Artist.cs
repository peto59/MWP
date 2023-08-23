using System;
using System.Collections.Generic;
using Android.Graphics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Android.App;
using Newtonsoft.Json;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class Artist : MusicBaseContainer
    {
        [JsonProperty]
        public override string Title { get; }
        
        public override List<Song> Songs { get; } = new List<Song>();
        public Song Song => Songs.Count > 0 ? Songs[0] : new Song("No Name", new DateTime(1970, 1, 1), "Default", false);

        public List<Album> Albums { get; } = new List<Album> {new Album("Uncategorized", "Default", true, false)};
        public Album Album => Albums.Count > 0 ? Albums[0] : new Album("No Album", "Default", false);
        
        public string ImgPath { get; }
        public bool Initialized { get; private set; } = true;
        public override Bitmap Image => GetImage();

        public void AddAlbum(ref List<Album> albums)
        {
            foreach (Album album in albums.Where(album => !Albums.Contains(album)))
            {
                Albums.Add(album);
            }
        }
        public void AddAlbum(ref Album album)
        {
            if (!Albums.Contains(album))
                Albums.Add(album);
        }
        
        public void AddSong(ref List<Song> songs)
        {
            foreach (Song song in songs.Where(song => !Songs.Contains(song)))
            {
                Songs.Add(song);
            }
        }
        public void AddSong(ref Song song)
        {
            if (!Songs.Contains(song))
                Songs.Add(song);
        }

        public void RemoveSong(Song song)
        {
            Songs.Remove(song);
        }
        
        public void RemoveSong(List<Song> songs)
        {
            songs.ForEach(RemoveSong);
        }
        
        public void RemoveAlbum(Album album)
        {
            Albums.Remove(album);
        }
        
        public void RemoveAlbum(List<Album> albums)
        {
            albums.ForEach(RemoveAlbum);
        }
        
        ///<summary>
        ///Nukes this object out of existence
        ///</summary>
        public void Nuke()
        {
            Songs.ForEach(song =>
            {
                song.RemoveArtist(this);
            });
            Albums.ForEach(album =>
            {
                album.RemoveArtist(this);
            });
            MainActivity.stateHandler.Artists.Remove(this);
            Initialized = false;
        }

        public static string GetImagePath(string name)
        {
            string artistPart = FileManager.Sanitize(FileManager.GetAlias(name));
            if (File.Exists($"{FileManager.MusicFolder}/{artistPart}/cover.jpg"))
                return $"{FileManager.MusicFolder}/{artistPart}/cover.jpg";
            if (File.Exists($"{FileManager.MusicFolder}/{artistPart}/cover.png"))
                return $"{FileManager.MusicFolder}/{artistPart}/cover.png";
            return "Default";
        }
        
        public override Bitmap GetImage(bool shouldFallBack = true)
        {
            Bitmap image = null;

            try
            {
                if (!string.IsNullOrEmpty(ImgPath) && ImgPath != "Default")
                {
                    using FileStream f = File.OpenRead(ImgPath);
                    image = BitmapFactory.DecodeStream(f);
                    f.Close();
                }
                else if (shouldFallBack)
                {
                    foreach (Song song in Songs.Where(song => song.Initialized))
                    {
                        image = song.GetImage(false);
                        if (image != null)
                        {
                            break;
                        }
                    }

                    if (image == null)
                    {
                        foreach (Album album in Albums.Where(album => album.Initialized))
                        {
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
                    if (Application.Context.Assets != null)
                        image = BitmapFactory.DecodeStream(
                            Application.Context.Assets.Open(
                                "music_placeholder.png")); //In case of no cover and no embedded picture show default image from assets 
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e.ToString());
#endif
                if (Application.Context.Assets != null)
                    image = BitmapFactory.DecodeStream(
                        Application.Context.Assets.Open(
                            "music_placeholder.png")); //In case of no cover and no embedded picture show default image from assetsthrow;
            }
            return image;
        }

        public void AddAlias(string newAlias)
        {
            FileManager.AddAlias(newAlias, Title);
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

        [JsonConstructor]
        public Artist(string title)
        {
            Title = title;
            Initialized = false;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Artist item))
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